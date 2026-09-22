using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Avsc.Syntax;

public sealed class AvscParser(SourceText sourceText, AvroParseOptions options, CancellationToken cancellationToken) : ParserBase(options, cancellationToken)
{
    private Option<T> Invalid<T>(AvroDiagnostic diagnostic)
    {
        Report(diagnostic);
        return Option.None<T>();
    }

    private Option<JsonPropertySyntax> GetRequiredProperty(JsonObjectSyntax syntax, string propertyName)
    {
        var property = syntax.GetProperty(propertyName);
        if (property is not null) return property;
        Report(AvroDiagnostic.MissingProperty(syntax, propertyName));
        return Option.None<JsonPropertySyntax>();
    }

    private Option<JsonPropertySyntax> GetOptionalProperty(JsonObjectSyntax syntax, string propertyName)
    {
        var property = syntax.GetProperty(propertyName);
        if (property is { Value.TokenType: not JsonTokenType.Null }) return property;
        return Option.None<JsonPropertySyntax>();
    }

    private Option<JsonObjectSyntax> AsObject(JsonSyntax syntax)
    {
        if (syntax is JsonObjectSyntax @object) return @object;
        Report(AvroDiagnostic.InvalidSchemaValue(syntax));
        return Option.None<JsonObjectSyntax>();
    }

    private static Option<string> GetRequiredString(JsonPropertySyntax property, AvscParser parser) => parser.GetRequiredString(property);

    private Option<string> GetRequiredString(JsonPropertySyntax property)
    {
        if (property.Value is JsonValueSyntax { TokenType: JsonTokenType.String or JsonTokenType.Null } value)
        {
            var @string = value.AsString();
            if (!string.IsNullOrWhiteSpace(@string)) return @string!;
        }
        Report(AvroDiagnostic.InvalidPropertyString(property, required: true));
        return Option.None<string>();
    }

    private static Option<string?> GetNullableString(JsonPropertySyntax property, AvscParser parser) => parser.GetNullableString(property);

    private Option<string?> GetNullableString(JsonPropertySyntax property)
    {
        if (property.Value is JsonValueSyntax { TokenType: JsonTokenType.String or JsonTokenType.Null } value)
        {
            var @string = value.AsString();
            return string.IsNullOrWhiteSpace(@string) ? null : @string;
        }
        Report(AvroDiagnostic.InvalidPropertyString(property, required: false));
        return Option.None<string?>();
    }

    private Option<string?> GetOptionalNullableString(JsonObjectSyntax syntax, string propertyName)
    {
        var property = GetOptionalProperty(syntax, propertyName);
        return property.IsSome ? GetNullableString(property.Value) : Option.Some<string?>(null);
    }

    private static Option<ImmutableArray<JsonSyntax>> GetArrayItems(JsonPropertySyntax property, AvscParser parser) => parser.GetArrayItems(property);

    private Option<ImmutableArray<JsonSyntax>> GetArrayItems(JsonPropertySyntax property)
    {
        if (property.Value is JsonArraySyntax array)
        {
            return array.Items;
        }
        Report(AvroDiagnostic.InvalidArray(property));
        return Option.None<ImmutableArray<JsonSyntax>>();
    }

    private static Option<ImmutableArray<string>> GetArrayStrings(JsonPropertySyntax property, AvscParser parser) => parser.GetArrayStrings(property, normalize: false);
    private static Option<ImmutableArray<string>> GetArraySymbols(JsonPropertySyntax property, AvscParser parser) => parser.GetArrayStrings(property, normalize: true);

    private Option<ImmutableArray<string>> GetOptionalArrayStrings(JsonObjectSyntax syntax, string propertyName)
    {
        var property = GetOptionalProperty(syntax, propertyName);
        return property.IsNone ? ImmutableArray<string>.Empty : property.Then(this, GetArrayStrings);
    }

