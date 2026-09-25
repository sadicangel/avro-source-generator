using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Avjs;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler;

public abstract class AvjsParser(SourceText sourceText, AvroParseOptions options, CancellationToken cancellationToken) : AvxxParser(options)
{
    protected AvroSchema? ParseSchema()
    {
        var root = ParseRootSyntax();
        if (root is null) return null;
        var schema = Schema(root, containingNamespace: null);
        if (schema is null || Diagnostics.HasErrors) return null;

        if (!schema.ContainsTopLevelSchema())
        {
            Report(AvroDiagnostic.MissingRootSchema(sourceText.GetSourceSpan()));
            return null;
        }

        return schema;
    }

    protected ProtocolSchema? ParseProtocol()
    {
        var root = ParseRootSyntax();
        if (root is null) return null;
        var syntax = AsProtocol(root);
        if (syntax is null) return null;
        var protocol = Protocol(syntax, containingNamespace: null);
        if (protocol is null || Diagnostics.HasErrors) return null;

        if (!protocol.ContainsTopLevelSchema())
        {
            Report(AvroDiagnostic.MissingRootSchema(sourceText.GetSourceSpan()));
            return null;
        }

        return protocol;
    }

    private T Invalid<T>(T value, AvroDiagnostic diagnostic)
    {
        Report(diagnostic);
        return value;
    }

    private JsonPropertySyntax? GetRequiredProperty(JsonObjectSyntax syntax, string propertyName)
    {
        var property = syntax.GetProperty(propertyName);
        if (property is not null) return property;
        Report(AvroDiagnostic.MissingProperty(syntax, propertyName));
        return null;
    }

    private static JsonPropertySyntax? GetOptionalProperty(JsonObjectSyntax syntax, string propertyName)
    {
        var property = syntax.GetProperty(propertyName);
        if (property is { Value.TokenType: not JsonTokenType.Null }) return property;
        return null;
    }

    private JsonObjectSyntax? AsProtocol(JsonSyntax syntax)
    {
        if (syntax is JsonObjectSyntax @object && @object.GetProperty(AvroJsonKeys.Protocol) is not null)
            return @object;

        return Invalid<JsonObjectSyntax?>(null, AvroDiagnostic.ProtocolExpected(syntax.SourceSpan));
    }

    private JsonObjectSyntax? AsObject(JsonSyntax syntax)
    {
        if (syntax is JsonObjectSyntax @object) return @object;
        Report(AvroDiagnostic.InvalidSchemaValue(syntax));
        return null;
    }

    private string? GetRequiredString(JsonPropertySyntax property)
    {
        if (property.Value is JsonValueSyntax { TokenType: JsonTokenType.String or JsonTokenType.Null } value)
        {
            var @string = value.GetString();
            if (!string.IsNullOrWhiteSpace(@string)) return @string!;
        }
        Report(AvroDiagnostic.InvalidPropertyString(property, required: true));
        return null;
    }

    private string? GetRequiredAvroName(JsonPropertySyntax property)
    {
        var name = GetRequiredString(property);
        if (name is null) return null;
        if (IsValidName(name)) return name;
        Report(AvroDiagnostic.InvalidAvroName(property));
        return null;
    }

    private string? GetNullableString(JsonPropertySyntax property)
    {
        if (property.Value is JsonValueSyntax { TokenType: JsonTokenType.String or JsonTokenType.Null } value)
        {
            var @string = value.GetString();
            return string.IsNullOrWhiteSpace(@string) ? null : @string;
        }
        Report(AvroDiagnostic.InvalidPropertyString(property, required: false));
        return null;
    }

    private string? GetOptionalNullableString(JsonObjectSyntax syntax, string propertyName)
    {
        var property = GetOptionalProperty(syntax, propertyName);
        return property is not null ? GetNullableString(property) : null;
    }

    private ImmutableArray<JsonSyntax> GetArrayItems(JsonPropertySyntax property)
    {
        if (property.Value is JsonArraySyntax array)
        {
            return array.Items;
        }
        Report(AvroDiagnostic.InvalidArray(property));
        return default;
    }

