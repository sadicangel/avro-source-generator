using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Avdl.Syntax.Annotations;
using AvroSourceGenerator.Avdl.Syntax.Declarations;
using AvroSourceGenerator.Avdl.Syntax.Types;
using AvroSourceGenerator.Avsc;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Avdl.Syntax;

public sealed partial class AvdlParser
{
    public static AvroFile ParseFile(SourceText source, AvroParseOptions options, CancellationToken cancellationToken) =>
        new AvdlParser(source, options, cancellationToken).ParseFile();

    public AvroFile ParseFile()
    {
        var source = sourceText;
        var syntaxTree = Parse();
        if (!syntaxTree.Diagnostics.IsEmpty)
            return AvroFile.Invalid(source, syntaxTree.Diagnostics, Options);

        try
        {
            return ParseCore(syntaxTree);
        }
        catch (InvalidSourceException ex)
        {
            return AvroFile.Invalid(source, ex.Diagnostics, Options);
        }
        catch (InvalidSchemaException ex)
        {
            return AvroFile.Invalid(source, AvroDiagnostic.InvalidSchemaValue(SourceSpan.FromSourceText(source), ex.Message), Options);
        }
    }

    private AvroFile ParseCore(SyntaxTree syntaxTree)
    {
        CancellationToken.ThrowIfCancellationRequested();
        var source = syntaxTree.SourceText;

        var imports = syntaxTree.Document.ImportDirectives
            .Concat(
                syntaxTree.Document.Declarations
                    .OfType<ProtocolDeclarationSyntax>()
                    .SelectMany(static protocol => protocol.Imports))
            .WithCancellation(CancellationToken)
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
        var root = Document(syntaxTree);
        if (imports.IsEmpty && !root.ContainsTopLevelSchema())
        {
            var sourceSpan = syntaxTree.Document.SchemaDirective?.MainSchemaType is { } mainSchemaType
                ? mainSchemaType.GetSourceSpan()
                : syntaxTree.Document.GetSourceSpan();
            throw new InvalidSourceException([AvroDiagnostic.MissingIdlRoot(sourceSpan)]);
        }

        return new AvroFile(
            source,
            root,
            [.. Declarations],
            [.. DeclarationSpans],
            GetReferences(),
            GetReferenceSpans(),
            GetDependencies(),
            imports,
            [.. Diagnostics],
            Options);
    }

    private AvroSchema Document(SyntaxTree syntaxTree)
    {
        ThrowIfCancellationRequested();
        var document = syntaxTree.Document;
        var containingNamespace = document.NamespaceDirective?.NamespaceName.FullName;
        var mainSchema = document.SchemaDirective?.MainSchemaType;

        if (mainSchema is null)
        {
            if (document.Declarations is not [ProtocolDeclarationSyntax protocol])
            {
                throw new InvalidSourceException([AvroDiagnostic.InvalidIdlDocument(syntaxTree.Document.GetSourceSpan())]);
            }

            return Protocol(protocol, containingNamespace);
        }

        foreach (var declaration in document.Declarations.WithCancellation(CancellationToken))
        {
            if (declaration is not ISchemaDeclarationSyntax schemaDeclaration)
            {
                throw new InvalidSourceException([AvroDiagnostic.InvalidIdlDeclaration(declaration.GetSourceSpan(), declaration.SyntaxKind)]);
            }

            Schema(schemaDeclaration, containingNamespace);
        }

        return Type(mainSchema, containingNamespace);
    }

