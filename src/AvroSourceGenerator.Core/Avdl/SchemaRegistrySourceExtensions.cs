using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Avdl.Syntax;
using AvroSourceGenerator.Avdl.Syntax.Annotations;
using AvroSourceGenerator.Avdl.Syntax.Declarations;
using AvroSourceGenerator.Avdl.Syntax.Directives;
using AvroSourceGenerator.Avdl.Syntax.Types;
using AvroSourceGenerator.Avdl.Text;
using AvroSourceGenerator.Avsc;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Registry;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Avdl;

public static class SchemaRegistrySourceExtensions
{
    extension(in SchemaRegistry schemaRegistry)
    {
        public void RegisterSource(SyntaxTree syntaxTree)
        {
            using (schemaRegistry.EnterRegisterScope())
            {
                var registeredSchema = schemaRegistry.Document(syntaxTree);
                if (registeredSchema is AvroSchemaReference reference)
                {
                    registeredSchema = schemaRegistry.Resolve(reference.SchemaName) ?? registeredSchema;
                }
                if (!registeredSchema.ContainsTopLevelSchema())
                {
                    var sourceSpan = syntaxTree.Document.SchemaDirective?.MainSchemaType is { } mainSchemaType
                        ? GetSourceSpan(mainSchemaType)
                        : GetSourceSpan(syntaxTree);
                    throw new InvalidSourceException("At least a named schema must be present in source.", sourceSpan);
                }
            }
        }

        private AvroSchema Document(SyntaxTree syntaxTree)
        {
            var document = syntaxTree.Document;
            var containingNamespace = document.NamespaceDirective?.NamespaceName.FullName;
            var mainSchema = document.SchemaDirective?.MainSchemaType;
            schemaRegistry.ValidateImports(document.ImportDirectives);

            if (mainSchema is null)
            {
                if (document.Declarations is not [ProtocolDeclarationSyntax protocol])
                {
                    throw new InvalidSourceException("Avro IDL files must contain a main schema directive or a single protocol declaration.", GetSourceSpan(syntaxTree));
                }

                return schemaRegistry.Protocol(protocol, containingNamespace);
            }

            foreach (var declaration in document.Declarations)
            {
                if (declaration is not ISchemaDeclarationSyntax schemaDeclaration)
                {
                    throw new InvalidSourceException($"Invalid declaration in Avro IDL file: {declaration.SyntaxKind}", GetSourceSpan(declaration));
                }

                schemaRegistry.Schema(schemaDeclaration, containingNamespace);
            }

            return schemaRegistry.Type(mainSchema, containingNamespace);
        }

        private void ValidateImports(IEnumerable<ImportDirectiveSyntax> imports)
        {
            if (schemaRegistry.Options.ReferenceResolution is ReferenceResolution.Strict
                && imports.FirstOrDefault() is { } import)
            {
                throw new InvalidSourceException(
                    "Imports are not yet supported in Avro IDL files. To work around this limitation, set AvroSourceGeneratorReferenceResolution to Deferred and include imported files as AdditionalFiles.",
                    import.ImportKeyword.SourceSpan);
            }
        }

        private AvroSchema Type(
            ITypeSyntax syntax,
            string? containingNamespace,
            ImmutableSortedDictionary<string, JsonElement>? properties = null,
            JsonElement? defaultJson = null)
        {
            properties ??= ImmutableSortedDictionary<string, JsonElement>.Empty;

            return syntax switch
            {
                AnnotatedTypeSyntax type => schemaRegistry.Annotated(type, containingNamespace, defaultJson),
                ArrayTypeSyntax type => schemaRegistry.Array(type, containingNamespace, properties),
                ILogicalTypeSyntax type => schemaRegistry.Logical(type, containingNamespace),
                MapTypeSyntax type => schemaRegistry.Map(type, containingNamespace, properties),
                NamedTypeSyntax type => schemaRegistry.Named(type.Name.FullName.ToSchemaName(), containingNamespace),
                OptionalTypeSyntax type => schemaRegistry.Optional(type, containingNamespace, defaultJson),
                PrimitiveTypeSyntax type => schemaRegistry.Primitive(type, containingNamespace, properties),
                UnionTypeSyntax type => schemaRegistry.Union(type, containingNamespace),
                // TODO: Fix this schema name. It's not a schema name, it's a type syntax.
                _ => throw new MissingReferenceException(new SchemaName(syntax.SyntaxKind.ToString(), containingNamespace)),
            };
        }