    private Option<ImmutableArray<JsonSyntax>> GetOptionalArrayItems(JsonObjectSyntax syntax, string propertyName)
    {
        var property = GetOptionalProperty(syntax, propertyName);
        return property.IsNone ? ImmutableArray<JsonSyntax>.Empty : property.Then(this, GetArrayItems);
    }

    private Option<ImmutableArray<JsonPropertySyntax>> GetObjectProperties(JsonPropertySyntax property)
    {
        if (property.Value is JsonObjectSyntax @object)
            return @object.Properties;

        Report(AvroDiagnostic.InvalidObject(property.Value, property.Parent!, property.Name.Value));
        return Option.None<ImmutableArray<JsonPropertySyntax>>();
    }

    private Option<bool> GetBoolean(JsonPropertySyntax property)
    {
        if (property.Value.TokenType == JsonTokenType.True) return true;
        if (property.Value.TokenType == JsonTokenType.False) return false;
        Report(AvroDiagnostic.InvalidPropertyBoolean(property.Value, property.Parent!, property.Name.Value));
        return Option.None<bool>();
    }

    private Option<bool?> GetOptionalBoolean(JsonObjectSyntax syntax, string propertyName)
    {
        var property = GetOptionalProperty(syntax, propertyName);
        return property.IsSome
            ? GetBoolean(property.Value).Then(static value => (bool?)value)
            : Option.Some<bool?>(null);
    }

    private Option<string?> GetOptionalSchemaString(JsonObjectSyntax syntax, string propertyName)
    {
        var property = syntax.GetProperty(propertyName);
        if (property is null)
            return Option.Some<string?>(null);

        var value = (property.Value as JsonValueSyntax)?.AsString();
        return !string.IsNullOrWhiteSpace(value)
            ? Option.Some<string?>(value)
            : Invalid<string?>(AvroDiagnostic.InvalidJsonString(property.Value));
    }