    private AvroSchema Type(
        ITypeSyntax syntax,
        string? containingNamespace,
        ImmutableSortedDictionary<string, JsonElement>? properties = null,
        JsonElement? defaultJson = null)
    {
        ThrowIfCancellationRequested();
        properties ??= ImmutableSortedDictionary<string, JsonElement>.Empty;

        return syntax switch
        {
            AnnotatedTypeSyntax type => Annotated(type, containingNamespace, defaultJson),
            ArrayTypeSyntax type => Array(type, containingNamespace, properties),
            ILogicalTypeSyntax type => Logical(type, containingNamespace),
            MapTypeSyntax type => Map(type, containingNamespace, properties),
            NamedTypeSyntax type => Reference(type.Name.FullName.ToSchemaName(), containingNamespace, type.Name.GetSourceSpan()),
            OptionalTypeSyntax type => Optional(type, containingNamespace, defaultJson),
            PrimitiveTypeSyntax type => Primitive(type, containingNamespace, properties),
            UnionTypeSyntax type => Union(type, containingNamespace),
            _ => throw new InvalidSourceException([AvroDiagnostic.InvalidIdlType(syntax.GetSourceSpan(), syntax.SyntaxKind)]),
        };
    }

    private NamedSchema Schema(ISchemaDeclarationSyntax declaration, string? containingNamespace)
    {
        return declaration switch
        {
            EnumDeclarationSyntax syntax => Enum(syntax, containingNamespace),
            ErrorDeclarationSyntax syntax => Error(syntax, containingNamespace),
            FixedDeclarationSyntax syntax => Fixed(syntax, containingNamespace),
            RecordDeclarationSyntax syntax => Record(syntax, containingNamespace),
            _ => throw new InvalidSourceException([AvroDiagnostic.InvalidIdlSchemaDeclaration(declaration.GetSourceSpan(), declaration.SyntaxKind)])
        };
    }

    private AvroSchema Annotated(AnnotatedTypeSyntax syntax, string? containingNamespace, JsonElement? defaultJson)
    {
        var logicalTypeName = syntax.Annotations.OfType<LogicalTypeAnnotationSyntax>().LastOrDefault() is { } annotation
            ? annotation.JsonValue.GetRequiredString("Logical type annotation value")
            : null;
        var properties = syntax.Annotations.GetProperties(ReservedSchemaProperties.IsReserved);
        var underlyingSchema = Type(syntax.Type, containingNamespace, properties, defaultJson);
        return logicalTypeName is not null
            ? LogicalSchema.Create(logicalTypeName, underlyingSchema, Options.GenerationTarget)
            : underlyingSchema;
    }

    private PrimitiveSchema Primitive(PrimitiveTypeSyntax syntax, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement> properties)
    {
        return syntax.SyntaxKind switch
        {
            SyntaxKind.VoidType => AvroSchema.Null,
            SyntaxKind.NullType => AvroSchema.Null,
            SyntaxKind.IntType => AvroSchema.Int,
            SyntaxKind.LongType => AvroSchema.Long,
            SyntaxKind.StringType => AvroSchema.String,
            SyntaxKind.BooleanType => AvroSchema.Boolean,
            SyntaxKind.FloatType => AvroSchema.Float,
            SyntaxKind.DoubleType => AvroSchema.Double,
            SyntaxKind.BytesType => AvroSchema.Bytes,

            _ => throw new InvalidSourceException([AvroDiagnostic.InvalidIdlPrimitive(syntax.TypeKeyword.SourceSpan, syntax.SyntaxKind)])
        };
    }

    private ArraySchema Array(ArrayTypeSyntax syntax, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement> properties)
    {
        var items = Type(syntax.ItemType, containingNamespace);
        return new ArraySchema(items, Documentation: null, properties);
    }

    private MapSchema Map(MapTypeSyntax syntax, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement> properties)
    {
        var values = Type(syntax.ValueType, containingNamespace);
        return new MapSchema(values, Documentation: null, properties);
    }

    private EnumSchema Enum(EnumDeclarationSyntax syntax, string? containingNamespace)
    {
        var schemaName = syntax.GetRequiredSchemaName(containingNamespace);
        using (EnterRecursionScope(schemaName))
        {
            var documentation = syntax.GetDocumentation();
            var aliases = syntax.GetAliases();
            var symbols = syntax.Symbols.WithCancellation(CancellationToken).Select(static symbol => symbol.FullName).ToImmutableArray();
            var @default = syntax.DefaultValue?.JsonValue.ToOptionalString("Enum default value");
            var properties = syntax.GetSchemaProperties();

            var enumSchema = new EnumSchema(schemaName, documentation, aliases, symbols, @default, properties);
            Declare(enumSchema, syntax.GetSourceSpan());
            return enumSchema;
        }
    }

