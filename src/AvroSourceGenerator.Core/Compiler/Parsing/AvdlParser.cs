using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Avdl;
using AvroSourceGenerator.Avdl.Annotations;
using AvroSourceGenerator.Avdl.Declarations;
using AvroSourceGenerator.Avdl.Types;
using AvroSourceGenerator.Compiler.Configuration;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler.Parsing;

internal sealed class AvdlParser(SyntaxTree syntaxTree, AvroParseOptions options, CancellationToken cancellationToken)
    : AvxxParser(options)
{
    public static (AvroFile File, SourceSpan RootSpan) ParseFile(SourceText source, AvroParseOptions options, CancellationToken cancellationToken) =>
        new AvdlParser(Parser.Parse(source, cancellationToken), options, cancellationToken).ParseFile();

    public (AvroFile File, SourceSpan RootSpan) ParseFile()
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sourceText = syntaxTree.SourceText;
        if (!syntaxTree.Diagnostics.IsEmpty)
            return (AvroFile.Invalid(sourceText, syntaxTree.Diagnostics, Options), sourceText.GetSourceSpan());

        // Multiplicity is validated while parsing, before accessing the singular directives.
        // Without a main schema, only a single protocol declaration is a complete document.
        if (syntaxTree.Document.SchemaDirective is null && syntaxTree.Document.Declarations is not [ProtocolDeclarationSyntax])
            return (AvroFile.Invalid(sourceText, [AvroDiagnostic.InvalidIdlDocument(syntaxTree.Document.GetSourceSpan())], Options), sourceText.GetSourceSpan());

        var rootSpan = syntaxTree.Document.SchemaDirective?.MainSchemaType is { } mainSchemaType
            ? mainSchemaType.GetSourceSpan()
            : syntaxTree.Document.GetSourceSpan();
        return (ParseCore(syntaxTree), rootSpan);
    }

    private AvroFile ParseCore(SyntaxTree syntaxTree)
    {
        var source = syntaxTree.SourceText;

        var imports = syntaxTree.Document.ImportDirectives
            .Concat(
                syntaxTree.Document.Declarations
                    .OfType<ProtocolDeclarationSyntax>()
                    .SelectMany(static protocol => protocol.Imports))
            .WithCancellation(cancellationToken)
            .Select(static import => new AvroImport(
                import.ImportTypeKeyword.SyntaxKind switch
                {
                    SyntaxKind.IdlKeyword => AvroImportKind.Idl,
                    SyntaxKind.ProtocolKeyword => AvroImportKind.Protocol,
                    SyntaxKind.SchemaKeyword => AvroImportKind.Schema,
                    _ => throw new InvalidOperationException("Unreachable: Unsupported Avro import kind."),
                },
                import.ImportPathLiteralToken.Value as string ?? string.Empty,
                import.ImportPathLiteralToken.SourceSpan))
            .ToImmutableArray();

        if (Document(syntaxTree) is not AvroSchema rootSchema)
            return AvroFile.Invalid(source, [.. Diagnostics], Options);

        return new AvroFile(
            source,
            rootSchema,
            [.. Declarations],
            [.. DeclarationSpans],
            GetReferences(),
            GetReferenceSpans(),
            GetDependencies(),
            imports,
            [.. Diagnostics],
            Options);
    }

    private Option<AvroSchema> Document(SyntaxTree syntaxTree)
    {
        var document = syntaxTree.Document;
        var containingNamespace = document.NamespaceDirective?.NamespaceName.FullName;
        var mainSchema = document.SchemaDirective?.MainSchemaType;
        if (mainSchema is null)
        {
            var protocol = (ProtocolDeclarationSyntax)document.Declarations[0];
            return Protocol(protocol, containingNamespace).Map<AvroSchema>(static protocol => protocol);
        }

        var isValid = true;
        foreach (var declaration in document.Declarations.WithCancellation(cancellationToken))
        {
            if (declaration is not ISchemaDeclarationSyntax schemaDeclaration)
            {
                Report(AvroDiagnostic.InvalidIdlDeclaration(declaration.GetSourceSpan()));
                isValid = false;
                continue;
            }

            isValid &= Schema(schemaDeclaration, containingNamespace) is AvroSchema;
        }

        var root = Type(mainSchema, containingNamespace);
        return isValid ? root : Option.None<AvroSchema>();
    }

    private Option<AvroSchema> Type(
        ITypeSyntax syntax,
        string? containingNamespace,
        ImmutableSortedDictionary<string, JsonElement>? properties = null,
        bool hasNonNullDefault = false)
    {
        properties ??= ImmutableSortedDictionary<string, JsonElement>.Empty;

        return syntax switch
        {
            AnnotatedTypeSyntax type => Annotated(type, containingNamespace, hasNonNullDefault),
            ArrayTypeSyntax type => Array(type, containingNamespace, properties),
            ILogicalTypeSyntax type => Logical(type),
            MapTypeSyntax type => Map(type, containingNamespace, properties),
            NamedTypeSyntax type => Named(type, containingNamespace),
            OptionalTypeSyntax type => Optional(type, containingNamespace, properties, hasNonNullDefault),
            PrimitiveTypeSyntax type => Primitive(type, properties),
            UnionTypeSyntax type => Union(type.Types, containingNamespace),
            _ => throw new InvalidOperationException($"Unreachable: Unsupported Avro IDL type syntax '{syntax.SyntaxKind}'."),
        };
    }

    private static Option<AvroSchema> Primitive(PrimitiveTypeSyntax syntax, ImmutableSortedDictionary<string, JsonElement> properties)
    {
        var schema = syntax.SyntaxKind switch
        {
            SyntaxKind.VoidType => AvroSchema.Null,
            SyntaxKind.NullType => AvroSchema.Null,
            SyntaxKind.BooleanType => AvroSchema.Boolean,
            SyntaxKind.IntType => AvroSchema.Int,
            SyntaxKind.LongType => AvroSchema.Long,
            SyntaxKind.FloatType => AvroSchema.Float,
            SyntaxKind.DoubleType => AvroSchema.Double,
            SyntaxKind.BytesType => AvroSchema.Bytes,
            SyntaxKind.StringType => AvroSchema.String,
            _ => throw new InvalidOperationException($"Unreachable: Unsupported Avro IDL primitive type '{syntax.TypeKeyword.SyntaxKind}'.")
        };
        return properties.IsEmpty ? schema : schema with { Properties = properties };
    }

    private Option<NamedSchema> Schema(ISchemaDeclarationSyntax declaration, string? containingNamespace)
    {
        var maybeSchema = declaration switch
        {
            EnumDeclarationSyntax syntax => Enum(syntax, containingNamespace),
            FixedDeclarationSyntax syntax => Fixed(syntax, containingNamespace),
            RecordDeclarationSyntax syntax => Record(syntax, containingNamespace),
            ErrorDeclarationSyntax syntax => Error(syntax, containingNamespace),
            _ => throw new InvalidOperationException($"Unreachable: Unsupported Avro IDL schema declaration '{declaration.SyntaxKind}'.")
        };

        if (!maybeSchema.TryGetValue(out NamedSchema? schema))
            return Option.None<NamedSchema>();

        if (schema is not FixedSchema fixedSchema || !declaration.Annotations.OfType<LogicalTypeAnnotationSyntax>().Any())
            return schema;

        if (!TryUnwrap(declaration.GetLogicalTypeName(), out var maybeLogicalType) || !maybeLogicalType.TryGetValue(out string? logicalType))
            return Option.None<NamedSchema>();

        // TODO: We can do better here...
        if (LogicalSchema.Create(logicalType, fixedSchema, Options.GenerationTarget) is not LogicalSchema)
            return fixedSchema;

        var annotated = fixedSchema with { Properties = fixedSchema.Properties.SetItem(AvroJsonKeys.LogicalType, JsonSerializer.SerializeToElement(logicalType)) };
        Replace(fixedSchema, annotated);
        return annotated;
    }

    private Option<AvroSchema> Logical(ILogicalTypeSyntax syntax)
    {
        if (syntax is DecimalLogicalTypeSyntax decimalSyntax)
        {
            var precision = decimalSyntax.PrecisionLiteralToken.Value is int precisionValue
                ? precisionValue
                : Report<int>(AvroDiagnostic.InvalidIdlDecimalPrecision(decimalSyntax.PrecisionLiteralToken.SourceSpan));
            var scale = decimalSyntax.ScaleLiteralToken.Value is int scaleValue
                ? scaleValue
                : Report<int>(AvroDiagnostic.InvalidIdlDecimalScale(decimalSyntax.ScaleLiteralToken.SourceSpan));
            if (precision.IsNone || scale.IsNone)
                return Option.None<AvroSchema>();

            var bytes = AvroSchema.Bytes with
            {
                Properties = ImmutableSortedDictionary.CreateRange(new Dictionary<string, JsonElement>
                {
                    ["precision"] = JsonSerializer.SerializeToElement(precision.GetValue()),
                    ["scale"] = JsonSerializer.SerializeToElement(scale.GetValue())
                })
            };

            return LogicalSchema.Create(LogicalTypeNames.Decimal, bytes, Options.GenerationTarget);
        }

        if (syntax is not LogicalTypeSyntax logical)
        {
            throw new InvalidOperationException($"Unreachable: Unsupported Avro IDL logical type syntax '{syntax.SyntaxKind}'.");
        }

        return logical.LogicalTypeNameKeyword.SyntaxKind switch
        {
            SyntaxKind.DateKeyword => LogicalSchema.Create(LogicalTypeNames.Date, AvroSchema.Int, Options.GenerationTarget),
            SyntaxKind.TimeMsKeyword => LogicalSchema.Create(LogicalTypeNames.TimeMillis, AvroSchema.Int, Options.GenerationTarget),
            SyntaxKind.TimestampMsKeyword => LogicalSchema.Create(LogicalTypeNames.TimestampMillis, AvroSchema.Long, Options.GenerationTarget),
            SyntaxKind.LocalTimestampMsKeyword => LogicalSchema.Create(LogicalTypeNames.LocalTimestampMillis, AvroSchema.Long, Options.GenerationTarget),
            SyntaxKind.UuidKeyword => LogicalSchema.Create(LogicalTypeNames.Uuid, AvroSchema.String, Options.GenerationTarget),
            _ => throw new InvalidOperationException($"Unreachable: Unsupported Avro IDL logical type '{logical.LogicalTypeNameKeyword.SyntaxKind}'.")
        };
    }

    private Option<AvroSchema> Annotated(AnnotatedTypeSyntax syntax, string? containingNamespace, bool hasNonNullDefault)
    {
        var logicalTypeName = Unwrap(syntax.GetLogicalTypeName());
        var properties = Unwrap(syntax.GetSchemaProperties());
        if (logicalTypeName.IsNone || properties.IsNone || Type(syntax.Type, containingNamespace, properties.GetValue(), hasNonNullDefault) is not AvroSchema underlyingSchema)
            return Option.None<AvroSchema>();

        return logicalTypeName.GetValue() switch
        {
            string logicalType => LogicalSchema.Create(logicalType, underlyingSchema, Options.GenerationTarget),
            _ => underlyingSchema
        };
    }

    private Option<AvroSchema> Named(NamedTypeSyntax syntax, string? containingNamespace) => syntax.Name.GetSchemaName(containingNamespace: null) switch
    {
        SchemaName name => Reference(name, containingNamespace, syntax.Name.GetSourceSpan()),
        AvroDiagnostic => Report<AvroSchema>(AvroDiagnostic.InvalidSchemaReference(syntax.Name.GetSourceSpan(), syntax.Name.FullName))
    };

    private Option<AvroSchema> Array(ArrayTypeSyntax syntax, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement> properties) => Type(syntax.ItemType, containingNamespace) switch
    {
        AvroSchema items => new ArraySchema(items, Documentation: null, properties),
        _ => Option.None<AvroSchema>()
    };

    private Option<AvroSchema> Map(MapTypeSyntax syntax, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement> properties) => Type(syntax.ValueType, containingNamespace) switch
    {
        AvroSchema values => new MapSchema(values, Documentation: null, properties),
        _ => Option.None<AvroSchema>()
    };

    private Option<NamedSchema> Enum(EnumDeclarationSyntax syntax, string? containingNamespace)
    {
        if (!TryUnwrap(syntax.GetSchemaName(containingNamespace), out var schemaName))
            return Option.None<NamedSchema>();
        if (IsInRecursionScope(schemaName))
            return Report<NamedSchema>(AvroDiagnostic.RecursiveDefinition(syntax.Name.GetSourceSpan(), schemaName));

        using var recursionScope = EnterRecursionScope(schemaName);
        var documentation = Unwrap(syntax.GetDocumentation());
        var aliases = Unwrap(syntax.GetAliases());
        var symbols = GetEnumSymbols(syntax.Symbols, schemaName);
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

    private Option<ImmutableArray<MemberName>> GetEnumSymbols(SeparatedSyntaxList<SimpleNameSyntax> syntaxlist, SchemaName schemaName)
    {
        var members = new HashSet<MemberName>();
        var valid = true;
        foreach (var symbol in syntaxlist.WithCancellation(cancellationToken))
        {
            var name = new MemberName(symbol.FullName);
            if (!members.Add(name))
            {
                valid = false;
                Report(AvroDiagnostic.DuplicateSymbol(symbol.GetSourceSpan(), name.SchemaName, schemaName));
            }
        }

        return valid ? members.ToImmutableArray() : Option.None<ImmutableArray<MemberName>>();
    }

    private Option<NamedSchema> Fixed(FixedDeclarationSyntax syntax, string? containingNamespace)
    {
        if (!TryUnwrap(syntax.GetSchemaName(containingNamespace), out var schemaName))
            return Option.None<NamedSchema>();
        if (IsInRecursionScope(schemaName))
            return Report<NamedSchema>(AvroDiagnostic.RecursiveDefinition(syntax.Name.GetSourceSpan(), schemaName));

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

    private Option<NamedSchema> Record(RecordDeclarationSyntax syntax, string? containingNamespace)
    {
        if (!TryUnwrap(syntax.GetSchemaName(containingNamespace), out var schemaName))
            return Option.None<NamedSchema>();
        if (IsInRecursionScope(schemaName))
            return Report<NamedSchema>(AvroDiagnostic.RecursiveDefinition(syntax.Name.GetSourceSpan(), schemaName));

        using var recursionScope = EnterRecursionScope(schemaName);
        var documentation = Unwrap(syntax.GetDocumentation());
        var aliases = Unwrap(syntax.GetAliases());
        var fields = Fields(syntax.Fields, schemaName);
        var properties = Unwrap(syntax.GetSchemaProperties());

        if (documentation.IsNone || aliases.IsNone || fields.IsNone || properties.IsNone)
            return Option.None<NamedSchema>();

        var recordSchema = new RecordSchema(schemaName, documentation.GetValue().GetValueOrDefault(), aliases.GetValue(), fields.GetValue(), properties.GetValue());
        Declare(recordSchema, syntax.GetSourceSpan());
        return recordSchema;
    }

    private Option<NamedSchema> Error(ErrorDeclarationSyntax syntax, string? containingNamespace)
    {
        if (!TryUnwrap(syntax.GetSchemaName(containingNamespace), out var schemaName))
            return Option.None<NamedSchema>();
        if (IsInRecursionScope(schemaName))
            return Report<NamedSchema>(AvroDiagnostic.RecursiveDefinition(syntax.Name.GetSourceSpan(), schemaName));

        using var recursionScope = EnterRecursionScope(schemaName);
        var documentation = Unwrap(syntax.GetDocumentation());
        var aliases = Unwrap(syntax.GetAliases());
        var fields = Fields(syntax.Fields, schemaName);
        var properties = Unwrap(syntax.GetSchemaProperties());

        if (documentation.IsNone || aliases.IsNone || fields.IsNone || properties.IsNone)
            return Option.None<NamedSchema>();

        var errorSchema = new ErrorSchema(schemaName, documentation.GetValue().GetValueOrDefault(), aliases.GetValue(), fields.GetValue(), properties.GetValue());
        Declare(errorSchema, syntax.GetSourceSpan());
        return errorSchema;
    }

    private Option<ImmutableArray<Field>> Fields(SyntaxList<FieldDeclarationSyntax> syntaxList, SchemaName containingSchemaName)
    {
        var fields = new HashSet<Field>(FieldByNameComparer.Instance);
        var valid = true;
        foreach (var syntax in syntaxList.WithCancellation(cancellationToken))
        {
            switch (Field(syntax, containingSchemaName))
            {
                case None:
                    return Option.None<ImmutableArray<Field>>();

                case Field field when !fields.Add(field):
                    valid = false;
                    Report(AvroDiagnostic.DuplicateField(syntax.Name.GetSourceSpan(), field.Name.SchemaName, containingSchemaName));
                    break;
            }
        }
        return valid ? Option.Some(fields.ToImmutableArray()) : Option.None<ImmutableArray<Field>>();
    }

    private Option<Field> Field(FieldDeclarationSyntax syntax, SchemaName containingSchemaName)
    {
        var name = Unwrap(syntax.GetMemberName());
        var defaultValue = Unwrap(syntax.DefaultValueClause.GetDefaultValue());
        var type = Type(syntax.Type, containingSchemaName.Namespace, hasNonNullDefault: defaultValue.IsNotNullOrNullLiteral);
        var documentation = Unwrap(syntax.GetDocumentation());
        var aliases = Unwrap(syntax.GetAliases());
        var order = Unwrap(syntax.GetOrder());
        var properties = Unwrap(syntax.GetSchemaProperties());
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

    private Option<AvroSchema> Union(IEnumerable<ITypeSyntax> items, string? containingNamespace, bool prependSystemError = false)
    {
        var valid = true;
        var branches = new HashSet<AvroSchema>(UnionBranchComparer.Instance);
        if (prependSystemError) branches.Add(AvroSchema.String);
        foreach (var item in items)
        {
            switch (Type(item, containingNamespace))
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

    private Option<AvroSchema> Optional(OptionalTypeSyntax syntax, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement> properties, bool hasNonNullDefault)
    {
        return Type(syntax.Type, containingNamespace, properties) switch
        {
            PrimitiveSchema { SchemaType: SchemaType.Null } => Report<AvroSchema>(AvroDiagnostic.DuplicateUnionBranch(syntax.Type.GetSourceSpan(), AvroTypeNames.Null)),
            UnionSchema => Report<AvroSchema>(AvroDiagnostic.NestedUnion(syntax.Type.GetSourceSpan())),
            AvroSchema schema => UnionSchema.Create(hasNonNullDefault ? [schema, AvroSchema.Null] : [AvroSchema.Null, schema], Options.LanguageFeatures.HasNullableReferenceTypes),
            None => Option.None<AvroSchema>(),
        };
    }

    private Option<ProtocolSchema> Protocol(ProtocolDeclarationSyntax syntax, string? containingNamespace)
    {
        if (!TryUnwrap(syntax.GetSchemaName(containingNamespace), out var schemaName))
            return Option.None<ProtocolSchema>();
        if (IsInRecursionScope(schemaName))
            return Report<ProtocolSchema>(AvroDiagnostic.RecursiveDefinition(syntax.Name.GetSourceSpan(), schemaName));

        using var recursionScope = EnterRecursionScope(schemaName);
        var types = ProtocolTypes(syntax.Types, schemaName.Namespace);
        var messages = ProtocolMessages(syntax.Messages, schemaName.Namespace);
        var documentation = Unwrap(syntax.GetDocumentation());
        var properties = Unwrap(syntax.GetProtocolProperties());

        if (types.IsNone || messages.IsNone || documentation.IsNone || properties.IsNone)
            return Option.None<ProtocolSchema>();

        var protocol = new ProtocolSchema(schemaName, documentation.GetValue().GetValueOrDefault(), types.GetValue(), messages.GetValue(), properties.GetValue());
        Declare(protocol, syntax.GetSourceSpan());
        return protocol;
    }

    private Option<ImmutableArray<NamedSchema>> ProtocolTypes(SyntaxList<ISchemaDeclarationSyntax> syntaxList, string? containingNamespace)
    {
        var schemas = ImmutableArray.CreateBuilder<NamedSchema>();
        foreach (var declaration in syntaxList.WithCancellation(cancellationToken))
        {
            if (Schema(declaration, containingNamespace) is not NamedSchema namedSchema)
                return Option.None<ImmutableArray<NamedSchema>>();
            schemas.Add(namedSchema);
        }
        return schemas.DrainToImmutable();
    }

    private Option<ImmutableArray<ProtocolMessage>> ProtocolMessages(SyntaxList<MessageDeclarationSyntax> syntaxList, string? containingNamespace)
    {
        var messages = ImmutableArray.CreateBuilder<ProtocolMessage>();
        foreach (var syntax in syntaxList.WithCancellation(cancellationToken))
        {
            if (Message(syntax, containingNamespace) is not ProtocolMessage message)
                return Option.None<ImmutableArray<ProtocolMessage>>();
            messages.Add(message);
        }
        return messages.DrainToImmutable();
    }

    private Option<ProtocolMessage> Message(MessageDeclarationSyntax syntax, string? containingNamespace)
    {
        var methodName = new MemberName(syntax.Name.FullName);
        var parameters = ProtocolRequest(syntax.Parameters, containingNamespace);
        var response = ProtocolResponse(syntax.Type, containingNamespace);
        var errors = ProtocolErrors(syntax.ThrowsErrorClause, containingNamespace);
        var oneWay = syntax.OneWayClause is not null ? true : default(bool?);
        var documentation = Unwrap(syntax.GetDocumentation());
        if (parameters.IsNone || response.IsNone || errors.IsNone || documentation.IsNone)
            return Option.None<ProtocolMessage>();

        var hasDeclaredErrors = errors.GetValue().GetValueOrDefault() is { } errorSchema &&
            errorSchema is not UnionSchema { Schemas: [{ SchemaType: SchemaType.String }] };
        if (oneWay is true && (response.GetValue().Type.SchemaType is not SchemaType.Null || hasDeclaredErrors))
            return Report<ProtocolMessage>(AvroDiagnostic.InvalidIdlOneWayMessage(syntax.OneWayClause!.OneWayKeyword.SourceSpan, syntax.Name.FullName));

        return new ProtocolMessage(
            methodName,
            documentation.GetValue().GetValueOrDefault(),
            parameters.GetValue(),
            response.GetValue(),
            errors.GetValue().GetValueOrDefault(),
            oneWay);
    }

    private Option<ImmutableArray<ProtocolRequestParameter>> ProtocolRequest(SeparatedSyntaxList<ParameterDeclarationSyntax> syntaxList, string? containingNamespace)
    {
        // TODO: This should be a hashset and we should check duplicate names.
        var parameters = ImmutableArray.CreateBuilder<ProtocolRequestParameter>();
        foreach (var syntax in syntaxList.WithCancellation(cancellationToken))
        {
            switch (ProtocolRequestParameter(syntax, containingNamespace))
            {
                case None:
                    return Option.None<ImmutableArray<ProtocolRequestParameter>>();

                case ProtocolRequestParameter parameter:
                    parameters.Add(parameter);
                    break;
            }
        }
        return parameters.DrainToImmutable();
    }

    private Option<ProtocolRequestParameter> ProtocolRequestParameter(ParameterDeclarationSyntax syntax, string? containingNamespace)
    {
        var name = Unwrap(syntax.GetMemberName());
        var defaultValue = Unwrap(syntax.DefaultValueClause.GetDefaultValue());
        var type = Type(syntax.Type, containingNamespace, hasNonNullDefault: defaultValue.IsNotNullOrNullLiteral);
        var documentation = Unwrap(syntax.GetDocumentation());
        if (name.IsNone || defaultValue.IsNone || type.IsNone || documentation.IsNone)
            return Option.None<ProtocolRequestParameter>();

        return new ProtocolRequestParameter(
            name.GetValue(),
            type.GetValue(),
            type.GetValue().GetUnderlyingOrSelf(),
            documentation.GetValue().GetValueOrDefault(),
            defaultValue.GetValue().GetValueOrDefault());
    }

    private Option<ProtocolResponse> ProtocolResponse(ITypeSyntax syntax, string? containingNamespace) => Type(syntax, containingNamespace) switch
    {
        AvroSchema schema => new ProtocolResponse(schema, schema.GetUnderlyingOrSelf()),
        _ => Option.None<ProtocolResponse>()
    };

    private Option<Option<AvroSchema>> ProtocolErrors(ThrowsErrorClauseSyntax? syntax, string? containingNamespace) =>
        Option.Some(syntax is null ? Option.None<AvroSchema>() : Union(syntax.Errors, containingNamespace, prependSystemError: true));

    private T Invalid<T>(T @default, AvroDiagnostic diagnostic)
    {
        Report(diagnostic);
        return @default;
    }
}