    private static Option<string?> GetOptionalString(JsonObjectSyntax syntax, string propertyName)
    {
        var value = (syntax.GetProperty(propertyName)?.Value as JsonValueSyntax)?.AsString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static Option<JsonElement?> GetDefaultJson(JsonObjectSyntax syntax)
    {
        var property = syntax.GetProperty(AvroJsonKeys.Default);
        return property is null ? Option.Some<JsonElement?>(null) : (JsonElement?)property.Value.ToJsonElement();
    }

    private static Option<int> GetSizeInt32(JsonPropertySyntax property, AvscParser parser)
    {
        var size = (property.Value as JsonValueSyntax)?.Int32Value;
        if (size is > 0)
            return size.Value;

        parser.Report(AvroDiagnostic.InvalidFixedSize(property));
        return Option.None<int>();
    }

    private Option<ImmutableArray<string>> GetArrayStrings(JsonPropertySyntax property, bool normalize)
    {
        if (!GetArrayItems(property).TryGetValue(out var items))
            return Option.None<ImmutableArray<string>>();

        var builder = ImmutableArray.CreateBuilder<string>(items.Length);
        foreach (var @string in items.Select(item => (item as JsonValueSyntax)?.AsString()))
        {
            if (string.IsNullOrWhiteSpace(@string))
            {
                Report(AvroDiagnostic.InvalidStringArray(property));
                return Option.None<ImmutableArray<string>>();
            }

            builder.Add(normalize ? @string!.ToValidName() : @string!);
        }
        return Option.Some(builder.MoveToImmutable());
    }

    private Option<SchemaName> GetSchemaName(JsonObjectSyntax syntax, string? containingNamespace) => GetName(syntax, AvroJsonKeys.Name, containingNamespace);
    private Option<SchemaName> GetProtocolName(JsonObjectSyntax syntax, string? containingNamespace) => GetName(syntax, AvroJsonKeys.Protocol, containingNamespace);

    private Option<SchemaName> GetSchemaName(JsonValueSyntax syntax, string? containingNamespace)
    {
        var qualifiedName = syntax.AsString();
        if (string.IsNullOrWhiteSpace(qualifiedName))
            return Invalid<SchemaName>(AvroDiagnostic.InvalidJsonString(syntax));

        _ = qualifiedName!.TrySplitQualifiedName(out var localName, out var @namespace);
        if (string.IsNullOrWhiteSpace(localName) || @namespace is "")
            return Invalid<SchemaName>(AvroDiagnostic.InvalidSchemaNameLeadingOrTrailingDot(syntax));

        return new SchemaName(localName, @namespace ?? containingNamespace);
    }

    private Option<SchemaName> GetName(JsonObjectSyntax syntax, string propertyName, string? containingNamespace)
    {
        if (!GetRequiredProperty(syntax, propertyName).TryGetValue(out var property))
            return Option.None<SchemaName>();

        return GetRequiredString(property)
            .Then(
                (Parser: this, Property: property),
                static (qualifiedName, state) =>
                {
                    if (qualifiedName.Contains("..", StringComparison.Ordinal))
                    {
                        state.Parser.Report(AvroDiagnostic.InvalidSchemaNameConsecutiveDots(state.Property));
                        return Option.None<(string LocalName, string? Namespace)>();
                    }

                    var hasNamespace = qualifiedName.TrySplitQualifiedName(out var localName, out var @namespace);
                    var namespaceResult = hasNamespace
                        ? Option.Some(@namespace)
                        : state.Parser.GetOptionalNullableString((JsonObjectSyntax)state.Property.Parent!, AvroJsonKeys.Namespace);

                    if (!namespaceResult.IsSome)
                        return Option.None<(string LocalName, string? Namespace)>();

                    @namespace = namespaceResult.Value;

                    if (string.IsNullOrWhiteSpace(localName) || @namespace is "")
                    {
                        state.Parser.Report(AvroDiagnostic.InvalidSchemaNameLeadingOrTrailingDot(state.Property));
                        return Option.None<(string LocalName, string? Namespace)>();
                    }

                    return localName.With(Option.Some(@namespace));
                })
            .Then(containingNamespace, static ((string LocalName, string? Namespace) name, string? containingNamespace) => new SchemaName(name.LocalName, name.Namespace ?? containingNamespace));
    }

    public static AvroFile Parse(SourceText sourceText, AvroParseOptions options, CancellationToken cancellationToken) =>
        new AvscParser(sourceText, options, cancellationToken).Parse();

    public AvroFile Parse()
    {
        ThrowIfCancellationRequested();
        var schema = ParseRootSyntax().Then((Parser: this, ContainingNamespace: (string?)null), Schema);
        if (!schema.IsSome)
            return AvroFile.Invalid(Source, [.. Diagnostics], Options);

        if (!schema.Value.ContainsTopLevelSchema())
        {
            Report(AvroDiagnostic.MissingRootSchema(Source.GetSourceSpan()));
            return AvroFile.Invalid(Source, [.. Diagnostics], Options);
        }

        return new AvroFile(
            Source,
            schema.Value,
            [.. Declarations],
            [.. DeclarationSpans],
            GetReferences(),
            GetReferenceSpans(),
            GetDependencies(),
            [],
            [.. Diagnostics],
            Options);
    }

    private SourceText Source => sourceText;

    private Option<JsonSyntax> ParseRootSyntax()
    {
        try
        {
            var reader = new JsonReader(sourceText, CancellationToken);
            if (!reader.Read())
            {
                Report(AvroDiagnostic.EmptyJson(sourceText.GetSourceSpan()));
                return Option.None<JsonSyntax>();
            }

            var root = reader.Parse();

            if (reader.Read())
            {
                Report(AvroDiagnostic.TrailingJson(reader.CurrentSpan));
                return Option.None<JsonSyntax>();
            }

            return root;
        }
        catch (JsonException ex)
        {
            Report(AvroDiagnostic.InvalidJson(SourceSpan.FromException(sourceText, ex), ex.Message));
            return Option.None<JsonSyntax>();
        }
    }

    private bool TryEnterSchemaScope(SchemaName schemaName, out RecursionScope scope)
    {
        if (TryEnterRecursionScope(schemaName, out scope)) return true;
        Report(AvroDiagnostic.RecursiveDefinition(sourceText.GetSourceSpan(), schemaName));
        return false;
    }

    private Option<AvroSchema> Schema(JsonSyntax syntax, string? containingNamespace)
    {
        CancellationToken.ThrowIfCancellationRequested();
        return syntax switch
        {
            JsonValueSyntax { TokenType: JsonTokenType.String } value => Named(value, containingNamespace),
            JsonObjectSyntax @object => Complex(@object, containingNamespace),
            JsonArraySyntax array => Union(array, containingNamespace),
            _ => Invalid<AvroSchema>(AvroDiagnostic.InvalidSchemaValue(syntax))
        };
    }

    // Just a helper so we don't have to type this everytime, everywhere!
    private static Option<AvroSchema> Schema(JsonPropertySyntax property, (AvscParser Parser, string? ContainingNamespace) state) =>
        state.Parser.Schema(property.Value, state.ContainingNamespace);

    private static Option<AvroSchema> Schema(JsonSyntax syntax, (AvscParser Parser, string? ContainingNamespace) state) =>
        state.Parser.Schema(syntax, state.ContainingNamespace);

    // TODO: Change JsonValueSyntax syntax -> string or SchemaName?
    private Option<AvroSchema> Named(JsonValueSyntax syntax, string? containingNamespace) => GetSchemaName(syntax, containingNamespace: null).TryGetValue(out var schemaName)
        ? Reference(schemaName, containingNamespace, syntax.SourceSpan)
        : Option.None<AvroSchema>();

    private Option<AvroSchema> Complex(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (syntax.HasProperty(AvroJsonKeys.Protocol))
            return Protocol(syntax, containingNamespace);

        var type = GetRequiredProperty(syntax, AvroJsonKeys.Type);
        var typeName = type.Then(this, GetRequiredString);
        if (!typeName.IsSome) return Option.None<AvroSchema>();

        var schema = typeName.Value switch
        {
            AvroTypeNames.Array => Array(syntax, containingNamespace),
            AvroTypeNames.Map => Map(syntax, containingNamespace),
            AvroTypeNames.Enum => Enum(syntax, containingNamespace),
            AvroTypeNames.Record => Record(syntax, containingNamespace, isError: false),
            AvroTypeNames.Error => Record(syntax, containingNamespace, isError: true),
            AvroTypeNames.Fixed => Fixed(syntax, containingNamespace),
            _ => Named((JsonValueSyntax)type.Value.Value, containingNamespace)
        };

        if (schema is { IsSome: true, Value: PrimitiveSchema })
        {
            var documentation = GetOptionalNullableString(syntax, AvroJsonKeys.Doc);
            var properties = Option.Some(syntax.GetSchemaProperties());
            schema = schema.With(documentation).With(properties).Then(static arguments =>
            {
                var (schema, documentation, properties) = arguments;
                return documentation is null && properties.IsEmpty
                    ? schema
                    : schema with
                    {
                        Documentation = documentation,
                        Properties = properties
                    };
            });
        }

        var logicalType = GetOptionalSchemaString(syntax, AvroJsonKeys.LogicalType);
        return schema.With(logicalType).Then(
            Options.GenerationTarget,
            static (arguments, target) =>
            {
                var (schema, logicalType) = arguments;
                return logicalType is null ? schema : LogicalSchema.Create(logicalType, schema, target);
            });
    }

    private Option<AvroSchema> Array(JsonObjectSyntax syntax, string? containingNamespace)
    {
        var items = GetRequiredProperty(syntax, AvroJsonKeys.Items).Then((this, containingNamespace), Schema);
        var documentation = GetOptionalNullableString(syntax, AvroJsonKeys.Doc);
        var properties = Option.Some(syntax.GetSchemaProperties());
        return items.With(documentation).With(properties)
            .Then(static AvroSchema ((AvroSchema ItemsSchema, string? Documentation, ImmutableSortedDictionary<string, JsonElement> Properties) result) =>
                new ArraySchema(result.ItemsSchema, result.Documentation, result.Properties));
    }

    private Option<AvroSchema> Map(JsonObjectSyntax syntax, string? containingNamespace)
    {
        var values = GetRequiredProperty(syntax, AvroJsonKeys.Values).Then((this, containingNamespace), Schema);
        var documentation = GetOptionalNullableString(syntax, AvroJsonKeys.Doc);
        var properties = Option.Some(syntax.GetSchemaProperties());
        return values.With(documentation).With(properties)
            .Then(static AvroSchema ((AvroSchema ValuesSchema, string? Documentation, ImmutableSortedDictionary<string, JsonElement> Properties) result) =>
                new MapSchema(result.ValuesSchema, result.Documentation, result.Properties));
    }

    private Option<AvroSchema> Enum(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (!GetSchemaName(syntax, containingNamespace).TryGetValue(out var schemaName) || !TryEnterSchemaScope(schemaName, out var scope))
            return Option.None<AvroSchema>();

        using (scope)
        {
            var documentation = GetOptionalNullableString(syntax, AvroJsonKeys.Doc);
            var aliases = GetOptionalArrayStrings(syntax, AvroJsonKeys.Aliases);
            var symbols = GetRequiredProperty(syntax, AvroJsonKeys.Symbols).Then(this, GetArraySymbols);
            var defaultSymbol = GetOptionalNullableString(syntax, AvroJsonKeys.Default);
            var properties = Option.Some(syntax.GetSchemaProperties());

            var schema = schemaName.With(documentation).With(aliases).With(symbols).With(defaultSymbol).With(properties)
                .Then(static AvroSchema ((SchemaName SchemaName, string? Documentation, ImmutableArray<string> Aliases, ImmutableArray<string> Symbols, string? DefaultSymbol, ImmutableSortedDictionary<string, JsonElement> Properties) args) =>
                    new EnumSchema(args.SchemaName, args.Documentation, args.Aliases, args.Symbols, args.DefaultSymbol, args.Properties));

            if (schema.IsSome)
                Declare((TopLevelSchema)schema.Value, syntax.SourceSpan);

            return schema;
        }
    }

    private Option<AvroSchema> Fixed(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (!GetSchemaName(syntax, containingNamespace).TryGetValue(out var schemaName) || !TryEnterSchemaScope(schemaName, out var scope))
            return Option.None<AvroSchema>();

        using (scope)
        {
            var documentation = GetOptionalNullableString(syntax, AvroJsonKeys.Doc);
            var aliases = GetOptionalArrayStrings(syntax, AvroJsonKeys.Aliases);
            var size = GetRequiredProperty(syntax, AvroJsonKeys.Size).Then(this, GetSizeInt32);
            var properties = Option.Some(syntax.GetSchemaProperties());

            var schema = schemaName.With(documentation).With(aliases).With(size).With(properties)
                .Then(
                    Options.GenerationTarget is GenerationTarget.Apache,
                    static AvroSchema ((SchemaName SchemaName, string? Documentation, ImmutableArray<string> Aliases, int Size, ImmutableSortedDictionary<string, JsonElement> Properties) args, bool isApache) =>
                        new FixedSchema(args.SchemaName, args.Documentation, args.Aliases, args.Size, args.Properties) { CSharpName = isApache ? CSharpName.FromSchemaName(args.SchemaName) : AvroSchema.Bytes.CSharpName });

            if (schema.IsSome)
                Declare((TopLevelSchema)schema.Value, syntax.SourceSpan);

            return schema;
        }
    }

    private Option<AvroSchema> Record(JsonObjectSyntax syntax, string? containingNamespace, bool isError)
    {
        if (!GetSchemaName(syntax, containingNamespace).TryGetValue(out var schemaName) || !TryEnterSchemaScope(schemaName, out var scope))
            return Option.None<AvroSchema>();

        using (scope)
        {
            var documentation = GetOptionalNullableString(syntax, AvroJsonKeys.Doc);
            var aliases = GetOptionalArrayStrings(syntax, AvroJsonKeys.Aliases);
            var fields = Fields(syntax, schemaName);
            var properties = Option.Some(syntax.GetSchemaProperties());

            var schema = schemaName.With(documentation).With(aliases).With(fields).With(properties)
                .Then(
                    isError,
                    static AvroSchema ((SchemaName SchemaName, string? Documentation, ImmutableArray<string> Aliases, ImmutableArray<Field> Fields, ImmutableSortedDictionary<string, JsonElement> Properties) args, bool isError) => isError
                        ? new ErrorSchema(args.SchemaName, args.Documentation, args.Aliases, args.Fields, args.Properties)
                        : new RecordSchema(args.SchemaName, args.Documentation, args.Aliases, args.Fields, args.Properties));

            if (schema.IsSome)
                Declare((TopLevelSchema)schema.Value, syntax.SourceSpan);

            return schema;
        }
    }

    private Option<ImmutableArray<T>> ParseItems<TSyntax, T, TState>(
        ImmutableArray<TSyntax> items,
        TState state,
        Func<TSyntax, TState, Option<T>> parse)
    {
        var builder = ImmutableArray.CreateBuilder<T>(items.Length);
        foreach (var item in items.WithCancellation(CancellationToken))
        {
            var result = parse(item, state);
            if (!result.TryGetValue(out var value))
                return Option.None<ImmutableArray<T>>();
            builder.Add(value);
        }
        return builder.MoveToImmutable();
    }

    private Option<ImmutableArray<T>> ParseItems<TSyntax, T, TState>(
        Option<ImmutableArray<TSyntax>> items,
        TState state,
        Func<TSyntax, TState, Option<T>> parse)
    {
        if (!items.TryGetValue(out var values))
            return Option.None<ImmutableArray<T>>();

        return ParseItems(values, state, parse);
    }

    private Option<ImmutableArray<T>> ParseObjectItems<T, TState>(
        Option<ImmutableArray<JsonSyntax>> items,
        TState state,
        Func<JsonObjectSyntax, TState, Option<T>> parse) =>
        ParseItems(
            items,
            (Parser: this, State: state, Parse: parse),
            static (item, state) => state.Parser.AsObject(item).Then(
                (state.State, state.Parse),
                static (@object, state) => state.Parse(@object, state.State)));

    private Option<ImmutableArray<Field>> Fields(JsonObjectSyntax syntax, SchemaName containingSchemaName) =>
        ParseObjectItems(
            GetRequiredProperty(syntax, AvroJsonKeys.Fields).Then(this, GetArrayItems),
            (Parser: this, ContainingSchemaName: containingSchemaName),
            static (field, state) => state.Parser.Field(field, state.ContainingSchemaName));

    private Option<Field> Field(JsonObjectSyntax syntax, SchemaName containingSchemaName)
    {
        if (!GetRequiredProperty(syntax, AvroJsonKeys.Name).Then(this, GetRequiredString).Then(static name => new FieldName(name)).TryGetValue(out var fieldName))
            return Option.None<Field>();

        var fieldType = GetRequiredProperty(syntax, AvroJsonKeys.Type).Then((this, containingSchemaName.Namespace), Schema);
        if (!fieldType.IsSome)
            return Option.None<Field>();

        fieldType = ResolveFieldType(fieldType.Value, fieldName, containingSchemaName, out var underlyingType, out var remarks);

        var documentation = GetOptionalNullableString(syntax, AvroJsonKeys.Doc);
        var aliases = GetOptionalArrayStrings(syntax, AvroJsonKeys.Aliases);
        var defaultJson = GetDefaultJson(syntax);
        var order = GetOptionalString(syntax, AvroJsonKeys.Order);
        var properties = Option.Some(syntax.GetSchemaProperties());


        return fieldName.With(fieldType).With(Option.Some(underlyingType)).With(documentation).With(aliases).With(defaultJson).With(order).With(properties).With(Option.Some(remarks))
            .Then(static result =>
            {
                var ((fieldName, fieldType, underlyingType, documentation, aliases, defaultJson, order, properties), remarks) = result;
                return new Field(
                    fieldName,
                    fieldType,
                    underlyingType,
                    documentation,
                    aliases,
                    defaultJson,
                    fieldType.GetValue(defaultJson),
                    order,
                    properties,
                    remarks);
            });
    }

    private Option<AvroSchema> Union(JsonArraySyntax syntax, string? containingNamespace)
    {
        return ParseItems(syntax.Items, (Parser: this, ContainingNamespace: containingNamespace), Schema)
            .Then(Options.UseNullableReferenceTypes, static AvroSchema (schemas, nullableReferences) => UnionSchema.Create(schemas, nullableReferences));
    }

    // TODO: Move protocol parsing into AvprParser.

    private Option<AvroSchema> Protocol(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (!GetProtocolName(syntax, containingNamespace).TryGetValue(out var schemaName) || !TryEnterSchemaScope(schemaName, out var scope))
            return Option.None<AvroSchema>();

        using (scope)
        {
            var types = ProtocolTypes(syntax, schemaName.Namespace);
            var messages = ProtocolMessages(syntax, schemaName.Namespace);
            var documentation = GetOptionalNullableString(syntax, AvroJsonKeys.Doc);
            var properties = Option.Some(syntax.GetProtocolProperties());

            var schema = schemaName.With(types).With(messages).With(documentation).With(properties)
                .Then(static AvroSchema ((SchemaName SchemaName, ImmutableArray<NamedSchema> Types, ImmutableArray<ProtocolMessage> Messages, string? Documentation, ImmutableSortedDictionary<string, JsonElement> Properties) args) =>
                    new ProtocolSchema(args.SchemaName, args.Documentation, args.Types, args.Messages, args.Properties));

            if (schema.IsSome)
                Declare((ProtocolSchema)schema.Value, syntax.SourceSpan);

            return schema;
        }
    }

    private Option<ImmutableArray<NamedSchema>> ProtocolTypes(JsonObjectSyntax syntax, string? containingNamespace) =>
        ParseItems(
            GetRequiredProperty(syntax, AvroJsonKeys.Types).Then(this, GetArrayItems),
            (Parser: this, ContainingNamespace: containingNamespace),
            static (item, state) => state.Parser.NamedSchema(item, state.ContainingNamespace).Then(static schema => (NamedSchema)schema));

    private Option<ImmutableArray<ProtocolMessage>> ProtocolMessages(JsonObjectSyntax syntax, string? containingNamespace) =>
        ParseItems(
            GetRequiredProperty(syntax, AvroJsonKeys.Messages).Then(this, static (property, parser) => parser.GetObjectProperties(property)),
            (Parser: this, ContainingNamespace: containingNamespace),
            static (message, state) => state.Parser.Message(message, state.ContainingNamespace));

    private Option<AvroSchema> NamedSchema(JsonSyntax syntax, string? containingNamespace)
    {
        var namedSchema = AsObject(syntax);
        if (!namedSchema.IsSome)
            return Option.None<AvroSchema>();

        var type = GetRequiredProperty(namedSchema.Value, AvroJsonKeys.Type).Then(this, GetRequiredString);
        if (!type.IsSome)
            return Option.None<AvroSchema>();

        return type.Value switch
        {
            AvroTypeNames.Enum => Enum(namedSchema.Value, containingNamespace),
            AvroTypeNames.Record => Record(namedSchema.Value, containingNamespace, isError: false),
            AvroTypeNames.Error => Record(namedSchema.Value, containingNamespace, isError: true),
            AvroTypeNames.Fixed => Fixed(namedSchema.Value, containingNamespace),
            _ => Invalid<AvroSchema>(AvroDiagnostic.UnknownSchemaType(namedSchema.Value, type.Value))
        };
    }

    private Option<ProtocolMessage> Message(JsonPropertySyntax message, string? containingNamespace)
    {
        var syntax = AsObject(message.Value);
        if (!syntax.IsSome)
            return Option.None<ProtocolMessage>();

        var parameters = ProtocolRequest(syntax.Value, containingNamespace);
        var response = ProtocolResponse(syntax.Value, containingNamespace);
        var errors = ProtocolErrors(syntax.Value, containingNamespace);
        var oneWay = GetOptionalBoolean(syntax.Value, AvroJsonKeys.OneWay);
        var documentation = GetOptionalNullableString(syntax.Value, AvroJsonKeys.Doc);
        var result = parameters.With(response).With(errors).With(oneWay).With(documentation);

        if (!result.IsSome)
            return Option.None<ProtocolMessage>();

        var (_, messageResponse, messageErrors, isOneWay, _) = result.Value;
        if (isOneWay is true && (messageResponse.Type.Type is not SchemaType.Null || !messageErrors.IsEmpty))
            return Invalid<ProtocolMessage>(AvroDiagnostic.InvalidOneWayMessage(message.Value, message.Name.Value));

        return result.Then(
            message.Name.Value,
            static (arguments, name) =>
            {
                var (parameters, response, errors, oneWay, documentation) = arguments;
                return new ProtocolMessage(name.ToValidName(), documentation, parameters, response, errors, oneWay);
            });
    }

    private Option<ImmutableArray<ProtocolRequestParameter>> ProtocolRequest(JsonObjectSyntax syntax, string? containingNamespace) =>
        ParseObjectItems(
            GetRequiredProperty(syntax, AvroJsonKeys.Request).Then(this, GetArrayItems),
            (Parser: this, ContainingNamespace: containingNamespace),
            static (parameter, state) => state.Parser.ProtocolRequestParameter(parameter, state.ContainingNamespace));

    private Option<ProtocolResponse> ProtocolResponse(JsonObjectSyntax syntax, string? containingNamespace) =>
        GetRequiredProperty(syntax, AvroJsonKeys.Response)
            .Then((Parser: this, ContainingNamespace: containingNamespace), Schema)
            .Then(static type => new ProtocolResponse(type, type is UnionSchema union ? union.UnderlyingSchema : type));

    private Option<ImmutableArray<AvroSchema>> ProtocolErrors(JsonObjectSyntax syntax, string? containingNamespace) =>
        ParseItems(
            GetOptionalArrayItems(syntax, AvroJsonKeys.Errors),
            (Parser: this, ContainingNamespace: containingNamespace),
            Schema);

    private Option<ProtocolRequestParameter> ProtocolRequestParameter(JsonObjectSyntax syntax, string? containingNamespace)
    {
        var name = GetRequiredProperty(syntax, AvroJsonKeys.Name).Then(this, GetRequiredString);
        var type = GetRequiredProperty(syntax, AvroJsonKeys.Type).Then((Parser: this, ContainingNamespace: containingNamespace), Schema);
        var documentation = GetOptionalNullableString(syntax, AvroJsonKeys.Doc);
        var defaultJson = GetDefaultJson(syntax);

        return name.With(type).With(documentation).With(defaultJson).Then(static result =>
        {
            var (name, type, documentation, defaultJson) = result;
            var underlyingType = type is UnionSchema union ? union.UnderlyingSchema : type;
            return new ProtocolRequestParameter(name.ToValidName(), type, underlyingType, documentation, defaultJson, type.GetValue(defaultJson));
        });
    }
}