    private FixedSchema Fixed(FixedDeclarationSyntax syntax, string? containingNamespace)
    {
        var schemaName = syntax.GetRequiredSchemaName(containingNamespace);
        using (EnterRecursionScope(schemaName))
        {
            var documentation = syntax.GetDocumentation();
            var aliases = syntax.GetAliases();
            var size = syntax.SizeLiteralToken.Value is int value and > 0
                ? value
                : throw new InvalidSourceException([AvroDiagnostic.InvalidIdlFixedSize(syntax.SizeLiteralToken.SourceSpan)]);
            var properties = syntax.GetSchemaProperties();

            var fixedSchema = new FixedSchema(schemaName, documentation, aliases, size, properties)
            {
                // Only Apache.Avro needs a custom type for fixed, others use byte[].
                CSharpName = Options.GenerationTarget is GenerationTarget.Apache
                    ? CSharpName.FromSchemaName(schemaName)
                    : AvroSchema.Bytes.CSharpName
            };
            Declare(fixedSchema, syntax.GetSourceSpan());
            return fixedSchema;
        }
    }

    private ErrorSchema Error(ErrorDeclarationSyntax syntax, string? containingNamespace)
    {
        var schemaName = syntax.GetRequiredSchemaName(containingNamespace);
        using (EnterRecursionScope(schemaName))
        {
            var documentation = syntax.GetDocumentation();
            var aliases = syntax.GetAliases();
            var fields = Fields(syntax.Fields, schemaName);
            var properties = syntax.GetSchemaProperties();

            var errorSchema = new ErrorSchema(schemaName, documentation, aliases, fields, properties);
            Declare(errorSchema, syntax.GetSourceSpan());
            return errorSchema;
        }
    }

    private RecordSchema Record(RecordDeclarationSyntax syntax, string? containingNamespace)
    {
        var schemaName = syntax.GetRequiredSchemaName(containingNamespace);
        using (EnterRecursionScope(schemaName))
        {
            var documentation = syntax.GetDocumentation();
            var aliases = syntax.GetAliases();
            var fields = Fields(syntax.Fields, schemaName);
            var properties = syntax.GetSchemaProperties();

            var recordSchema = new RecordSchema(schemaName, documentation, aliases, fields, properties);
            Declare(recordSchema, syntax.GetSourceSpan());
            return recordSchema;
        }
    }

    private ImmutableArray<Field> Fields(SyntaxList<FieldDeclarationSyntax> syntaxList, SchemaName containingSchemaName)
    {
        var fields = ImmutableArray.CreateBuilder<Field>(syntaxList.Count);
        foreach (var syntax in syntaxList.WithCancellation(CancellationToken))
            fields.Add(Field(syntax, containingSchemaName));
        return fields.MoveToImmutable();
    }

    private Field Field(FieldDeclarationSyntax syntax, SchemaName containingSchemaName)
    {
        var name = new FieldName(syntax.Name.FullName);
        var defaultJson = syntax.DefaultValueClause?.JsonValue.ToOptionalJsonElement();
        var type = Type(syntax.Type, containingSchemaName.Namespace, defaultJson: defaultJson);
        type = ResolveFieldType(type, name, containingSchemaName, out var underlyingType, out var remarks);

        var documentation = syntax.GetDocumentation();
        var aliases = syntax.GetAliases();
        var @default = type.GetValue(defaultJson);
        var order = syntax.Annotations.OfType<OrderAnnotationSyntax>().LastOrDefault() is { } orderAnnotation
            ? orderAnnotation.JsonValue.GetRequiredString("Order annotation value")
            : null;
        var properties = syntax.GetSchemaProperties();

        return new Field(name, type, underlyingType, documentation, aliases, defaultJson, @default, order, properties, remarks);
    }