        private AvroSchema Named(SchemaName schemaName, string? containingNamespace)
        {
            if (schemaRegistry.Find(schemaName, containingNamespace) is { } named)
            {
                return named;
            }

            schemaName = schemaName.ResolveIn(containingNamespace);
            if (schemaRegistry.Options.ReferenceResolution is ReferenceResolution.Strict)
            {
                throw new MissingReferenceException(schemaName);
            }

            schemaRegistry.AddReference(schemaName);

            return schemaRegistry.Find(schemaName, containingNamespace: null)
                ?? throw new InvalidOperationException("Unreachable code: reference should have been added in the registry.");
        }

        private NamedSchema Schema(ISchemaDeclarationSyntax declaration, string? containingNamespace)
        {
            return declaration switch
            {
                EnumDeclarationSyntax syntax => schemaRegistry.Enum(syntax, containingNamespace),
                ErrorDeclarationSyntax syntax => schemaRegistry.Error(syntax, containingNamespace),
                FixedDeclarationSyntax syntax => schemaRegistry.Fixed(syntax, containingNamespace),
                RecordDeclarationSyntax syntax => schemaRegistry.Record(syntax, containingNamespace),
                _ => throw new InvalidSourceException($"Invalid declaration: {declaration.SyntaxKind}", GetSourceSpan(declaration))
            };
        }

        private AvroSchema Annotated(AnnotatedTypeSyntax syntax, string? containingNamespace, JsonElement? defaultJson)
        {
            var logicalTypeName = syntax.Annotations.OfType<LogicalTypeAnnotationSyntax>().LastOrDefault()?.LogicalTypeName;
            var properties = syntax.Annotations.OfType<CustomAnnotationSyntax>()
                .Where(a => !ReservedSchemaProperties.IsReserved(a.AnnotationName.FullName))
                .ToImmutableSortedDictionary(a => a.AnnotationName.FullName, a => a.JsonValue.ToJsonElement());
            var underlyingSchema = schemaRegistry.Type(syntax.Type, containingNamespace, properties, defaultJson);
            return logicalTypeName is not null
                ? LogicalSchema.Create(logicalTypeName, underlyingSchema, schemaRegistry.Options.TargetProfile)
                : underlyingSchema;
        }

        private PrimitiveSchema Primitive(PrimitiveTypeSyntax syntax, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement> properties)
        {
            return syntax.SyntaxKind switch
            {
                SyntaxKind.VoidType => AvroSchema.Object,
                SyntaxKind.NullType => AvroSchema.Object,
                SyntaxKind.IntType => AvroSchema.Int,
                SyntaxKind.LongType => AvroSchema.Long,
                SyntaxKind.StringType => AvroSchema.String,
                SyntaxKind.BooleanType => AvroSchema.Boolean,
                SyntaxKind.FloatType => AvroSchema.Float,
                SyntaxKind.DoubleType => AvroSchema.Double,
                SyntaxKind.BytesType => AvroSchema.Bytes,

                _ => throw new InvalidSourceException($"Invalid primitive type: {syntax.SyntaxKind}", syntax.TypeKeyword.SourceSpan)
            };
        }

        private ArraySchema Array(ArrayTypeSyntax syntax, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement> properties)
        {
            var items = schemaRegistry.Type(syntax.ItemType, containingNamespace);
            return new ArraySchema(items, Documentation: null, properties);
        }

        private MapSchema Map(MapTypeSyntax syntax, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement> properties)
        {
            var values = schemaRegistry.Type(syntax.ValueType, containingNamespace);
            return new MapSchema(values, Documentation: null, properties);
        }

        private EnumSchema Enum(EnumDeclarationSyntax syntax, string? containingNamespace)
        {
            var schemaName = syntax.GetRequiredSchemaName(containingNamespace);
            using (schemaRegistry.EnterRecursionScope(schemaName))
            {
                var documentation = syntax.GetDocumentation();
                var aliases = syntax.GetAliases();
                var symbols = syntax.Symbols.Select(s => s.FullName).ToImmutableArray();
                var @default = syntax.DefaultValue?.JsonValue.ToOptionalString();
                var properties = syntax.GetSchemaProperties();

                var enumSchema = new EnumSchema(schemaName, documentation, aliases, symbols, @default, properties);
                schemaRegistry.Register(enumSchema);
                return enumSchema;
            }
        }

