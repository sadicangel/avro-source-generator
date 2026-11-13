using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using AvroSourceGenerator.Avdl;
using AvroSourceGenerator.Avdl.Annotations;
using AvroSourceGenerator.Avdl.Declarations;
using AvroSourceGenerator.Avdl.Types;
using AvroSourceGenerator.Compiler.Configuration;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Schemas.Defaults;
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
        var rootSchema = Document(syntaxTree);
        if (rootSchema is null || Diagnostics.HasErrors) return AvroFile.Invalid(source, [.. Diagnostics], Options);

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

    private AvroSchema? Document(SyntaxTree syntaxTree)
    {
        var document = syntaxTree.Document;
        var containingNamespace = document.NamespaceDirective?.NamespaceName.FullName;
        var mainSchema = document.SchemaDirective?.MainSchemaType;
        if (mainSchema is null)
        {
            var protocol = (ProtocolDeclarationSyntax)document.Declarations[0];
            return Protocol(protocol, containingNamespace);
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

            isValid &= Schema(schemaDeclaration, containingNamespace) is not null;
        }

        var root = Type(mainSchema, containingNamespace);
        return isValid ? root : null;
    }

    private AvroSchema? Type(
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

    private AvroSchema? Annotated(AnnotatedTypeSyntax syntax, string? containingNamespace, bool hasNonNullDefault)
    {
        var tracker = TrackDiagnostics();
        var logicalTypeName = syntax.Annotations.OfType<LogicalTypeAnnotationSyntax>().LastOrDefault() is { } annotation
            ? GetString(annotation.JsonValue, AvroDiagnosticCode.InvalidIdlLogicalTypeAnnotation, required: true)
            : null;
        var properties = syntax.Annotations.GetProperties(ReservedSchemaProperties.IsReserved);
        var underlyingSchema = Type(syntax.Type, containingNamespace, properties, hasNonNullDefault);
        if (underlyingSchema is null || tracker.HasNewDiagnostics) return null;
        return logicalTypeName is not null
            ? LogicalSchema.Create(logicalTypeName, underlyingSchema, Options.GenerationTarget)
            : underlyingSchema;
    }

    private static PrimitiveSchema Primitive(PrimitiveTypeSyntax syntax, ImmutableSortedDictionary<string, JsonElement> properties)
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

    private AvroSchema? Array(ArrayTypeSyntax syntax, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement> properties)
    {
        var items = Type(syntax.ItemType, containingNamespace);
        return items is not null ? new ArraySchema(items, Documentation: null, properties) : null;
    }

    private AvroSchema? Map(MapTypeSyntax syntax, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement> properties)
    {
        var values = Type(syntax.ValueType, containingNamespace);
        return values is not null ? new MapSchema(values, Documentation: null, properties) : null;
    }

    private AvroSchema? Optional(OptionalTypeSyntax syntax, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement> properties, bool hasNonNullDefault)
    {
        var underlyingSchema = Type(syntax.Type, containingNamespace, properties);
        switch (underlyingSchema)
        {
            case null: return null;
            case PrimitiveSchema { SchemaType: SchemaType.Null }:
                Report(AvroDiagnostic.DuplicateUnionBranch(syntax.Type.GetSourceSpan(), AvroTypeNames.Null));
                return null;
            case UnionSchema:
                Report(AvroDiagnostic.NestedUnion(syntax.Type.GetSourceSpan()));
                return null;
        }

        ImmutableArray<AvroSchema> schemas = hasNonNullDefault
            ? [underlyingSchema, AvroSchema.Null]
            : [AvroSchema.Null, underlyingSchema];
        return UnionSchema.Create(schemas, Options.LanguageFeatures.HasNullableReferenceTypes);
    }

    private AvroSchema? Union(IEnumerable<ITypeSyntax> items, string? containingNamespace, bool includeSystemError = false)
    {
        var schemas = UnionBranches(items, containingNamespace, includeSystemError);
        return schemas.IsDefault ? null : UnionSchema.Create(schemas, Options.LanguageFeatures.HasNullableReferenceTypes);
    }

    private ImmutableArray<AvroSchema> UnionBranches(IEnumerable<ITypeSyntax> items, string? containingNamespace, bool includeSystemError = false)
    {
        var schemas = ImmutableArray.CreateBuilder<AvroSchema>();
        var branches = new HashSet<AvroSchema>(UnionBranchComparer.Instance);
        if (includeSystemError)
        {
            branches.Add(AvroSchema.String);
            schemas.Add(AvroSchema.String);
        }
        var valid = true;
        foreach (var type in items.WithCancellation(cancellationToken))
        {
            var schema = Type(type, containingNamespace);
            switch (schema)
            {
                case null:
                    valid = false;
                    break;
                case UnionSchema:
                    Report(AvroDiagnostic.NestedUnion(type.GetSourceSpan()));
                    valid = false;
                    break;
                case not null when !branches.Add(schema):
                    Report(AvroDiagnostic.DuplicateUnionBranch(type.GetSourceSpan(), schema.SchemaName.FullName));
                    valid = false;
                    break;
                default:
                    schemas.Add(schema);
                    break;
            }
        }
        return valid ? schemas.DrainToImmutable() : default;
    }

    private AvroSchema? Logical(ILogicalTypeSyntax syntax)
    {
        if (syntax is DecimalLogicalTypeSyntax decimalSyntax)
        {
            var tracker = TrackDiagnostics();
            var precision = decimalSyntax.PrecisionLiteralToken.Value is int precisionValue
                ? precisionValue
                : Invalid(0, AvroDiagnostic.InvalidIdlDecimalPrecision(decimalSyntax.PrecisionLiteralToken.SourceSpan));
            var scale = decimalSyntax.ScaleLiteralToken.Value is int scaleValue
                ? scaleValue
                : Invalid(0, AvroDiagnostic.InvalidIdlDecimalScale(decimalSyntax.ScaleLiteralToken.SourceSpan));
            if (tracker.HasNewDiagnostics)
                return null;
            var properties = ImmutableSortedDictionary<string, JsonElement>.Empty
                .Add("precision", JsonSerializer.SerializeToElement(precision))
                .Add("scale", JsonSerializer.SerializeToElement(scale));
            var bytes = AvroSchema.Bytes with { Properties = properties };

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

    private AvroSchema? Named(NamedTypeSyntax syntax, string? containingNamespace)
    {
        syntax.Name.FullName.TrySplitQualifiedName(out var name, out var ns);
        if (string.IsNullOrWhiteSpace(name) || ns is "")
            return Invalid<AvroSchema?>(null, AvroDiagnostic.InvalidSchemaReference(syntax.Name.GetSourceSpan(), syntax.Name.FullName));
        return Reference(new SchemaName(name, ns), containingNamespace, syntax.Name.GetSourceSpan());
    }

    private NamedSchema? Schema(ISchemaDeclarationSyntax declaration, string? containingNamespace)
    {
        var schema = declaration switch
        {
            EnumDeclarationSyntax syntax => Enum(syntax, containingNamespace),
            FixedDeclarationSyntax syntax => Fixed(syntax, containingNamespace),
            RecordDeclarationSyntax syntax => Record(syntax, containingNamespace),
            ErrorDeclarationSyntax syntax => Error(syntax, containingNamespace),
            _ => throw new InvalidOperationException($"Unreachable: Unsupported Avro IDL schema declaration '{declaration.SyntaxKind}'.")
        };

        if (schema is not FixedSchema fixedSchema ||
            declaration.Annotations.OfType<LogicalTypeAnnotationSyntax>().LastOrDefault() is not { } annotation)
            return schema;

        var logicalType = GetString(annotation.JsonValue, AvroDiagnosticCode.InvalidIdlLogicalTypeAnnotation, required: true);
        if (logicalType is null) return null;
        if (LogicalSchema.Create(logicalType, fixedSchema, Options.GenerationTarget) is not LogicalSchema)
            return fixedSchema;

        var annotated = fixedSchema with { Properties = fixedSchema.Properties.SetItem(AvroJsonKeys.LogicalType, annotation.JsonValue.ToJsonElement()) };
        Replace(fixedSchema, annotated);
        return annotated;
    }

    private NamedSchema? Enum(EnumDeclarationSyntax syntax, string? containingNamespace)
    {
        var tracker = TrackDiagnostics();
        var schemaName = GetSchemaName(syntax, containingNamespace);
        if (tracker.HasNewDiagnostics) return null;
        if (IsInRecursionScope(schemaName))
            return Invalid<NamedSchema?>(null, AvroDiagnostic.RecursiveDefinition(syntax.Name.GetSourceSpan(), schemaName));

        using var scope = EnterRecursionScope(schemaName);
        var documentation = syntax.GetDocumentation();
        var aliases = GetAliases(syntax);
        var symbols = new HashSet<string>(StringComparer.Ordinal);
        foreach (var symbol in syntax.Symbols.WithCancellation(cancellationToken))
            if (!symbols.Add(symbol.FullName))
                Report(AvroDiagnostic.DuplicateSymbol(symbol.GetSourceSpan(), symbol.FullName, schemaName));
        var defaultValue = GetEnumDefault(syntax);
        var properties = syntax.GetSchemaProperties();
        if (aliases.IsDefault || tracker.HasNewDiagnostics)
            return null;

        var schema = new EnumSchema(schemaName, documentation, aliases, [.. syntax.Symbols.WithCancellation(cancellationToken).Select(static symbol => new MemberName(symbol.FullName))], defaultValue, properties);
        Declare(schema, syntax.GetSourceSpan());
        return schema;
    }

    private string? GetEnumDefault(EnumDeclarationSyntax syntax) =>
        syntax.DefaultValue is { } clause
            ? GetString(clause.JsonValue, AvroDiagnosticCode.InvalidIdlEnumDefault, required: false)
            : null;

    private NamedSchema? Fixed(FixedDeclarationSyntax syntax, string? containingNamespace)
    {
        var tracker = TrackDiagnostics();
        var schemaName = GetSchemaName(syntax, containingNamespace);
        if (tracker.HasNewDiagnostics) return null;
        if (IsInRecursionScope(schemaName))
            return Invalid<NamedSchema?>(null, AvroDiagnostic.RecursiveDefinition(syntax.Name.GetSourceSpan(), schemaName));

        using var scope = EnterRecursionScope(schemaName);
        var documentation = syntax.GetDocumentation();
        var aliases = GetAliases(syntax);
        var size = GetFixedSize(syntax);
        var properties = syntax.GetSchemaProperties();
        if (aliases.IsDefault || tracker.HasNewDiagnostics)
            return null;

        var schema = new FixedSchema(schemaName, documentation, aliases, size, properties)
        {
            // Only Apache.Avro needs a custom type for fixed, others use byte[].
            CSharpName = Options.GenerationTarget is GenerationTarget.Apache
                ? CSharpName.FromSchemaName(schemaName)
                : AvroSchema.Bytes.CSharpName
        };
        Declare(schema, syntax.GetSourceSpan());
        return schema;
    }

    private int GetFixedSize(FixedDeclarationSyntax syntax) =>
        syntax.SizeLiteralToken.Value is int value and > 0
            ? value
            : Invalid(0, AvroDiagnostic.InvalidIdlFixedSize(syntax.SizeLiteralToken.SourceSpan));

    private NamedSchema? Error(ErrorDeclarationSyntax syntax, string? containingNamespace)
    {
        var tracker = TrackDiagnostics();
        var schemaName = GetSchemaName(syntax, containingNamespace);
        if (tracker.HasNewDiagnostics) return null;
        if (IsInRecursionScope(schemaName))
            return Invalid<NamedSchema?>(null, AvroDiagnostic.RecursiveDefinition(syntax.Name.GetSourceSpan(), schemaName));

        using var scope = EnterRecursionScope(schemaName);
        var documentation = syntax.GetDocumentation();
        var aliases = GetAliases(syntax);
        var fields = Fields(syntax.Fields, schemaName);
        var properties = syntax.GetSchemaProperties();
        if (aliases.IsDefault || fields.IsDefault || tracker.HasNewDiagnostics)
            return null;

        var schema = new ErrorSchema(schemaName, documentation, aliases, fields, properties);
        Declare(schema, syntax.GetSourceSpan());
        return schema;
    }

    private NamedSchema? Record(RecordDeclarationSyntax syntax, string? containingNamespace)
    {
        var tracker = TrackDiagnostics();
        var schemaName = GetSchemaName(syntax, containingNamespace);
        if (tracker.HasNewDiagnostics) return null;
        if (IsInRecursionScope(schemaName))
            return Invalid<NamedSchema?>(null, AvroDiagnostic.RecursiveDefinition(syntax.Name.GetSourceSpan(), schemaName));

        using var scope = EnterRecursionScope(schemaName);
        var documentation = syntax.GetDocumentation();
        var aliases = GetAliases(syntax);
        var fields = Fields(syntax.Fields, schemaName);
        var properties = syntax.GetSchemaProperties();
        if (aliases.IsDefault || fields.IsDefault || tracker.HasNewDiagnostics)
            return null;

        var schema = new RecordSchema(schemaName, documentation, aliases, fields, properties);
        Declare(schema, syntax.GetSourceSpan());
        return schema;
    }

    private ImmutableArray<Field> Fields(SyntaxList<FieldDeclarationSyntax> syntaxList, SchemaName containingSchemaName)
    {
        var fields = ImmutableArray.CreateBuilder<Field>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        var valid = true;
        foreach (var syntax in syntaxList.WithCancellation(cancellationToken))
        {
            if (Field(syntax, containingSchemaName) is not { } field)
            {
                valid = false;
            }
            else if (!names.Add(field.Name.SchemaName))
            {
                Report(AvroDiagnostic.DuplicateField(syntax.Name.GetSourceSpan(), field.Name.SchemaName, containingSchemaName));
                valid = false;
            }
            else
            {
                fields.Add(field);
            }
        }
        return valid ? fields.DrainToImmutable() : default;
    }

    private Field? Field(FieldDeclarationSyntax syntax, SchemaName containingSchemaName)
    {
        var tracker = TrackDiagnostics();
        var name = new MemberName(syntax.Name.FullName);
        var defaultValue = syntax.DefaultValueClause?.JsonValue?.ToJsonElement() is JsonElement json ? new UnboundDefaultValue(json) : null;
        var hasNonNullDefault = defaultValue is { Json.ValueKind: not (JsonValueKind.Null or JsonValueKind.Undefined) };
        var type = Type(syntax.Type, containingSchemaName.Namespace, hasNonNullDefault: hasNonNullDefault);
        var documentation = syntax.GetDocumentation();
        var aliases = GetAliases(syntax);
        var order = syntax.Annotations.OfType<OrderAnnotationSyntax>().LastOrDefault() is { } annotation
            ? GetString(annotation.JsonValue, AvroDiagnosticCode.InvalidIdlOrder, required: true)
            : null;
        var properties = syntax.GetSchemaProperties();

        if (type is null || aliases.IsDefault || tracker.HasNewDiagnostics)
            return null;

        var fieldType = ResolveFieldType(type, name, containingSchemaName, out var underlyingType, out var remarks);
        cancellationToken.ThrowIfCancellationRequested();
        if (tracker.HasNewDiagnostics) return null;
        return new Field(name, fieldType, underlyingType, documentation, aliases, defaultValue, order, properties, remarks);
    }

    private ProtocolSchema? Protocol(ProtocolDeclarationSyntax syntax, string? containingNamespace)
    {
        var tracker = TrackDiagnostics();
        var schemaName = GetSchemaName(syntax, containingNamespace);
        if (tracker.HasNewDiagnostics) return null;
        if (IsInRecursionScope(schemaName))
            return Invalid<ProtocolSchema?>(null, AvroDiagnostic.RecursiveDefinition(syntax.Name.GetSourceSpan(), schemaName));

        using var scope = EnterRecursionScope(schemaName);
        var documentation = syntax.GetDocumentation();
        var types = ProtocolTypes(syntax.Types, schemaName.Namespace);
        var messages = ProtocolMessages(syntax.Messages, schemaName.Namespace);
        var properties = syntax.GetProtocolProperties();
        if (types.IsDefault || messages.IsDefault || tracker.HasNewDiagnostics)
            return null;
        var protocol = new ProtocolSchema(schemaName, documentation, types, messages, properties);
        Declare(protocol, syntax.GetSourceSpan());
        return protocol;
    }

    private ImmutableArray<NamedSchema> ProtocolTypes(SyntaxList<ISchemaDeclarationSyntax> syntaxList, string? containingNamespace)
    {
        var schemas = ImmutableArray.CreateBuilder<NamedSchema>();
        var valid = true;
        foreach (var declaration in syntaxList.WithCancellation(cancellationToken))
        {
            var schema = Schema(declaration, containingNamespace);
            if (schema is not null)
                schemas.Add(schema);
            else
                valid = false;
        }
        return valid ? schemas.DrainToImmutable() : default;
    }

    private ImmutableArray<ProtocolMessage> ProtocolMessages(SyntaxList<MessageDeclarationSyntax> syntaxList, string? containingNamespace)
    {
        var messages = ImmutableArray.CreateBuilder<ProtocolMessage>();
        var valid = true;
        foreach (var syntax in syntaxList.WithCancellation(cancellationToken))
        {
            if (Message(syntax, containingNamespace) is { } message)
                messages.Add(message);
            else
                valid = false;
        }
        return valid ? messages.DrainToImmutable() : default;
    }

    private ProtocolMessage? Message(MessageDeclarationSyntax syntax, string? containingNamespace)
    {
        var tracker = TrackDiagnostics();
        var methodName = syntax.Name.FullName.ToValidName();
        var documentation = syntax.GetDocumentation();
        var requestParameters = ProtocolRequestParameters(syntax.Parameters, containingNamespace);
        var response = ProtocolResponse(syntax.Type, containingNamespace);
        var errors = ProtocolErrors(syntax.ThrowsErrorClause, containingNamespace);
        var oneWay = syntax.OneWayClause is not null ? true : default(bool?);
        if (oneWay is true && response is not null && (response.Type.SchemaType is not SchemaType.Null || errors is not null))
            Report(AvroDiagnostic.InvalidIdlOneWayMessage(syntax.OneWayClause!.OneWayKeyword.SourceSpan, syntax.Name.FullName));
        if (requestParameters.IsDefault || response is null || tracker.HasNewDiagnostics)
            return null;
        return new ProtocolMessage(methodName, documentation, requestParameters, response, errors, oneWay);
    }

    private ImmutableArray<ProtocolRequestParameter> ProtocolRequestParameters(SeparatedSyntaxList<ParameterDeclarationSyntax> syntaxList, string? containingNamespace)
    {
        var parameters = ImmutableArray.CreateBuilder<ProtocolRequestParameter>();
        var valid = true;
        foreach (var syntax in syntaxList.WithCancellation(cancellationToken))
        {
            if (ProtocolRequestParameter(syntax, containingNamespace) is { } parameter)
                parameters.Add(parameter);
            else
                valid = false;
        }
        return valid ? parameters.DrainToImmutable() : default;
    }

    private ProtocolRequestParameter? ProtocolRequestParameter(ParameterDeclarationSyntax syntax, string? containingNamespace)
    {
        var tracker = TrackDiagnostics();
        var name = syntax.Name.FullName.ToValidName();
        var defaultValue = syntax.DefaultValueClause?.JsonValue?.ToJsonElement() is JsonElement json ? new UnboundDefaultValue(json) : null;
        var hasNonNullDefault = defaultValue is { Json.ValueKind: not (JsonValueKind.Null or JsonValueKind.Undefined) };
        var type = Type(syntax.Type, containingNamespace, hasNonNullDefault: hasNonNullDefault);
        var documentation = syntax.GetDocumentation();
        if (type is null) return null;
        var underlyingType = type is UnionSchema union ? union.UnderlyingSchema : type;
        cancellationToken.ThrowIfCancellationRequested();
        if (tracker.HasNewDiagnostics) return null;
        return new ProtocolRequestParameter(name, type, underlyingType, documentation, defaultValue);
    }

    private ProtocolResponse? ProtocolResponse(ITypeSyntax syntax, string? containingNamespace)
    {
        var type = Type(syntax, containingNamespace);
        if (type is null) return null;
        var underlyingType = type is UnionSchema union ? union.UnderlyingSchema : type;
        return new ProtocolResponse(type, underlyingType);
    }

    private AvroSchema? ProtocolErrors(ThrowsErrorClauseSyntax? syntax, string? containingNamespace) =>
        syntax is null ? null : Union(syntax.Errors, containingNamespace, includeSystemError: true);

    private ImmutableArray<string> GetAliases(IDeclarationSyntax syntax)
    {
        if (syntax.Annotations.OfType<AliasesAnnotationSyntax>().LastOrDefault() is not { } annotation)
            return [];
        if (annotation.JsonValue.JsonNode is not JsonArray array)
            return Invalid(default(ImmutableArray<string>), AvroDiagnostic.InvalidIdlProperty(AvroDiagnosticCode.InvalidIdlAliases, annotation.JsonValue.GetSourceSpan()));

        var builder = ImmutableArray.CreateBuilder<string>(array.Count);
        foreach (var node in array.WithCancellation(cancellationToken))
        {
            if (node is not JsonValue value || !value.TryGetValue<string>(out var result))
                return Invalid(default(ImmutableArray<string>), AvroDiagnostic.InvalidIdlProperty(AvroDiagnosticCode.InvalidIdlAliases, annotation.JsonValue.GetSourceSpan()));
            builder.Add(result);
        }
        return builder.MoveToImmutable();
    }

    private SchemaName GetSchemaName(IDeclarationSyntax syntax, string? containingNamespace)
    {
        var name = syntax.Name.FullName;
        if (name.TrySplitQualifiedName(out name, out var ns))
            return new SchemaName(name, ns);
        if (syntax.Annotations.OfType<NamespaceAnnotationSyntax>().LastOrDefault() is { } annotation)
        {
            var tracker = TrackDiagnostics();
            containingNamespace = GetString(annotation.JsonValue, AvroDiagnosticCode.InvalidIdlNamespace, required: true);
            if (tracker.HasNewDiagnostics) return default;
        }
        return new SchemaName(name, containingNamespace);
    }

    private string? GetString(JsonValueSyntax syntax, AvroDiagnosticCode code, bool required)
    {
        if (syntax.JsonNode is null && !required)
            return null;
        if (syntax.JsonNode is JsonValue value && value.TryGetValue<string>(out var result))
            return result;
        return Invalid<string?>(null, AvroDiagnostic.InvalidIdlProperty(code, syntax.GetSourceSpan()));
    }

    private T Invalid<T>(T @default, AvroDiagnostic diagnostic)
    {
        Report(diagnostic);
        return @default;
    }
}