    private UnionSchema Optional(OptionalTypeSyntax syntax, string? containingNamespace, JsonElement? defaultJson)
    {
        var underlyingSchema = Type(syntax.Type, containingNamespace);
        var schemas = defaultJson is null or { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined }
            ? ImmutableArray.Create(AvroSchema.Null, underlyingSchema)
            : ImmutableArray.Create(underlyingSchema, AvroSchema.Null);
        return UnionSchema.Create(schemas, Options.UseNullableReferenceTypes);
    }

    private UnionSchema Union(UnionTypeSyntax syntax, string? containingNamespace)
    {
        var builder = ImmutableArray.CreateBuilder<AvroSchema>(syntax.Types.Count);
        foreach (var typeSyntax in syntax.Types.WithCancellation(CancellationToken))
            builder.Add(Type(typeSyntax, containingNamespace));
        var schemas = builder.MoveToImmutable();

        return UnionSchema.Create(schemas, Options.UseNullableReferenceTypes);
    }

    private AvroSchema Logical(ILogicalTypeSyntax syntax, string? containingNamespace)
    {
        if (syntax is DecimalLogicalTypeSyntax decimalSyntax)
        {
            var precision = decimalSyntax.PrecisionLiteralToken.Value as int?
                ?? throw new InvalidSourceException([AvroDiagnostic.InvalidIdlDecimalPrecision(decimalSyntax.PrecisionLiteralToken.SourceSpan)]);
            var scale = decimalSyntax.ScaleLiteralToken.Value as int?
                ?? throw new InvalidSourceException([AvroDiagnostic.InvalidIdlDecimalScale(decimalSyntax.ScaleLiteralToken.SourceSpan)]);
            var properties = ImmutableSortedDictionary<string, JsonElement>.Empty
                .Add("precision", JsonSerializer.SerializeToElement(precision))
                .Add("scale", JsonSerializer.SerializeToElement(scale));
            var bytes = AvroSchema.Bytes with { Properties = properties };
            return LogicalSchema.Create(LogicalTypeNames.Decimal, bytes, Options.GenerationTarget);
        }

        if (syntax is not LogicalTypeSyntax logical)
        {
            throw new InvalidSourceException([AvroDiagnostic.InvalidIdlLogicalType(syntax.GetSourceSpan(), syntax.SyntaxKind)]);
        }

        return logical.LogicalTypeNameKeyword.SyntaxKind switch
        {
            SyntaxKind.DateKeyword => LogicalSchema.Create(LogicalTypeNames.Date, AvroSchema.Int, Options.GenerationTarget),
            SyntaxKind.TimeMsKeyword => LogicalSchema.Create(LogicalTypeNames.TimeMillis, AvroSchema.Int, Options.GenerationTarget),
            SyntaxKind.TimestampMsKeyword => LogicalSchema.Create(LogicalTypeNames.TimestampMillis, AvroSchema.Long, Options.GenerationTarget),
            SyntaxKind.LocalTimestampMsKeyword => LogicalSchema.Create(LogicalTypeNames.LocalTimestampMillis, AvroSchema.Long, Options.GenerationTarget),
            SyntaxKind.UuidKeyword => LogicalSchema.Create(LogicalTypeNames.Uuid, AvroSchema.String, Options.GenerationTarget),
            _ => throw new InvalidSourceException([AvroDiagnostic.InvalidIdlLogicalType(logical.LogicalTypeNameKeyword.SourceSpan, syntax.SyntaxKind)])
        };
    }

    private ProtocolSchema Protocol(ProtocolDeclarationSyntax syntax, string? containingNamespace)
    {
        var schemaName = syntax.GetRequiredSchemaName(containingNamespace);
        using (EnterRecursionScope(schemaName))
        {
            var documentation = syntax.GetDocumentation();
            var types = ProtocolTypes(syntax.Types, schemaName.Namespace);
            var messages = ProtocolMessages(syntax.Messages, schemaName.Namespace);
            var properties = syntax.GetProtocolProperties();

            var protocolSchema = new ProtocolSchema(schemaName, documentation, types, messages, properties);

            Declare(protocolSchema, syntax.GetSourceSpan());

            return protocolSchema;
        }
    }