        private FixedSchema Fixed(FixedDeclarationSyntax syntax, string? containingNamespace)
        {
            var schemaName = syntax.GetRequiredSchemaName(containingNamespace);
            using (schemaRegistry.EnterRecursionScope(schemaName))
            {
                var documentation = syntax.GetDocumentation();
                var aliases = syntax.GetAliases();
                var size = syntax.SizeLiteralToken.Value is int value and > 0
                    ? value
                    : throw new InvalidSourceException("Fixed size must be a positive integer.", syntax.SizeLiteralToken.SourceSpan);
                var properties = syntax.GetSchemaProperties();

                var fixedSchema = schemaRegistry.Options.TargetProfile switch
                {
                    // Only Apache.Avro needs a custom type for fixed, others use byte[].
                    TargetProfile.Apache => new FixedSchema(schemaName, documentation, aliases, size, properties),
                    _ => FixedSchema.CreateAsByteArray(schemaName, documentation, aliases, size, properties),
                };
                schemaRegistry.Register(fixedSchema);
                return fixedSchema;
            }
        }

        private ErrorSchema Error(ErrorDeclarationSyntax syntax, string? containingNamespace)
        {
            var schemaName = syntax.GetRequiredSchemaName(containingNamespace);
            using (schemaRegistry.EnterRecursionScope(schemaName))
            {
                var documentation = syntax.GetDocumentation();
                var aliases = syntax.GetAliases();
                var fields = schemaRegistry.Fields(syntax.Fields, schemaName);
                var properties = syntax.GetSchemaProperties();

                var errorSchema = new ErrorSchema(schemaName, documentation, aliases, fields, properties);
                schemaRegistry.Register(errorSchema);
                return errorSchema;
            }
        }

        private RecordSchema Record(RecordDeclarationSyntax syntax, string? containingNamespace)
        {
            var schemaName = syntax.GetRequiredSchemaName(containingNamespace);
            using (schemaRegistry.EnterRecursionScope(schemaName))
            {
                var documentation = syntax.GetDocumentation();
                var aliases = syntax.GetAliases();
                var fields = schemaRegistry.Fields(syntax.Fields, schemaName);
                var properties = syntax.GetSchemaProperties();

                var recordSchema = new RecordSchema(schemaName, documentation, aliases, fields, properties);
                schemaRegistry.Register(recordSchema);
                return recordSchema;
            }
        }

        private ImmutableArray<Field> Fields(SyntaxList<FieldDeclarationSyntax> syntaxList, SchemaName containingSchemaName)
        {
            var fields = ImmutableArray.CreateBuilder<Field>(syntaxList.Count);
            foreach (var syntax in syntaxList)
                fields.Add(schemaRegistry.Field(syntax, containingSchemaName));
            return fields.MoveToImmutable();
        }

        private Field Field(FieldDeclarationSyntax syntax, SchemaName containingSchemaName)
        {
            var name = syntax.Name.FullName.ToValidName();
            var defaultJson = syntax.DefaultValueClause?.JsonValue.ToOptionalJsonElement();
            var type = schemaRegistry.Type(syntax.Type, containingSchemaName.Namespace, defaultJson: defaultJson);
            type = schemaRegistry.ResolveFieldType(type, name, containingSchemaName, out var underlyingType, out var remarks);

            var documentation = syntax.GetDocumentation();
            var aliases = syntax.GetAliases();
            var @default = type.GetValue(defaultJson);
            var order = syntax.Annotations.OfType<OrderAnnotationSyntax>().LastOrDefault()?.Order;
            var properties = syntax.GetSchemaProperties();

            return new Field(name, type, underlyingType, documentation, aliases, defaultJson, @default, order, properties, remarks);
        }

        private UnionSchema Optional(OptionalTypeSyntax syntax, string? containingNamespace, JsonElement? defaultJson)
        {
            var underlyingSchema = schemaRegistry.Type(syntax.Type, containingNamespace);
            var schemas = defaultJson is null or { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined }
                ? ImmutableArray.Create(AvroSchema.Object, underlyingSchema)
                : ImmutableArray.Create(underlyingSchema, AvroSchema.Object);
            return UnionSchema.Create(schemas, schemaRegistry.Options.UseNullableReferenceTypes);
        }

