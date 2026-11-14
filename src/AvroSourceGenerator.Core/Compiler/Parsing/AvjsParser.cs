using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Avjs;
using AvroSourceGenerator.Compiler.Configuration;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler.Parsing;

internal closed class AvjsParser(SourceText sourceText, AvroParseOptions options, CancellationToken cancellationToken) : AvxxParser(options)
{
    protected Option<AvroSchema> ParseSchema() => ParseRootSyntax(sourceText, cancellationToken) switch
    {
        JsonSyntax root => Schema(root, containingNamespace: null),
        AvroDiagnostic diagnostic => Report<AvroSchema>(diagnostic)
    };

    protected Option<AvroSchema> Schema(Option<JsonSyntax> maybeSyntax, string? containingNamespace)
    {
        if (!maybeSyntax.TryGetValue(out JsonSyntax? syntax))
            return Option.None<AvroSchema>();
        return syntax switch
        {
            JsonValueSyntax { TokenType: JsonTokenType.String } value => Named(value, containingNamespace),
            JsonObjectSyntax @object when @object.Property(AvroJsonKeys.Type) is not null => Complex(@object, containingNamespace),
            JsonArraySyntax array => Union(array, containingNamespace, prependSystemError: false),
            _ => Report<AvroSchema>(AvroDiagnostic.InvalidProperty(syntax))
        };
    }

    private Option<AvroSchema> Complex(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (!TryUnwrap(syntax.GetRequiredValue(AvroJsonKeys.Type), out var type) || !TryUnwrap(type.AsString(), out var typeName))
            return Option.None<AvroSchema>();

        var maybeSchema = typeName switch
        {
            AvroTypeNames.Array => Array(syntax, containingNamespace),
            AvroTypeNames.Map => Map(syntax, containingNamespace),
            AvroTypeNames.Enum => Enum(syntax, containingNamespace).Map(static named => (AvroSchema)named),
            AvroTypeNames.Fixed => Fixed(syntax, containingNamespace).Map(static named => (AvroSchema)named),
            AvroTypeNames.Record => Record(syntax, containingNamespace).Map(static named => (AvroSchema)named),
            AvroTypeNames.Error => Error(syntax, containingNamespace).Map(static named => (AvroSchema)named),
            _ => Named(type, containingNamespace)
        };

        if (!maybeSchema.TryGetValue(out AvroSchema? schema))
            return Option.None<AvroSchema>();

        if (schema is PrimitiveSchema primitive)
        {
            if (!TryUnwrap(syntax.GetDocumentation(), out var documentation) || !TryUnwrap(syntax.GetSchemaProperties(), out var properties))
                return Option.None<AvroSchema>();

            if (documentation.IsSome || !properties.IsEmpty)
            {
                schema = schema with
                {
                    Documentation = documentation.GetValueOrDefault(),
                    Properties = properties
                };
            }
        }

        if (!TryUnwrap(syntax.GetLogicalTypeName(), out var maybeLogicalType))
            return Option.None<AvroSchema>();

        return maybeLogicalType is string logicalType
            ? Logical(logicalType, schema)
            : schema;
    }

    private Option<AvroSchema> Logical(string logicalType, AvroSchema schema)
    {
        var logicalSchema = LogicalSchema.Create(logicalType, schema, Options.GenerationTarget);
        if (logicalSchema is LogicalSchema) return logicalSchema;

        // The target ignores this logical type, so omit the annotation from its generated schema.
        var underlyingSchema = schema with { Properties = schema.Properties.Remove(AvroJsonKeys.LogicalType) };
        if (schema is TopLevelSchema declaration)
            Replace(declaration, (TopLevelSchema)underlyingSchema);

        return underlyingSchema;
    }

    private Option<AvroSchema> Named(JsonValueSyntax syntax, string? containingNamespace)
    {
        if (!TryUnwrap(syntax.AsSchemaName(containingNamespace: null), out var schemaName))
            return Option.None<AvroSchema>();

        return Reference(schemaName, containingNamespace, syntax.SourceSpan);
    }

    private Option<AvroSchema> Array(JsonObjectSyntax syntax, string? containingNamespace)
    {
        var itemsSchema = Schema(Unwrap(syntax.GetRequiredProperty(AvroJsonKeys.Items).Map(static property => property.Value)), containingNamespace);
        var documentation = Unwrap(syntax.GetDocumentation());
        var properties = Unwrap(syntax.GetSchemaProperties());
        if (itemsSchema.IsNone || documentation.IsNone || properties.IsNone)
            return Option.None<AvroSchema>();

        return new ArraySchema(itemsSchema.GetValue(), documentation.GetValue().GetValueOrDefault(), properties.GetValue());
    }

    private Option<AvroSchema> Map(JsonObjectSyntax syntax, string? containingNamespace)
    {
        var valueSchema = Schema(Unwrap(syntax.GetRequiredProperty(AvroJsonKeys.Values).Map(static property => property.Value)), containingNamespace);
        var documentation = Unwrap(syntax.GetDocumentation());
        var properties = Unwrap(syntax.GetSchemaProperties());
        if (valueSchema.IsNone || documentation.IsNone || properties.IsNone)
            return Option.None<AvroSchema>();

        return new MapSchema(valueSchema.GetValue(), documentation.GetValue().GetValueOrDefault(), properties.GetValue());
    }

    private Option<NamedSchema> Enum(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (!TryUnwrap(syntax.GetSchemaName(containingNamespace), out var schemaName))
            return Option.None<NamedSchema>();
        if (IsInRecursionScope(schemaName))
            return Report<NamedSchema>(AvroDiagnostic.RecursiveDefinition(syntax.Property(AvroJsonKeys.Name)!.Value.SourceSpan, schemaName));

        using var recursionScope = EnterRecursionScope(schemaName);
        var documentation = Unwrap(syntax.GetDocumentation());
        var aliases = Unwrap(syntax.GetAliases());
        var symbols = GetEnumSymbols(syntax, schemaName);
        var defaultSymbol = Unwrap(syntax.GetOptionalNullableString(AvroJsonKeys.Default));
        var properties = Unwrap(syntax.GetSchemaProperties());

        if (documentation.IsNone || aliases.IsNone || symbols.IsNone || defaultSymbol.IsNone || properties.IsNone)
            return Option.None<NamedSchema>();

        var enumSchema = new EnumSchema(
            schemaName,
            documentation.GetValue().GetValueOrDefault(),
            aliases.GetValue(),
            symbols.GetValue(),
            defaultSymbol.GetValue().GetValueOrDefault(),
            properties.GetValue());
        Declare(enumSchema, syntax.SourceSpan);
        return enumSchema;
    }

    private Option<ImmutableArray<MemberName>> GetEnumSymbols(JsonObjectSyntax syntax, SchemaName schemaName)
    {
        var maybeSymbols = syntax.GetRequiredArray(AvroJsonKeys.Symbols);
        if (!maybeSymbols.TryGetValue(out JsonArraySyntax? symbols))
            return Report<ImmutableArray<MemberName>>(maybeSymbols.GetError());

        var members = new HashSet<MemberName>();
        var valid = true;
        foreach (var symbol in symbols.Items.WithCancellation(cancellationToken))
        {
            switch (symbol.AsValue().Bind(static value => value.AsMemberName()))
            {
                case AvroDiagnostic diagnostic:
                    return Report<ImmutableArray<MemberName>>(diagnostic);

                case MemberName name when !members.Add(name):
                    valid = false;
                    Report(AvroDiagnostic.DuplicateSymbol(symbol.SourceSpan, name.SchemaName, schemaName));
                    break;
            }
        }

        return valid ? members.ToImmutableArray() : Option.None<ImmutableArray<MemberName>>();
    }

    private Option<NamedSchema> Fixed(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (!TryUnwrap(syntax.GetSchemaName(containingNamespace), out var schemaName))
            return Option.None<NamedSchema>();
        if (IsInRecursionScope(schemaName))
            return Report<NamedSchema>(AvroDiagnostic.RecursiveDefinition(syntax.Property(AvroJsonKeys.Name)!.Value.SourceSpan, schemaName));

        using var recursionScope = EnterRecursionScope(schemaName);
        var documentation = Unwrap(syntax.GetDocumentation());
        var aliases = Unwrap(syntax.GetAliases());
        var size = Unwrap(syntax.GetFixedSize());
        var properties = Unwrap(syntax.GetSchemaProperties());

        if (documentation.IsNone || aliases.IsNone || size.IsNone || properties.IsNone)
            return Option.None<NamedSchema>();

        var fixedSchema = new FixedSchema(schemaName, documentation.GetValue().GetValueOrDefault(), aliases.GetValue(), size.GetValue(), properties.GetValue())
        {
            CSharpName = Options.GenerationTarget is GenerationTarget.Apache
                ? CSharpName.FromSchemaName(schemaName)
                : AvroSchema.Bytes.CSharpName
        };
        Declare(fixedSchema, syntax.SourceSpan);
        return fixedSchema;
    }

    private Option<NamedSchema> Record(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (!TryUnwrap(syntax.GetSchemaName(containingNamespace), out var schemaName))
            return Option.None<NamedSchema>();
        if (IsInRecursionScope(schemaName))
            return Report<NamedSchema>(AvroDiagnostic.RecursiveDefinition(syntax.Property(AvroJsonKeys.Name)!.Value.SourceSpan, schemaName));

        using var recursionScope = EnterRecursionScope(schemaName);
        var documentation = Unwrap(syntax.GetDocumentation());
        var aliases = Unwrap(syntax.GetAliases());
        var fields = Fields(syntax, schemaName);
        var properties = Unwrap(syntax.GetSchemaProperties());

        if (documentation.IsNone || aliases.IsNone || fields.IsNone || properties.IsNone)
            return Option.None<NamedSchema>();

        var recordSchema = new RecordSchema(schemaName, documentation.GetValue().GetValueOrDefault(), aliases.GetValue(), fields.GetValue(), properties.GetValue());
        Declare(recordSchema, syntax.SourceSpan);
        return recordSchema;
    }

    private Option<NamedSchema> Error(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (!TryUnwrap(syntax.GetSchemaName(containingNamespace), out var schemaName))
            return Option.None<NamedSchema>();
        if (IsInRecursionScope(schemaName))
            return Report<NamedSchema>(AvroDiagnostic.RecursiveDefinition(syntax.Property(AvroJsonKeys.Name)!.Value.SourceSpan, schemaName));

        using var recursionScope = EnterRecursionScope(schemaName);
        var documentation = Unwrap(syntax.GetDocumentation());
        var aliases = Unwrap(syntax.GetAliases());
        var fields = Fields(syntax, schemaName);
        var properties = Unwrap(syntax.GetSchemaProperties());

        if (documentation.IsNone || aliases.IsNone || fields.IsNone || properties.IsNone)
            return Option.None<NamedSchema>();

        var errorSchema = new ErrorSchema(schemaName, documentation.GetValue().GetValueOrDefault(), aliases.GetValue(), fields.GetValue(), properties.GetValue());
        Declare(errorSchema, syntax.SourceSpan);
        return errorSchema;
    }

    private Option<ImmutableArray<Field>> Fields(JsonObjectSyntax syntax, SchemaName containingSchemaName)
    {
        if (!TryUnwrap(syntax.GetRequiredArray(AvroJsonKeys.Fields), out var array))
            return Option.None<ImmutableArray<Field>>();

        var fields = new HashSet<Field>(FieldByNameComparer.Instance);
        var valid = true;
        foreach (var item in array.Items.WithCancellation(cancellationToken))
        {
            switch (Field(item, containingSchemaName))
            {
                case None:
                    return Option.None<ImmutableArray<Field>>();

                case Field field when !fields.Add(field):
                    valid = false;
                    Report(AvroDiagnostic.DuplicateField(((JsonObjectSyntax)item).Property(AvroJsonKeys.Name)!.Value.SourceSpan, field.Name.SchemaName, containingSchemaName));
                    break;
            }
        }
        return valid ? Option.Some(fields.ToImmutableArray()) : Option.None<ImmutableArray<Field>>();
    }

    private Option<Field> Field(JsonSyntax syntax, SchemaName containingSchemaName)
    {
        if (!TryUnwrap(syntax.AsObject(), out var field))
            return Option.None<Field>();

        var name = Unwrap(field.GetMemberName());
        var type = Schema(Unwrap(field.GetRequiredProperty(AvroJsonKeys.Type).Map(static property => property.Value)), containingSchemaName.Namespace);
        var documentation = Unwrap(field.GetDocumentation());
        var aliases = Unwrap(field.GetAliases());
        var defaultValue = Unwrap(field.GetDefaultValue());
        var order = Unwrap(field.GetOptionalProperty(AvroJsonKeys.Order).Traverse(static order => order.GetRequiredString()));
        var properties = Unwrap(field.GetSchemaProperties());
        if (name.IsNone || type.IsNone || documentation.IsNone || aliases.IsNone || defaultValue.IsNone || order.IsNone || properties.IsNone)
            return Option.None<Field>();

        var fieldType = ResolveFieldType(type.GetValue(), name.GetValue(), containingSchemaName, out var underlyingType, out var remarks);
        return new Field(
            name.GetValue(),
            fieldType,
            underlyingType,
            documentation.GetValue().GetValueOrDefault(),
            aliases.GetValue(),
            defaultValue.GetValue().GetValueOrDefault(),
            order.GetValue().GetValueOrDefault(),
            properties.GetValue(),
            remarks);
    }

    private Option<AvroSchema> Union(JsonArraySyntax syntax, string? containingNamespace, bool prependSystemError)
    {
        var valid = true;
        var branches = new HashSet<AvroSchema>(UnionBranchComparer.Instance);
        if (prependSystemError) branches.Add(AvroSchema.String);
        foreach (var item in syntax.Items)
        {
            switch (Schema(item, containingNamespace))
            {
                case None:
                    valid = false;
                    break;

                case UnionSchema:
                    valid = false;
                    Report(AvroDiagnostic.NestedUnion(item.SourceSpan));
                    break;

                case AvroSchema schema when !branches.Add(schema):
                    valid = false;
                    Report(AvroDiagnostic.DuplicateUnionBranch(item.SourceSpan, schema.GetUnderlyingOrSelf().SchemaName.FullName));
                    break;
            }
        }

        return valid ? UnionSchema.Create([.. branches], Options.LanguageFeatures.HasNullableReferenceTypes) : Option.None<AvroSchema>();
    }

    protected Option<ProtocolSchema> ParseProtocol() => ParseRootSyntax(sourceText, cancellationToken) switch
    {
        JsonSyntax root => Protocol(root, containingNamespace: null),
        AvroDiagnostic diagnostic => Report<ProtocolSchema>(diagnostic)
    };

    protected Option<ProtocolSchema> Protocol(JsonSyntax syntax, string? containingNamespace)
    {
        if (syntax is not JsonObjectSyntax @object || @object.Property(AvroJsonKeys.Protocol) is null)
            return Report<ProtocolSchema>(AvroDiagnostic.ProtocolExpected(syntax.SourceSpan));
        if (!TryUnwrap(@object.GetProtocolName(containingNamespace), out var schemaName))
            return Option.None<ProtocolSchema>();
        if (IsInRecursionScope(schemaName))
            return Report<ProtocolSchema>(AvroDiagnostic.RecursiveDefinition(@object.Property(AvroJsonKeys.Protocol)!.Value.SourceSpan, schemaName));

        using var recursionScope = EnterRecursionScope(schemaName);
        var types = ProtocolTypes(@object, schemaName.Namespace);
        var messages = ProtocolMessages(@object, schemaName.Namespace);
        var documentation = Unwrap(@object.GetDocumentation());
        var properties = Unwrap(@object.GetProtocolProperties());

        if (types.IsNone || messages.IsNone || documentation.IsNone || properties.IsNone)
            return Option.None<ProtocolSchema>();

        var protocol = new ProtocolSchema(schemaName, documentation.GetValue().GetValueOrDefault(), types.GetValue(), messages.GetValue(), properties.GetValue());
        Declare(protocol, @object.SourceSpan);
        return protocol;
    }

    private Option<ImmutableArray<NamedSchema>> ProtocolTypes(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (!TryUnwrap(syntax.GetRequiredArray(AvroJsonKeys.Types), out var array))
            return Option.None<ImmutableArray<NamedSchema>>();

        var types = ImmutableArray.CreateBuilder<NamedSchema>(array.Items.Length);
        foreach (var item in array.Items)
        {
            if (NamedSchema(item, containingNamespace) is not NamedSchema namedSchema)
                return Option.None<ImmutableArray<NamedSchema>>();
            types.Add(namedSchema);
        }
        return types.MoveToImmutable();
    }

    private Option<NamedSchema> NamedSchema(JsonSyntax syntax, string? containingNamespace)
    {
        if (!TryUnwrap(syntax.AsObject(), out var @object) || !TryUnwrap(@object.GetRequiredString(AvroJsonKeys.Type), out var type))
            return Option.None<NamedSchema>();

        return type switch
        {
            AvroTypeNames.Enum => Enum(@object, containingNamespace),
            AvroTypeNames.Fixed => Fixed(@object, containingNamespace),
            AvroTypeNames.Record => Record(@object, containingNamespace),
            AvroTypeNames.Error => Error(@object, containingNamespace),
            _ => Report<NamedSchema>(AvroDiagnostic.InvalidProperty(@object.Property(AvroJsonKeys.Type)!))
        };
    }

    private Option<ImmutableArray<ProtocolMessage>> ProtocolMessages(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (!TryUnwrap(syntax.GetRequiredObject(AvroJsonKeys.Messages), out var @object))
            return Option.None<ImmutableArray<ProtocolMessage>>();

        var messages = ImmutableArray.CreateBuilder<ProtocolMessage>(@object.Properties.Length);
        foreach (var property in @object.Properties.WithCancellation(cancellationToken))
        {
            if (Message(property, containingNamespace) is not ProtocolMessage message)
                return Option.None<ImmutableArray<ProtocolMessage>>();
            messages.Add(message);
        }

        return messages.MoveToImmutable();
    }

    private Option<ProtocolMessage> Message(JsonPropertySyntax property, string? containingNamespace)
    {
        if (!TryUnwrap(property.Value.AsObject(), out var message))
            return Option.None<ProtocolMessage>();

        var methodName = new MemberName(property.Name.Value);
        var parameters = ProtocolRequest(message, containingNamespace);
        var response = ProtocolResponse(message, containingNamespace);
        var errors = ProtocolErrors(message, containingNamespace);
        var oneWay = Unwrap(message.GetOptionalBoolean(AvroJsonKeys.OneWay));
        var documentation = Unwrap(message.GetDocumentation());
        if (parameters.IsNone || response.IsNone || errors.IsNone || oneWay.IsNone || documentation.IsNone)
            return Option.None<ProtocolMessage>();

        var hasDeclaredErrors = errors.GetValue().GetValueOrDefault() is { } errorSchema &&
            errorSchema is not UnionSchema { Schemas: [{ SchemaType: SchemaType.String }] };
        if (oneWay.GetValue() is true && (response.GetValue().Type.SchemaType is not SchemaType.Null || hasDeclaredErrors))
            return Report<ProtocolMessage>(AvroDiagnostic.InvalidOneWayMessage(property.Value, property.Name.Value));

        return new ProtocolMessage(
            methodName,
            documentation.GetValue().GetValueOrDefault(),
            parameters.GetValue(),
            response.GetValue(),
            errors.GetValue().GetValueOrDefault(),
            oneWay.GetValue().TryGetValue(out bool oneWayValue) ? oneWayValue : null);
    }

    private Option<ImmutableArray<ProtocolRequestParameter>> ProtocolRequest(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (!TryUnwrap(syntax.GetRequiredArray(AvroJsonKeys.Request), out var request))
            return Option.None<ImmutableArray<ProtocolRequestParameter>>();

        // TODO: This should be a hashset and we should check duplicate names.
        var parameters = ImmutableArray.CreateBuilder<ProtocolRequestParameter>(request.Items.Length);
        foreach (var item in request.Items.WithCancellation(cancellationToken))
        {
            switch (ProtocolRequestParameter(item, containingNamespace))
            {
                case None:
                    return Option.None<ImmutableArray<ProtocolRequestParameter>>();

                case ProtocolRequestParameter parameter:
                    parameters.Add(parameter);
                    break;
            }
        }
        return parameters.MoveToImmutable();
    }

    private Option<ProtocolRequestParameter> ProtocolRequestParameter(JsonSyntax syntax1, string? containingNamespace)
    {
        if (!TryUnwrap(syntax1.AsObject(), out var syntax))
            return Option.None<ProtocolRequestParameter>();

        var name = Unwrap(syntax.GetMemberName());
        var type = Schema(Unwrap(syntax.GetRequiredProperty(AvroJsonKeys.Type)).Map(static property => property.Value), containingNamespace);
        var documentation = Unwrap(syntax.GetDocumentation());
        var defaultValue = Unwrap(syntax.GetDefaultValue());
        if (name.IsNone || type.IsNone || documentation.IsNone || defaultValue.IsNone)
            return Option.None<ProtocolRequestParameter>();

        return new ProtocolRequestParameter(
            name.GetValue(),
            type.GetValue(),
            type.GetValue().GetUnderlyingOrSelf(),
            documentation.GetValue().GetValueOrDefault(),
            defaultValue.GetValue().GetValueOrDefault());
    }

    private Option<ProtocolResponse> ProtocolResponse(JsonObjectSyntax syntax, string? containingNamespace)
    {
        var response = Unwrap(syntax.GetRequiredProperty(AvroJsonKeys.Response)).Map(static property => property.Value);
        return Schema(response, containingNamespace) switch
        {
            AvroSchema schema => new ProtocolResponse(schema, schema.GetUnderlyingOrSelf()),
            _ => Option.None<ProtocolResponse>()
        };
    }

    private Option<Option<AvroSchema>> ProtocolErrors(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (!TryUnwrap(syntax.GetOptionalArray(AvroJsonKeys.Errors), out var maybeErrors))
            return Option.None<Option<AvroSchema>>();
        return maybeErrors switch
        {
            JsonArraySyntax errors => Union(errors, containingNamespace, prependSystemError: true).Map(Option.Some),
            None => Option.Some(Option.None<AvroSchema>())
        };
    }

    protected static Result<JsonSyntax> ParseRootSyntax(SourceText sourceText, CancellationToken cancellationToken)
    {
        var reader = new JsonReader(sourceText);
        try
        {
            if (!reader.Read())
            {
                return AvroDiagnostic.EmptyJson(sourceText.GetSourceSpan());
            }

            var root = reader.Parse(cancellationToken);

            if (reader.Read())
            {
                return AvroDiagnostic.TrailingJson(reader.CurrentSpan);
            }

            return root;
        }
        catch (JsonException ex)
        {
            var span = ex.LineNumber is null && ex.BytePositionInLine is null && !reader.CurrentSpan.IsNone
                ? reader.CurrentSpan
                : SourceSpan.FromException(sourceText, ex);
            return AvroDiagnostic.InvalidJson(span, ex);
        }
    }
}
