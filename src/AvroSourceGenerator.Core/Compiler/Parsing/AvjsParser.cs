using System.Collections.Immutable;
using AvroSourceGenerator.Compiler.Configuration;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Syntax.Json;
using AvroSourceGenerator.Syntax;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler.Parsing;

internal closed class AvjsParser(SourceText sourceText, AvroParseOptions options, CancellationToken cancellationToken) : AvxxParser(options)
{
    protected Option<AvroSchema> ParseSchema()
    {
        var tree = Parser.ParseJson(sourceText, cancellationToken);
        if (!tree.Diagnostics.IsEmpty)
        {
            Diagnostics.AddRange(tree.Diagnostics);
            return Option.None<AvroSchema>();
        }

        return Schema(tree.Root, containingNamespace: null);
    }

    private Option<AvroSchema> Schema(Result<JsonValueSyntax> value, string? containingNamespace, Func<JsonValueSyntax, AvroDiagnostic>? invalidShapeDiagnostic = null)
    {
        if (!TryUnwrap(value, out var syntax))
            return Option.None<AvroSchema>();

        return syntax switch
        {
            JsonObjectSyntax @object => Complex(@object, containingNamespace),
            JsonArraySyntax array => Union(array, containingNamespace),
            JsonStringSyntax @string => Named(@string, containingNamespace),
            _ => Report<AvroSchema>(invalidShapeDiagnostic?.Invoke(syntax) ?? AvroDiagnostic.SchemaExpected(syntax.GetSourceSpan()))
        };
    }

    private Option<AvroSchema> Complex(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (!TryUnwrap(syntax.GetRequiredProperty(AvroJsonKeys.Type), out var typeProperty))
            return Option.None<AvroSchema>();

        if (typeProperty.Value is not JsonStringSyntax @string)
            return Report<AvroSchema>(AvroDiagnostic.InvalidSchemaType(typeProperty.Value.GetSourceSpan()));

        var maybeSchema = @string.Value switch
        {
            AvroTypeNames.Array => Array(syntax, containingNamespace),
            AvroTypeNames.Map => Map(syntax, containingNamespace),
            AvroTypeNames.Enum => Enum(syntax, containingNamespace).Map(static named => (AvroSchema)named),
            AvroTypeNames.Fixed => Fixed(syntax, containingNamespace).Map(static named => (AvroSchema)named),
            AvroTypeNames.Record => Record(syntax, containingNamespace).Map(static named => (AvroSchema)named),
            AvroTypeNames.Error => Error(syntax, containingNamespace).Map(static named => (AvroSchema)named),
            _ => Named(@string, containingNamespace)
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

        return Logical(syntax, schema);
    }

    private Option<AvroSchema> Logical(JsonObjectSyntax syntax, AvroSchema schema)
    {
        if (!TryUnwrap(syntax.GetLogicalTypeName(), out var maybeLogicalType))
            return Option.None<AvroSchema>();
        if (maybeLogicalType is not string logicalType)
            return schema;

        if (logicalType is LogicalTypeNames.Decimal)
        {
            var precision = Unwrap(syntax.GetDecimalPrecision());
            var scale = Unwrap(syntax.GetDecimalScale());
            if (precision.IsNone || scale.IsNone)
                return Option.None<AvroSchema>();
        }

        var logicalSchema = LogicalSchema.Create(logicalType, schema, Options.GenerationTarget);
        if (logicalSchema is LogicalSchema) return logicalSchema;

        // The target ignores this logical type, so omit the annotation from its generated schema.
        var underlyingSchema = schema with { Properties = schema.Properties.Remove(AvroJsonKeys.LogicalType) };
        if (schema is TopLevelSchema declaration)
            Replace(declaration, (TopLevelSchema)underlyingSchema);

        return underlyingSchema;
    }

    private Option<AvroSchema> Named(JsonStringSyntax syntax, string? containingNamespace) => !TryUnwrap(syntax.AsSchemaReference(), out var schemaName)
        ? Option.None<AvroSchema>()
        : Reference(schemaName, containingNamespace, syntax.GetSourceSpan());

    private Option<AvroSchema> Array(JsonObjectSyntax syntax, string? containingNamespace)
    {
        var itemsSchema = Schema(syntax.GetItems(), containingNamespace,
            static value => AvroDiagnostic.InvalidItems(value.GetSourceSpan()));
        var documentation = Unwrap(syntax.GetDocumentation());
        var properties = Unwrap(syntax.GetSchemaProperties());
        if (itemsSchema.IsNone || documentation.IsNone || properties.IsNone)
            return Option.None<AvroSchema>();

        return new ArraySchema(itemsSchema.GetValue(), documentation.GetValue().GetValueOrDefault(), properties.GetValue());
    }

    private Option<AvroSchema> Map(JsonObjectSyntax syntax, string? containingNamespace)
    {
        var valuesSchema = Schema(syntax.GetValues(), containingNamespace,
            static value => AvroDiagnostic.InvalidValues(value.GetSourceSpan()));
        var documentation = Unwrap(syntax.GetDocumentation());
        var properties = Unwrap(syntax.GetSchemaProperties());
        if (valuesSchema.IsNone || documentation.IsNone || properties.IsNone)
            return Option.None<AvroSchema>();

        return new MapSchema(valuesSchema.GetValue(), documentation.GetValue().GetValueOrDefault(), properties.GetValue());
    }

    private Option<NamedSchema> Enum(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (!CanDeclareSchema(syntax, isProtocol: false, containingNamespace, out var schemaName))
            return Option.None<NamedSchema>();

        using var recursionScope = EnterRecursionScope(schemaName);
        var documentation = Unwrap(syntax.GetDocumentation());
        var aliases = Unwrap(syntax.GetAliases());
        var symbols = GetEnumSymbols(syntax, schemaName);
        var defaultSymbol = Unwrap(syntax.GetEnumDefault());
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
        Declare(enumSchema, syntax.GetSourceSpan());
        return enumSchema;
    }

    private Option<ImmutableArray<MemberName>> GetEnumSymbols(JsonObjectSyntax syntax, SchemaName schemaName)
    {
        var maybeSymbols = syntax.GetSymbols();
        if (!maybeSymbols.TryGetValue(out JsonArraySyntax? symbols))
            return Report<ImmutableArray<MemberName>>(maybeSymbols.GetError());

        var members = new HashSet<MemberName>();
        var valid = true;
        foreach (var symbol in symbols.Elements.WithCancellation(cancellationToken))
        {
            if (symbol.GetStringValue() is not string symbolName || !NameValidator.IsValidMemberName(symbolName))
                return Report<ImmutableArray<MemberName>>(AvroDiagnostic.InvalidSymbols(symbol.GetSourceSpan()));

            if (!members.Add(new MemberName(symbolName)))
            {
                valid = false;
                Report(AvroDiagnostic.DuplicateSymbol(symbol.GetSourceSpan(), symbolName, schemaName));
            }
        }

        return valid ? members.ToImmutableArray() : Option.None<ImmutableArray<MemberName>>();
    }

    private Option<NamedSchema> Fixed(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (!CanDeclareSchema(syntax, isProtocol: false, containingNamespace, out var schemaName))
            return Option.None<NamedSchema>();

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
        Declare(fixedSchema, syntax.GetSourceSpan());
        return fixedSchema;
    }

    private Option<NamedSchema> Record(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (!CanDeclareSchema(syntax, isProtocol: false, containingNamespace, out var schemaName))
            return Option.None<NamedSchema>();

        using var recursionScope = EnterRecursionScope(schemaName);
        var documentation = Unwrap(syntax.GetDocumentation());
        var aliases = Unwrap(syntax.GetAliases());
        var fields = Fields(syntax, schemaName);
        var properties = Unwrap(syntax.GetSchemaProperties());

        if (documentation.IsNone || aliases.IsNone || fields.IsNone || properties.IsNone)
            return Option.None<NamedSchema>();

        var recordSchema = new RecordSchema(schemaName, documentation.GetValue().GetValueOrDefault(), aliases.GetValue(), fields.GetValue(), properties.GetValue());
        Declare(recordSchema, syntax.GetSourceSpan());
        return recordSchema;
    }

    private Option<NamedSchema> Error(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (!CanDeclareSchema(syntax, isProtocol: false, containingNamespace, out var schemaName))
            return Option.None<NamedSchema>();

        using var recursionScope = EnterRecursionScope(schemaName);
        var documentation = Unwrap(syntax.GetDocumentation());
        var aliases = Unwrap(syntax.GetAliases());
        var fields = Fields(syntax, schemaName);
        var properties = Unwrap(syntax.GetSchemaProperties());

        if (documentation.IsNone || aliases.IsNone || fields.IsNone || properties.IsNone)
            return Option.None<NamedSchema>();

        var errorSchema = new ErrorSchema(schemaName, documentation.GetValue().GetValueOrDefault(), aliases.GetValue(), fields.GetValue(), properties.GetValue());
        Declare(errorSchema, syntax.GetSourceSpan());
        return errorSchema;
    }

    private Option<ImmutableArray<Field>> Fields(JsonObjectSyntax syntax, SchemaName containingSchemaName)
    {
        if (!TryUnwrap(syntax.GetFields(), out var fieldsArray))
            return Option.None<ImmutableArray<Field>>();

        var fields = new HashSet<Field>(FieldByNameComparer.Instance);
        var valid = true;
        foreach (var item in fieldsArray.Elements.WithCancellation(cancellationToken))
        {
            switch (Field(item, containingSchemaName))
            {
                case None:
                    return Option.None<ImmutableArray<Field>>();

                case Field field when !fields.Add(field):
                    valid = false;
                    Report(AvroDiagnostic.DuplicateField(((JsonObjectSyntax)item).Property(AvroJsonKeys.Name)!.Value.GetSourceSpan(), field.Name.SchemaName, containingSchemaName));
                    break;
            }
        }
        return valid ? Option.Some(fields.ToImmutableArray()) : Option.None<ImmutableArray<Field>>();
    }

    private Option<Field> Field(JsonValueSyntax syntax, SchemaName containingSchemaName)
    {
        if (syntax is not JsonObjectSyntax field)
            return Report<Field>(AvroDiagnostic.InvalidFields(syntax.GetSourceSpan()));

        var name = Unwrap(field.GetFieldName());
        var type = Schema(field.GetFieldType(), containingSchemaName.Namespace,
            static value => AvroDiagnostic.InvalidFieldType(value.GetSourceSpan()));
        var documentation = Unwrap(field.GetDocumentation());
        var aliases = Unwrap(field.GetAliases());
        var defaultValue = Unwrap(field.GetDefaultValue());
        var order = Unwrap(field.GetOrder());
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

    private Option<AvroSchema> Union(JsonArraySyntax syntax, string? containingNamespace, bool prependSystemError = false)
    {
        var valid = true;
        var branches = new HashSet<AvroSchema>(UnionBranchComparer.Instance);
        if (prependSystemError) branches.Add(AvroSchema.String);
        foreach (var item in syntax.Elements.WithCancellation(cancellationToken))
        {
            switch (Schema(item, containingNamespace))
            {
                case None:
                    valid = false;
                    break;

                case UnionSchema:
                    valid = false;
                    Report(AvroDiagnostic.NestedUnion(item.GetSourceSpan()));
                    break;

                case AvroSchema schema when !branches.Add(schema):
                    valid = false;
                    Report(AvroDiagnostic.DuplicateUnionBranch(item.GetSourceSpan(), schema.GetUnderlyingOrSelf().SchemaName.FullName));
                    break;
            }
        }

        return valid ? UnionSchema.Create([.. branches], Options.LanguageFeatures.HasNullableReferenceTypes) : Option.None<AvroSchema>();
    }

    protected Option<ProtocolSchema> ParseProtocol()
    {
        var tree = Parser.ParseJson(sourceText, cancellationToken);
        if (!tree.Diagnostics.IsEmpty)
        {
            Diagnostics.AddRange(tree.Diagnostics);
            return Option.None<ProtocolSchema>();
        }

        return Protocol(tree.Root, containingNamespace: null);
    }

    protected Option<ProtocolSchema> Protocol(JsonValueSyntax syntax, string? containingNamespace)
    {
        if (syntax is not JsonObjectSyntax @object || @object.Property(AvroJsonKeys.Protocol) is null)
            return Report<ProtocolSchema>(AvroDiagnostic.ProtocolExpected(syntax.GetSourceSpan()));
        if (!CanDeclareSchema(@object, isProtocol: true, containingNamespace, out var schemaName))
            return Option.None<ProtocolSchema>();

        using var recursionScope = EnterRecursionScope(schemaName);
        var types = ProtocolTypes(@object, schemaName.Namespace);
        var messages = ProtocolMessages(@object, schemaName.Namespace);
        var documentation = Unwrap(@object.GetDocumentation());
        var properties = Unwrap(@object.GetProtocolProperties());

        if (types.IsNone || messages.IsNone || documentation.IsNone || properties.IsNone)
            return Option.None<ProtocolSchema>();

        var protocol = new ProtocolSchema(schemaName, documentation.GetValue().GetValueOrDefault(), types.GetValue(), messages.GetValue(), properties.GetValue());
        Declare(protocol, @object.GetSourceSpan());
        return protocol;
    }

    private Option<ImmutableArray<NamedSchema>> ProtocolTypes(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (!TryUnwrap(syntax.GetTypes(), out var typesArray))
            return Option.None<ImmutableArray<NamedSchema>>();

        var types = ImmutableArray.CreateBuilder<NamedSchema>(typesArray.Elements.Length);
        foreach (var item in typesArray.Elements)
        {
            if (NamedSchema(item, containingNamespace) is not NamedSchema namedSchema)
                return Option.None<ImmutableArray<NamedSchema>>();
            types.Add(namedSchema);
        }
        return types.MoveToImmutable();
    }

    private Option<NamedSchema> NamedSchema(JsonValueSyntax syntax, string? containingNamespace)
    {
        if (syntax is not JsonObjectSyntax @object)
            return Report<NamedSchema>(AvroDiagnostic.InvalidTypes(syntax.GetSourceSpan()));
        if (!TryUnwrap(@object.GetRequiredProperty(AvroJsonKeys.Type), out var type))
            return Option.None<NamedSchema>();

        var maybeSchema = type.Value.GetStringValue().GetValueOrDefault() switch
        {
            AvroTypeNames.Enum => Enum(@object, containingNamespace),
            AvroTypeNames.Fixed => Fixed(@object, containingNamespace),
            AvroTypeNames.Record => Record(@object, containingNamespace),
            AvroTypeNames.Error => Error(@object, containingNamespace),
            _ => Report<NamedSchema>(AvroDiagnostic.InvalidProtocolDeclarationType(type.Value.GetSourceSpan()))
        };

        if (!maybeSchema.TryGetValue(out NamedSchema? schema))
            return Option.None<NamedSchema>();

        // Protocol types remain named declarations; supported logical annotations stay
        // on the underlying schema, while ignored annotations use the shared replacement path.
        return Logical(@object, schema).Map(static schema => (NamedSchema)(schema is LogicalSchema logical ? logical.UnderlyingSchema : schema));
    }

    private Option<ImmutableArray<ProtocolMessage>> ProtocolMessages(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (!TryUnwrap(syntax.GetMessages(), out var messagesObject))
            return Option.None<ImmutableArray<ProtocolMessage>>();

        var messages = ImmutableArray.CreateBuilder<ProtocolMessage>(messagesObject.Properties.Length);
        foreach (var property in messagesObject.Properties.WithCancellation(cancellationToken))
        {
            if (Message(property, containingNamespace) is not ProtocolMessage message)
                return Option.None<ImmutableArray<ProtocolMessage>>();
            messages.Add(message);
        }

        return messages.MoveToImmutable();
    }

    private Option<ProtocolMessage> Message(JsonPropertySyntax property, string? containingNamespace)
    {
        if (property.Value is not JsonObjectSyntax message)
            return Report<ProtocolMessage>(AvroDiagnostic.InvalidMessages(property.Value.GetSourceSpan()));

        var methodName = new MemberName(property.Name.ValueText);
        var parameters = ProtocolRequest(message, containingNamespace);
        var response = ProtocolResponse(message, containingNamespace);
        var errors = ProtocolErrors(message, containingNamespace);
        var oneWay = Unwrap(message.GetOneWay());
        var documentation = Unwrap(message.GetDocumentation());
        if (parameters.IsNone || response.IsNone || errors.IsNone || oneWay.IsNone || documentation.IsNone)
            return Option.None<ProtocolMessage>();

        if (oneWay.GetValue() is true && !AllowsOneWay(response.GetValue().Type, errors.GetValue().GetValueOrDefault()))
            return Report<ProtocolMessage>(AvroDiagnostic.InvalidOneWayMessage(message.Property(AvroJsonKeys.OneWay)!.Value.GetSourceSpan(), property.Name.ValueText));

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
        if (!TryUnwrap(syntax.GetRequest(), out var request))
            return Option.None<ImmutableArray<ProtocolRequestParameter>>();

        // TODO: This should be a hashset and we should check duplicate names.
        var parameters = ImmutableArray.CreateBuilder<ProtocolRequestParameter>(request.Elements.Length);
        foreach (var item in request.Elements.WithCancellation(cancellationToken))
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

    private Option<ProtocolRequestParameter> ProtocolRequestParameter(JsonValueSyntax syntax, string? containingNamespace)
    {
        if (syntax is not JsonObjectSyntax parameter)
            return Report<ProtocolRequestParameter>(AvroDiagnostic.InvalidRequest(syntax.GetSourceSpan()));

        var name = Unwrap(parameter.GetParameterName());
        var type = Schema(parameter.GetParameterType(), containingNamespace,
            static value => AvroDiagnostic.InvalidParameterType(value.GetSourceSpan()));
        var documentation = Unwrap(parameter.GetDocumentation());
        var defaultValue = Unwrap(parameter.GetDefaultValue());
        if (name.IsNone || type.IsNone || documentation.IsNone || defaultValue.IsNone)
            return Option.None<ProtocolRequestParameter>();

        return new ProtocolRequestParameter(
            name.GetValue(),
            type.GetValue(),
            type.GetValue().GetUnderlyingOrSelf(),
            documentation.GetValue().GetValueOrDefault(),
            defaultValue.GetValue().GetValueOrDefault());
    }

    private Option<ProtocolResponse> ProtocolResponse(JsonObjectSyntax syntax, string? containingNamespace) =>
        Schema(syntax.GetResponse(), containingNamespace,
            static value => AvroDiagnostic.InvalidResponse(value.GetSourceSpan()))
            .Map(static type => new ProtocolResponse(type, type.GetUnderlyingOrSelf()));

    private Option<Option<AvroSchema>> ProtocolErrors(JsonObjectSyntax syntax, string? containingNamespace)
    {
        if (!TryUnwrap(syntax.GetErrors(), out var errors))
            return Option.None<Option<AvroSchema>>();
        return errors is JsonArraySyntax array
            ? Union(array, containingNamespace, prependSystemError: true).Map(Option.Some)
            : Option.Some(Option.None<AvroSchema>());
    }

    private bool CanDeclareSchema(JsonObjectSyntax syntax, bool isProtocol, string? containingNamespace, out SchemaName schemaName)
    {
        schemaName = default;
        if (!TryUnwrap(syntax.GetRequiredProperty(isProtocol ? AvroJsonKeys.Protocol : AvroJsonKeys.Name), out var nameProperty))
            return false;
        if (!TryUnwrap(nameProperty.GetValueAsSchemaName(), out schemaName))
            return false;

        if (!schemaName.IsQualified)
        {
            if (!TryUnwrap(syntax.GetNamespace(), out var ns))
                return false;
            schemaName = schemaName.ResolveIn(ns.GetValueOrDefault() ?? containingNamespace);
        }
        if (!IsInRecursionScope(schemaName))
            return true;

        Report(AvroDiagnostic.RecursiveDefinition(nameProperty.Value.GetSourceSpan(), schemaName));
        return false;
    }
}