        private UnionSchema Union(UnionTypeSyntax syntax, string? containingNamespace)
        {
            var builder = ImmutableArray.CreateBuilder<AvroSchema>(syntax.Types.Count);
            foreach (var typeSyntax in syntax.Types)
                builder.Add(schemaRegistry.Type(typeSyntax, containingNamespace));
            var schemas = builder.MoveToImmutable();

            return UnionSchema.Create(schemas, schemaRegistry.Options.UseNullableReferenceTypes);
        }

        private AvroSchema Logical(ILogicalTypeSyntax syntax, string? containingNamespace)
        {
            if (syntax is DecimalLogicalTypeSyntax decimalSyntax)
            {
                var precision = decimalSyntax.PrecisionLiteralToken.Value as int?
                    ?? throw new InvalidSourceException("Decimal precision must be an integer.", decimalSyntax.PrecisionLiteralToken.SourceSpan);
                var scale = decimalSyntax.ScaleLiteralToken.Value as int?
                    ?? throw new InvalidSourceException("Decimal scale must be an integer.", decimalSyntax.ScaleLiteralToken.SourceSpan);
                var properties = ImmutableSortedDictionary<string, JsonElement>.Empty
                    .Add("precision", JsonSerializer.SerializeToElement(precision))
                    .Add("scale", JsonSerializer.SerializeToElement(scale));
                var bytes = AvroSchema.Bytes with { Properties = properties };
                return LogicalSchema.Create(LogicalTypeNames.Decimal, bytes, schemaRegistry.Options.TargetProfile);
            }

            if (syntax is not LogicalTypeSyntax logical)
            {
                throw new InvalidSourceException($"Invalid logical type syntax: {syntax.SyntaxKind}", GetSourceSpan(syntax));
            }

            return logical.LogicalTypeNameKeyword.SyntaxKind switch
            {
                SyntaxKind.DateKeyword => LogicalSchema.Create(LogicalTypeNames.Date, AvroSchema.Int, schemaRegistry.Options.TargetProfile),
                SyntaxKind.TimeMsKeyword => LogicalSchema.Create(LogicalTypeNames.TimeMillis, AvroSchema.Int, schemaRegistry.Options.TargetProfile),
                SyntaxKind.TimestampMsKeyword => LogicalSchema.Create(LogicalTypeNames.TimestampMillis, AvroSchema.Long, schemaRegistry.Options.TargetProfile),
                SyntaxKind.LocalTimestampMsKeyword => LogicalSchema.Create(LogicalTypeNames.LocalTimestampMillis, AvroSchema.Long, schemaRegistry.Options.TargetProfile),
                SyntaxKind.UuidKeyword => LogicalSchema.Create(LogicalTypeNames.Uuid, AvroSchema.String, schemaRegistry.Options.TargetProfile),
                _ => throw new InvalidSourceException($"Invalid logical type syntax: {syntax.SyntaxKind}", logical.LogicalTypeNameKeyword.SourceSpan)
            };
        }

        private ProtocolSchema Protocol(ProtocolDeclarationSyntax syntax, string? containingNamespace)
        {
            schemaRegistry.ValidateImports(syntax.Imports);

            var schemaName = syntax.GetRequiredSchemaName(containingNamespace);
            using (schemaRegistry.EnterRecursionScope(schemaName))
            {
                var documentation = syntax.GetDocumentation();
                var types = schemaRegistry.ProtocolTypes(syntax.Types, schemaName.Namespace);
                var messages = schemaRegistry.ProtocolMessages(syntax.Messages, schemaName.Namespace);
                var properties = syntax.GetProtocolProperties();

                var protocolSchema = new ProtocolSchema(schemaName, documentation, types, messages, properties);

                schemaRegistry.Register(protocolSchema);

                return protocolSchema;
            }
        }

        private ImmutableArray<NamedSchema> ProtocolTypes(SyntaxList<ISchemaDeclarationSyntax> syntaxList, string? containingNamespace)
        {
            var types = ImmutableArray.CreateBuilder<NamedSchema>();
            foreach (var type in syntaxList)
                types.Add(schemaRegistry.Schema(type, containingNamespace));

            return types.ToImmutable();
        }