    private ImmutableArray<string> GetOptionalArrayStrings(JsonObjectSyntax syntax, string propertyName)
    {
        var property = GetOptionalProperty(syntax, propertyName);
        return property is null ? ImmutableArray<string>.Empty : GetArrayStrings(property, normalize: false);
    }

    private ImmutableArray<JsonSyntax> GetOptionalArrayItems(JsonObjectSyntax syntax, string propertyName)
    {
        var property = GetOptionalProperty(syntax, propertyName);
        return property is null ? ImmutableArray<JsonSyntax>.Empty : GetArrayItems(property);
    }

    private ImmutableArray<JsonPropertySyntax> GetObjectProperties(JsonPropertySyntax property)
    {
        if (property.Value is JsonObjectSyntax @object)
            return @object.Properties;

        Report(AvroDiagnostic.InvalidObject(property.Value, property.Parent!, property.Name.Value));
        return default;
    }

    private bool? GetBoolean(JsonPropertySyntax property)
    {
        if (property.Value.TokenType == JsonTokenType.True) return true;
        if (property.Value.TokenType == JsonTokenType.False) return false;
        Report(AvroDiagnostic.InvalidPropertyBoolean(property.Value, property.Parent!, property.Name.Value));
        return null;
    }

    private bool? GetOptionalBoolean(JsonObjectSyntax syntax, string propertyName)
    {
        var property = GetOptionalProperty(syntax, propertyName);
        return property is not null ? GetBoolean(property) : null;
    }

    private string? GetOptionalSchemaString(JsonObjectSyntax syntax, string propertyName)
    {
        var property = syntax.GetProperty(propertyName);
        if (property is null)
            return null;

        var value = (property.Value as JsonValueSyntax)?.GetString();
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : Invalid<string?>(null, AvroDiagnostic.InvalidJsonString(property.Value));
    }