    private ImmutableArray<NamedSchema> ProtocolTypes(SyntaxList<ISchemaDeclarationSyntax> syntaxList, string? containingNamespace)
    {
        var types = ImmutableArray.CreateBuilder<NamedSchema>(syntaxList.Count);
        foreach (var type in syntaxList.WithCancellation(CancellationToken))
            types.Add(Schema(type, containingNamespace));

        return types.MoveToImmutable();
    }

    private ImmutableArray<ProtocolMessage> ProtocolMessages(SyntaxList<MessageDeclarationSyntax> syntaxList, string? containingNamespace)
    {
        var protocolMessages = ImmutableArray.CreateBuilder<ProtocolMessage>(syntaxList.Count);
        foreach (var syntax in syntaxList.WithCancellation(CancellationToken))
            protocolMessages.Add(Message(syntax, containingNamespace));
        return protocolMessages.MoveToImmutable();
    }

    private ProtocolMessage Message(MessageDeclarationSyntax syntax, string? containingNamespace)
    {
        var methodName = syntax.Name.FullName.ToValidName();
        var documentation = syntax.GetDocumentation();
        var requestParameters = ProtocolRequestParameters(syntax.Parameters, containingNamespace);
        var response = ProtocolResponse(syntax.Type, containingNamespace);
        var errors = ProtocolErrors(syntax.ThrowsErrorClause, containingNamespace);
        var oneWay = syntax.OneWayClause is not null ? true : default(bool?);
        if (oneWay is true && (response.Type.Type is not SchemaType.Null || errors.Length > 0))
        {
            throw new InvalidSourceException([AvroDiagnostic.InvalidIdlOneWayMessage(syntax.OneWayClause!.OneWayKeyword.SourceSpan, syntax.Name.FullName)]);
        }

        return new ProtocolMessage(methodName, documentation, requestParameters, response, errors, oneWay);
    }

    private ImmutableArray<ProtocolRequestParameter> ProtocolRequestParameters(SeparatedSyntaxList<ParameterDeclarationSyntax> syntaxList, string? containingNamespace)
    {
        var fields = ImmutableArray.CreateBuilder<ProtocolRequestParameter>(syntaxList.Count);
        foreach (var syntax in syntaxList.WithCancellation(CancellationToken))
            fields.Add(ProtocolRequestParameter(syntax, containingNamespace));

        return fields.MoveToImmutable();
    }

    private ProtocolRequestParameter ProtocolRequestParameter(ParameterDeclarationSyntax syntax, string? containingNamespace)
    {
        var name = syntax.Name.FullName.ToValidName();
        var defaultJson = syntax.DefaultValueClause?.JsonValue.ToOptionalJsonElement();
        var type = Type(syntax.Type, containingNamespace, defaultJson: defaultJson);
        var underlyingType = type is UnionSchema union ? union.UnderlyingSchema : type;

        var documentation = syntax.GetDocumentation();
        var @default = type.GetValue(defaultJson);
        return new ProtocolRequestParameter(name, type, underlyingType, documentation, defaultJson, @default);
    }

    private ProtocolResponse ProtocolResponse(ITypeSyntax syntax, string? containingNamespace)
    {
        var type = Type(syntax, containingNamespace);
        var underlyingType = type is UnionSchema union ? union.UnderlyingSchema : type;

        return new ProtocolResponse(type, underlyingType);
    }

    private ImmutableArray<AvroSchema> ProtocolErrors(ThrowsErrorClauseSyntax? syntax, string? containingNamespace)
    {
        if (syntax is null || syntax.Errors is [])
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<AvroSchema>(syntax.Errors.Count);
        foreach (var errorSyntax in syntax.Errors.WithCancellation(CancellationToken))
        {
            // TODO: Do we need to validate that this is an error schema?
            builder.Add(Type(errorSyntax, containingNamespace));
        }

        return builder.MoveToImmutable();
    }
}