        private ImmutableArray<ProtocolMessage> ProtocolMessages(SyntaxList<MessageDeclarationSyntax> syntaxList, string? containingNamespace)
        {
            var protocolMessages = ImmutableArray.CreateBuilder<ProtocolMessage>();
            foreach (var syntax in syntaxList)
                protocolMessages.Add(schemaRegistry.Message(syntax, containingNamespace));
            return protocolMessages.ToImmutable();
        }

        private ProtocolMessage Message(MessageDeclarationSyntax syntax, string? containingNamespace)
        {
            var methodName = syntax.Name.FullName.ToValidName();
            var documentation = syntax.GetDocumentation();
            var requestParameters = schemaRegistry.ProtocolRequestParameters(syntax.Parameters, containingNamespace);
            var response = schemaRegistry.ProtocolResponse(syntax.Type, containingNamespace);
            var errors = schemaRegistry.ProtocolErrors(syntax.ThrowsErrorClause, containingNamespace);
            var oneWay = syntax.OneWayClause is not null ? true : default(bool?);
            if (oneWay is true && (response.Type.Type is not SchemaType.Null || errors.Length > 0))
            {
                throw new InvalidSourceException($"One-way protocol message '{syntax.Name.FullName}' must have a null response and no errors.", syntax.OneWayClause!.OneWayKeyword.SourceSpan);
            }

            return new ProtocolMessage(methodName, documentation, requestParameters, response, errors, oneWay);
        }

        private ImmutableArray<ProtocolRequestParameter> ProtocolRequestParameters(SeparatedSyntaxList<ParameterDeclarationSyntax> syntaxList, string? containingNamespace)
        {
            var fields = ImmutableArray.CreateBuilder<ProtocolRequestParameter>();
            foreach (var syntax in syntaxList)
                fields.Add(schemaRegistry.ProtocolRequestParameter(syntax, containingNamespace));

            return fields.ToImmutable();
        }

        private ProtocolRequestParameter ProtocolRequestParameter(ParameterDeclarationSyntax syntax, string? containingNamespace)
        {
            var name = syntax.Name.FullName.ToValidName();
            var defaultJson = syntax.DefaultValueClause?.JsonValue.ToOptionalJsonElement();
            var type = schemaRegistry.Type(syntax.Type, containingNamespace, defaultJson: defaultJson);
            var underlyingType = type is UnionSchema union ? union.UnderlyingSchema : type;

            var documentation = syntax.GetDocumentation();
            var @default = type.GetValue(defaultJson);
            return new ProtocolRequestParameter(name, type, underlyingType, documentation, defaultJson, @default);
        }

        private ProtocolResponse ProtocolResponse(ITypeSyntax syntax, string? containingNamespace)
        {
            var type = schemaRegistry.Type(syntax, containingNamespace);
            var underlyingType = type is UnionSchema union ? union.UnderlyingSchema : type;

            return new ProtocolResponse(type, underlyingType);
        }

        private ImmutableArray<AvroSchema> ProtocolErrors(ThrowsErrorClauseSyntax? syntax, string? containingNamespace)
        {
            if (syntax is null || syntax.Errors is [])
            {
                return [];
            }

            var builder = ImmutableArray.CreateBuilder<AvroSchema>();
            foreach (var errorSyntax in syntax.Errors)
            {
                // TODO: Do we need to validate that this is an error schema?
                builder.Add(schemaRegistry.Type(errorSyntax, containingNamespace));
            }

            return builder.ToImmutable();
        }
    }

    private static SourceSpan GetSourceSpan(SyntaxTree syntaxTree)
    {
        return TryGetFirstSourceSpan(syntaxTree.Document, out var sourceSpan)
            ? sourceSpan
            : syntaxTree.SourceText.GetSpan(0, syntaxTree.SourceText.Text.Length);
    }

    private static SourceSpan GetSourceSpan(ISyntaxNode syntax)
    {
        return TryGetFirstSourceSpan(syntax, out var sourceSpan)
            ? sourceSpan
            : throw new InvalidOperationException($"Syntax node '{syntax.SyntaxKind}' has no source span.");
    }

    private static bool TryGetFirstSourceSpan(ISyntaxNode syntax, out SourceSpan sourceSpan)
    {
        if (syntax is SyntaxToken token)
        {
            sourceSpan = token.SourceSpan;
            return true;
        }

        foreach (var child in syntax.Children())
        {
            if (TryGetFirstSourceSpan(child, out sourceSpan))
            {
                return true;
            }
        }

        sourceSpan = default;
        return false;
    }
}