    private static string? GetOptionalString(JsonObjectSyntax syntax, string propertyName)
    {
        var value = (syntax.GetProperty(propertyName)?.Value as JsonValueSyntax)?.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static JsonElement? GetDefaultJson(JsonObjectSyntax syntax) => syntax.GetProperty(AvroJsonKeys.Default)?.Value.AsJsonElement();

    private int GetSizeInt32(JsonPropertySyntax property)
    {
        var size = (property.Value as JsonValueSyntax)?.GetInt32();
        if (size is > 0)
            return size.Value;

        Report(AvroDiagnostic.InvalidFixedSize(property));
        return 0;
    }

    private ImmutableArray<string> GetArrayStrings(JsonPropertySyntax property, bool normalize)
    {
        var items = GetArrayItems(property);
        if (items.IsDefault) return default;

        var builder = ImmutableArray.CreateBuilder<string>(items.Length);
        foreach (var item in items.WithCancellation(cancellationToken))
        {
            var @string = (item as JsonValueSyntax)?.GetString();
            if (string.IsNullOrWhiteSpace(@string))
            {
                Report(AvroDiagnostic.InvalidStringArray(property));
                return default;
            }

            if (normalize && !IsValidName(@string))
            {
                Report(AvroDiagnostic.InvalidAvroName(item));
                return default;
            }

            builder.Add(normalize ? @string!.ToValidName() : @string!);
        }
        return builder.MoveToImmutable();
    }

    private SchemaName GetSchemaName(JsonValueSyntax syntax, string? containingNamespace)
    {
        var qualifiedName = syntax.GetString();
        if (string.IsNullOrWhiteSpace(qualifiedName))
            return Invalid(default(SchemaName), AvroDiagnostic.InvalidJsonString(syntax));

        if (!IsValidQualifiedName(qualifiedName!))
            return Invalid(default(SchemaName), AvroDiagnostic.InvalidAvroName(syntax));

        _ = qualifiedName!.TrySplitQualifiedName(out var localName, out var @namespace);
        if (!IsValidName(localName) || (@namespace is not null && !IsValidNamespace(@namespace)))
            return Invalid(default(SchemaName), AvroDiagnostic.InvalidAvroName(syntax));

        return new SchemaName(localName, @namespace ?? containingNamespace);
    }

    private SchemaName GetSchemaName(JsonPropertySyntax property, string? containingNamespace)
    {
        var qualifiedName = GetRequiredString(property);
        if (qualifiedName is null) return default;
        if (!IsValidQualifiedName(qualifiedName))
            return Invalid(default(SchemaName), AvroDiagnostic.InvalidAvroName(property));

        var hasNamespace = qualifiedName.TrySplitQualifiedName(out var localName, out var @namespace);
        var tracker = TrackDiagnostics();
        if (!hasNamespace)
            @namespace = GetOptionalNullableString((JsonObjectSyntax)property.Parent!, AvroJsonKeys.Namespace);
        if (tracker.HasNewDiagnostics) return default;
        if (!IsValidName(localName) || (@namespace is not null && !IsValidNamespace(@namespace)))
            return Invalid(default(SchemaName), AvroDiagnostic.InvalidAvroName(property));
        return new SchemaName(localName, @namespace ?? containingNamespace);
    }

    private static bool IsValidNamespace(string value) => value.Length == 0 || value.Split('.').All(IsValidName);

    private static bool IsValidQualifiedName(string value) => value.Split('.').All(IsValidName);

    private static bool IsValidName(string? value) => !string.IsNullOrEmpty(value) &&
        (char.IsAsciiLetter(value![0]) || value[0] is '_') &&
        value.All(static character => char.IsAsciiLetterOrDigit(character) || character is '_');

    protected JsonSyntax? ParseRootSyntax()
    {
        try
        {
            var reader = new JsonReader(sourceText);
            if (!reader.Read())
            {
                Report(AvroDiagnostic.EmptyJson(sourceText.GetSourceSpan()));
                return null;
            }

            var root = reader.Parse(cancellationToken);

            if (reader.Read())
            {
                Report(AvroDiagnostic.TrailingJson(reader.CurrentSpan));
                return null;
            }

            return root;
        }
        catch (JsonException ex)
        {
            Report(AvroDiagnostic.InvalidJson(SourceSpan.FromException(sourceText, ex), ex.Message));
            return null;
        }
    }

    protected AvroSchema? Schema(JsonSyntax syntax, string? containingNamespace)
    {
        return syntax switch
        {
            JsonValueSyntax { TokenType: JsonTokenType.String } value => Named(value, containingNamespace),
            JsonObjectSyntax @object when @object.GetProperty(AvroJsonKeys.Type) is not null => Complex(@object, containingNamespace),
            JsonArraySyntax array => Union(array, containingNamespace),
            _ => Invalid<AvroSchema?>(null, AvroDiagnostic.SchemaExpected(syntax.SourceSpan))
        };
    }

    // TODO: Change JsonValueSyntax syntax -> string or SchemaName?
    private AvroSchema? Named(JsonValueSyntax syntax, string? containingNamespace)
    {
        var schemaName = GetSchemaName(syntax, containingNamespace: null);
        return schemaName.Name is null ? null : Reference(schemaName, containingNamespace, syntax.SourceSpan);
    }

    private AvroSchema? Complex(JsonObjectSyntax syntax, string? containingNamespace)
    {
        var type = GetRequiredProperty(syntax, AvroJsonKeys.Type);
        if (type is null) return null;
        var typeName = GetRequiredString(type);
        if (typeName is null) return null;

        AvroSchema? schema = typeName switch
        {
            AvroTypeNames.Array => Array(syntax, containingNamespace),
            AvroTypeNames.Map => Map(syntax, containingNamespace),
            AvroTypeNames.Enum => Enum(syntax, containingNamespace),
            AvroTypeNames.Record => Record(syntax, containingNamespace),
            AvroTypeNames.Error => Error(syntax, containingNamespace),
            AvroTypeNames.Fixed => Fixed(syntax, containingNamespace),
            _ => Named((JsonValueSyntax)type.Value, containingNamespace)
        };

        if (schema is PrimitiveSchema)
        {
            var tracker = TrackDiagnostics();
            var documentation = GetOptionalNullableString(syntax, AvroJsonKeys.Doc);
            var properties = syntax.GetSchemaProperties();
            if (tracker.HasNewDiagnostics) schema = null;
            if (schema is not null && (documentation is not null || !properties.IsEmpty))
                schema = schema with { Documentation = documentation, Properties = properties };
        }

        var logicalTracker = TrackDiagnostics();
        var logicalType = GetOptionalSchemaString(syntax, AvroJsonKeys.LogicalType);
        if (schema is null || logicalTracker.HasNewDiagnostics) return null;
        return logicalType is null ? schema : LogicalSchema.Create(logicalType, schema, Options.GenerationTarget);
    }

    private AvroSchema? Array(JsonObjectSyntax syntax, string? containingNamespace)
    {
        var tracker = TrackDiagnostics();
        var itemsProperty = GetRequiredProperty(syntax, AvroJsonKeys.Items);
        var items = itemsProperty is null ? null : Schema(itemsProperty.Value, containingNamespace);
        var documentation = GetOptionalNullableString(syntax, AvroJsonKeys.Doc);
        var properties = syntax.GetSchemaProperties();
        return items is not null && !tracker.HasNewDiagnostics
            ? new ArraySchema(items, documentation, properties)
            : null;
    }

    private AvroSchema? Map(JsonObjectSyntax syntax, string? containingNamespace)
    {
        var tracker = TrackDiagnostics();
        var valuesProperty = GetRequiredProperty(syntax, AvroJsonKeys.Values);
        var values = valuesProperty is null ? null : Schema(valuesProperty.Value, containingNamespace);
        var documentation = GetOptionalNullableString(syntax, AvroJsonKeys.Doc);
        var properties = syntax.GetSchemaProperties();
        return values is not null && !tracker.HasNewDiagnostics
            ? new MapSchema(values, documentation, properties)
            : null;
    }

    private NamedSchema? Enum(JsonObjectSyntax syntax, string? containingNamespace) => EnterRegisterScope(
        syntax,
        AvroJsonKeys.Name,
        containingNamespace,
        static (parser, syntax, schemaName) =>
        {
            var tracker = parser.TrackDiagnostics();
            var documentation = parser.GetOptionalNullableString(syntax, AvroJsonKeys.Doc);
            var aliases = parser.GetOptionalArrayStrings(syntax, AvroJsonKeys.Aliases);
            var symbolsProperty = parser.GetRequiredProperty(syntax, AvroJsonKeys.Symbols);
            var symbols = symbolsProperty is null ? default : parser.GetArrayStrings(symbolsProperty, normalize: true);
            var defaultSymbol = parser.GetOptionalNullableString(syntax, AvroJsonKeys.Default);
            var properties = syntax.GetSchemaProperties();
            return !tracker.HasNewDiagnostics && !aliases.IsDefault && !symbols.IsDefault
                ? new EnumSchema(schemaName, documentation, aliases, symbols, defaultSymbol, properties)
                : null;
        });

    private NamedSchema? Fixed(JsonObjectSyntax syntax, string? containingNamespace) => EnterRegisterScope(
        syntax,
        AvroJsonKeys.Name,
        containingNamespace,
        static (parser, syntax, schemaName) =>
        {
            var tracker = parser.TrackDiagnostics();
            var documentation = parser.GetOptionalNullableString(syntax, AvroJsonKeys.Doc);
            var aliases = parser.GetOptionalArrayStrings(syntax, AvroJsonKeys.Aliases);
            var sizeProperty = parser.GetRequiredProperty(syntax, AvroJsonKeys.Size);
            var size = sizeProperty is null ? 0 : parser.GetSizeInt32(sizeProperty);
            var properties = syntax.GetSchemaProperties();
            return !tracker.HasNewDiagnostics && !aliases.IsDefault && size > 0
                ? new FixedSchema(schemaName, documentation, aliases, size, properties)
                {
                    CSharpName = parser.Options.GenerationTarget is GenerationTarget.Apache
                        ? CSharpName.FromSchemaName(schemaName)
                        : AvroSchema.Bytes.CSharpName
                }
                : null;
        });

    private NamedSchema? Record(JsonObjectSyntax syntax, string? containingNamespace) => EnterRegisterScope(
        syntax,
        AvroJsonKeys.Name,
        containingNamespace,
        static (parser, syntax, schemaName) =>
        {
            var tracker = parser.TrackDiagnostics();
            var documentation = parser.GetOptionalNullableString(syntax, AvroJsonKeys.Doc);
            var aliases = parser.GetOptionalArrayStrings(syntax, AvroJsonKeys.Aliases);
            var fields = parser.Fields(syntax, schemaName);
            var properties = syntax.GetSchemaProperties();
            return !tracker.HasNewDiagnostics && !aliases.IsDefault && !fields.IsDefault
                ? new RecordSchema(schemaName, documentation, aliases, fields, properties)
                : null;
        });

    private NamedSchema? Error(JsonObjectSyntax syntax, string? containingNamespace) => EnterRegisterScope(
        syntax,
        AvroJsonKeys.Name,
        containingNamespace,
        static (parser, syntax, schemaName) =>
        {
            var tracker = parser.TrackDiagnostics();
            var documentation = parser.GetOptionalNullableString(syntax, AvroJsonKeys.Doc);
            var aliases = parser.GetOptionalArrayStrings(syntax, AvroJsonKeys.Aliases);
            var fields = parser.Fields(syntax, schemaName);
            var properties = syntax.GetSchemaProperties();
            return !tracker.HasNewDiagnostics && !aliases.IsDefault && !fields.IsDefault
                ? new ErrorSchema(schemaName, documentation, aliases, fields, properties)
                : null;
        });

    private ImmutableArray<T> ParseItems<TSyntax, T, TState>(
        ImmutableArray<TSyntax> items,
        TState state,
        Func<TSyntax, TState, T?> parse)
        where T : class
    {
        if (items.IsDefault) return default;
        var builder = ImmutableArray.CreateBuilder<T>(items.Length);
        foreach (var item in items.WithCancellation(cancellationToken))
        {
            var result = parse(item, state);
            if (result is null)
                return default;
            builder.Add(result);
        }
        return builder.MoveToImmutable();
    }

    private ImmutableArray<T> ParseObjectItems<T, TState>(
        ImmutableArray<JsonSyntax> items,
        TState state,
        Func<JsonObjectSyntax, TState, T?> parse) where T : class =>
        ParseItems(
            items,
            (Parser: this, State: state, Parse: parse),
            static (item, state) => state.Parser.AsObject(item) is { } @object
                ? state.Parse(@object, state.State)
                : null);

    private ImmutableArray<Field> Fields(JsonObjectSyntax syntax, SchemaName containingSchemaName) =>
        ParseObjectItems(
            GetRequiredProperty(syntax, AvroJsonKeys.Fields) is { } property ? GetArrayItems(property) : default,
            (Parser: this, ContainingSchemaName: containingSchemaName),
            static (field, state) => state.Parser.Field(field, state.ContainingSchemaName));

    private Field? Field(JsonObjectSyntax syntax, SchemaName containingSchemaName)
    {
        var nameProperty = GetRequiredProperty(syntax, AvroJsonKeys.Name);
        var name = nameProperty is null ? null : GetRequiredAvroName(nameProperty);
        if (name is null) return null;
        var fieldName = new FieldName(name);

        var typeProperty = GetRequiredProperty(syntax, AvroJsonKeys.Type);
        var fieldType = typeProperty is null ? null : Schema(typeProperty.Value, containingSchemaName.Namespace);
        if (fieldType is null) return null;

        fieldType = ResolveFieldType(fieldType, fieldName, containingSchemaName, out var underlyingType, out var remarks);

        var tracker = TrackDiagnostics();
        var documentation = GetOptionalNullableString(syntax, AvroJsonKeys.Doc);
        var aliases = GetOptionalArrayStrings(syntax, AvroJsonKeys.Aliases);
        var defaultJson = GetDefaultJson(syntax);
        var order = GetOptionalString(syntax, AvroJsonKeys.Order);
        var properties = syntax.GetSchemaProperties();
        if (tracker.HasNewDiagnostics || aliases.IsDefault) return null;
        return new Field(fieldName, fieldType, underlyingType, documentation, aliases, defaultJson,
            fieldType.GetValue(defaultJson), order, properties, remarks);
    }

    private AvroSchema? Union(JsonArraySyntax syntax, string? containingNamespace)
    {
        var schemas = ParseItems(syntax.Items, (Parser: this, ContainingNamespace: containingNamespace),
            static (item, state) => state.Parser.Schema(item, state.ContainingNamespace));
        return schemas.IsDefault ? null : UnionSchema.Create(schemas, Options.UseNullableReferenceTypes);
    }


    protected ProtocolSchema? Protocol(JsonObjectSyntax syntax, string? containingNamespace)
    {
        return EnterRegisterScope(
            syntax,
            AvroJsonKeys.Protocol,
            containingNamespace,
            static (parser, syntax, schemaName) =>
            {
                var tracker = parser.TrackDiagnostics();
                var types = parser.ProtocolTypes(syntax, schemaName.Namespace);
                var messages = parser.ProtocolMessages(syntax, schemaName.Namespace);
                var documentation = parser.GetOptionalNullableString(syntax, AvroJsonKeys.Doc);
                var properties = syntax.GetProtocolProperties();
                return !tracker.HasNewDiagnostics && !types.IsDefault && !messages.IsDefault
                    ? new ProtocolSchema(schemaName, documentation, types, messages, properties)
                    : null;
            });
    }

    private ImmutableArray<NamedSchema> ProtocolTypes(JsonObjectSyntax syntax, string? containingNamespace) =>
        ParseItems(
            GetRequiredProperty(syntax, AvroJsonKeys.Types) is { } property ? GetArrayItems(property) : default,
            (Parser: this, ContainingNamespace: containingNamespace),
            static (item, state) => state.Parser.NamedSchema(item, state.ContainingNamespace));

    private ImmutableArray<ProtocolMessage> ProtocolMessages(JsonObjectSyntax syntax, string? containingNamespace) =>
        ParseItems(
            GetRequiredProperty(syntax, AvroJsonKeys.Messages) is { } property ? GetObjectProperties(property) : default,
            (Parser: this, ContainingNamespace: containingNamespace),
            static (message, state) => state.Parser.Message(message, state.ContainingNamespace));

    private NamedSchema? NamedSchema(JsonSyntax syntax, string? containingNamespace)
    {
        if (syntax is not JsonObjectSyntax namedSchema || namedSchema.GetProperty(AvroJsonKeys.Type) is null)
            return Invalid<NamedSchema?>(null, AvroDiagnostic.SchemaExpected(syntax.SourceSpan));

        var type = GetRequiredString(namedSchema.GetProperty(AvroJsonKeys.Type)!);
        if (type is null) return null;

        return type switch
        {
            AvroTypeNames.Enum => Enum(namedSchema, containingNamespace),
            AvroTypeNames.Record => Record(namedSchema, containingNamespace),
            AvroTypeNames.Error => Error(namedSchema, containingNamespace),
            AvroTypeNames.Fixed => Fixed(namedSchema, containingNamespace),
            _ => Invalid<NamedSchema?>(null, AvroDiagnostic.UnknownSchemaType(namedSchema, type))
        };
    }

    private ProtocolMessage? Message(JsonPropertySyntax message, string? containingNamespace)
    {
        var syntax = AsObject(message.Value);
        if (syntax is null) return null;

        var tracker = TrackDiagnostics();
        var parameters = ProtocolRequest(syntax, containingNamespace);
        var response = ProtocolResponse(syntax, containingNamespace);
        var errors = ProtocolErrors(syntax, containingNamespace);
        var oneWay = GetOptionalBoolean(syntax, AvroJsonKeys.OneWay);
        var documentation = GetOptionalNullableString(syntax, AvroJsonKeys.Doc);
        if (tracker.HasNewDiagnostics || parameters.IsDefault || response is null || errors.IsDefault)
            return null;
        if (oneWay is true && (response.Type.Type is not SchemaType.Null || !errors.IsEmpty))
            return Invalid<ProtocolMessage?>(null, AvroDiagnostic.InvalidOneWayMessage(message.Value, message.Name.Value));
        return new ProtocolMessage(message.Name.Value.ToValidName(), documentation, parameters, response, errors, oneWay);
    }

    private ImmutableArray<ProtocolRequestParameter> ProtocolRequest(JsonObjectSyntax syntax, string? containingNamespace) =>
        ParseObjectItems(
            GetRequiredProperty(syntax, AvroJsonKeys.Request) is { } property ? GetArrayItems(property) : default,
            (Parser: this, ContainingNamespace: containingNamespace),
            static (parameter, state) => state.Parser.ProtocolRequestParameter(parameter, state.ContainingNamespace));

    private ProtocolResponse? ProtocolResponse(JsonObjectSyntax syntax, string? containingNamespace)
    {
        var property = GetRequiredProperty(syntax, AvroJsonKeys.Response);
        if (property is null) return null;
        var type = Schema(property.Value, containingNamespace);
        return type is not null ? new ProtocolResponse(type, type is UnionSchema union ? union.UnderlyingSchema : type) : null;
    }

    private ImmutableArray<AvroSchema> ProtocolErrors(JsonObjectSyntax syntax, string? containingNamespace) =>
        ParseItems(
            GetOptionalArrayItems(syntax, AvroJsonKeys.Errors),
            (Parser: this, ContainingNamespace: containingNamespace),
            static (item, state) => state.Parser.Schema(item, state.ContainingNamespace));

    private ProtocolRequestParameter? ProtocolRequestParameter(JsonObjectSyntax syntax, string? containingNamespace)
    {
        var tracker = TrackDiagnostics();
        var nameProperty = GetRequiredProperty(syntax, AvroJsonKeys.Name);
        var name = nameProperty is null ? null : GetRequiredAvroName(nameProperty);
        var typeProperty = GetRequiredProperty(syntax, AvroJsonKeys.Type);
        var type = typeProperty is null ? null : Schema(typeProperty.Value, containingNamespace);
        var documentation = GetOptionalNullableString(syntax, AvroJsonKeys.Doc);
        var defaultJson = GetDefaultJson(syntax);
        if (name is null || type is null || tracker.HasNewDiagnostics) return null;
        var underlyingType = type is UnionSchema union ? union.UnderlyingSchema : type;
        return new ProtocolRequestParameter(name.ToValidName(), type, underlyingType, documentation, defaultJson, type.GetValue(defaultJson));
    }

    private TSchema? EnterRegisterScope<TSchema>(
        JsonObjectSyntax syntax,
        string propertyName,
        string? containingNamespace,
        Func<AvjsParser, JsonObjectSyntax, SchemaName, TSchema?> parse)
        where TSchema : TopLevelSchema
    {
        var property = GetRequiredProperty(syntax, propertyName);
        if (property is null) return null;

        var tracker = TrackDiagnostics();
        var schemaName = GetSchemaName(property, containingNamespace);
        if (schemaName.Name is null || tracker.HasNewDiagnostics)
            return null;

        if (IsInRecursionScope(schemaName))
            return Invalid<TSchema?>(null, AvroDiagnostic.RecursiveDefinition(property.Value.SourceSpan, schemaName));

        using var scope = EnterRecursionScope(schemaName);
        var schema = parse(this, syntax, schemaName);
        if (schema is null) return null;

        Declare(schema, syntax.SourceSpan);

        return schema;
    }
}
