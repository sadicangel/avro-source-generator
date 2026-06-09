using AvroSourceGenerator.Avdl.Diagnostics;
using AvroSourceGenerator.Avdl.Syntax;
using AvroSourceGenerator.Avdl.Syntax.Annotations;
using AvroSourceGenerator.Avdl.Syntax.Declarations;
using AvroSourceGenerator.Avdl.Syntax.Directives;
using AvroSourceGenerator.Avdl.Syntax.Types;

namespace AvroSourceGenerator.Tests.Avdl;

public sealed class ParserTests
{
    [Fact]
    public void Parse_DirectivesAndProtocol_ReturnsDocument()
    {
        var unit = Parse(
            """
            namespace example.avro;
            schema Main;
            import idl "dep.avdl";
            import protocol "dep.avpr";
            import schema "dep.avsc";
            protocol P {}
            """);

        Assert.Equal(5, unit.Directives.Count);
        Assert.IsType<QualifiedNameSyntax>(Assert.IsType<NamespaceDirectiveSyntax>(unit.Directives[0]).NamespaceName);
        Assert.Equal("Main", Assert.IsType<SchemaDirectiveSyntax>(unit.Directives[1]).MainSchemaName!.Identifier.SourceSpan.ToString());

        var idlImport = Assert.IsType<ImportDirectiveSyntax>(unit.Directives[2]);
        Assert.Equal(SyntaxKind.IdlKeyword, idlImport.ImportTypeKeyword.SyntaxKind);
        Assert.Equal("dep.avdl", idlImport.ImportPathLiteralToken.Value);

        var protocolImport = Assert.IsType<ImportDirectiveSyntax>(unit.Directives[3]);
        Assert.Equal(SyntaxKind.ProtocolKeyword, protocolImport.ImportTypeKeyword.SyntaxKind);
        Assert.Equal("dep.avpr", protocolImport.ImportPathLiteralToken.Value);

        var schemaImport = Assert.IsType<ImportDirectiveSyntax>(unit.Directives[4]);
        Assert.Equal(SyntaxKind.SchemaKeyword, schemaImport.ImportTypeKeyword.SyntaxKind);
        Assert.Equal("dep.avsc", schemaImport.ImportPathLiteralToken.Value);

        var protocol = Assert.IsType<ProtocolDeclarationSyntax>(Assert.Single(unit.Declarations));
        Assert.Equal("P", protocol.Name.Identifier.SourceSpan.ToString());
        Assert.Empty(protocol.Imports);
    }

    [Theory]
    [InlineData("schema int;", SyntaxKind.IntType)]
    [InlineData("schema array<string>;", SyntaxKind.ArrayType)]
    [InlineData("schema union { null, string };", SyntaxKind.UnionType)]
    [InlineData("schema Message;", SyntaxKind.NamedType)]
    public void Parse_SchemaDirective_ReturnsMainSchemaType(string text, SyntaxKind expectedTypeKind)
    {
        var unit = Parse(text);

        var schema = Assert.IsType<SchemaDirectiveSyntax>(Assert.Single(unit.Directives));
        Assert.Equal(expectedTypeKind, schema.MainSchemaType.SyntaxKind);
    }

    [Fact]
    public void Parse_ProtocolImports_ReturnsImportsOnProtocol()
    {
        var unit = Parse(
            """
            protocol P {
                import idl "common.avdl";
                import protocol "dep.avpr";
                import schema "dep.avsc";

                record Request {
                    string name;
                }

                void ping();
            }
            """);

        Assert.Empty(unit.Directives);

        var protocol = Assert.IsType<ProtocolDeclarationSyntax>(Assert.Single(unit.Declarations));
        Assert.Equal(3, protocol.Imports.Count);

        var idlImport = protocol.Imports[0];
        Assert.Equal(SyntaxKind.IdlKeyword, idlImport.ImportTypeKeyword.SyntaxKind);
        Assert.Equal("common.avdl", idlImport.ImportPathLiteralToken.Value);

        var protocolImport = protocol.Imports[1];
        Assert.Equal(SyntaxKind.ProtocolKeyword, protocolImport.ImportTypeKeyword.SyntaxKind);
        Assert.Equal("dep.avpr", protocolImport.ImportPathLiteralToken.Value);

        var schemaImport = protocol.Imports[2];
        Assert.Equal(SyntaxKind.SchemaKeyword, schemaImport.ImportTypeKeyword.SyntaxKind);
        Assert.Equal("dep.avsc", schemaImport.ImportPathLiteralToken.Value);

        Assert.Single(protocol.Types);
        Assert.Single(protocol.Messages);

        var kinds = AvdlTestHelpers.FlattenKinds(unit);
        Assert.Equal(3, kinds.Count(kind => kind == SyntaxKind.ImportDirective));
    }

    [Fact]
    public void Parse_TopLevelDeclarations_ReturnsDeclarationShapesAndMetadata()
    {
        var unit = Parse(
            """
            /** Kind doc */
            @aliases(["OldKind", "OlderKind"])
            enum Kind { A, B } = A;

            fixed MD5(16);

            record User {
                string @order("ignore") name = "Ada";
                int result_code = -1;
                map<string> config = { "x": 1, "ok": true, "tags": ["a", "b"] };
            }

            error Problem {
                string message;
            }
            """);

        Assert.Equal(4, unit.Declarations.Count);

        var enumDeclaration = Assert.IsType<EnumDeclarationSyntax>(unit.Declarations[0]);
        Assert.Equal("Kind", enumDeclaration.Name.Identifier.SourceSpan.ToString());
        Assert.Equal(" Kind doc ", Assert.Single(enumDeclaration.Documentation).DocumentationTrivia.SourceSpan.ToString());
        var aliases = Assert.IsType<AliasesAnnotationSyntax>(Assert.Single(enumDeclaration.Annotations));
        Assert.Equal(["OldKind", "OlderKind"], aliases.Aliases);
        Assert.Equal(2, Assert.IsType<JsonArray>(aliases.JsonValue.JsonNode).Count);
        Assert.Equal(2, enumDeclaration.Symbols.Count);
        Assert.Equal("A", enumDeclaration.DefaultValue!.JsonValue.JsonNode?.GetValue<string>());

        var fixedDeclaration = Assert.IsType<FixedDeclarationSyntax>(unit.Declarations[1]);
        Assert.Equal("MD5", fixedDeclaration.Name.Identifier.SourceSpan.ToString());
        Assert.Equal(16, fixedDeclaration.SizeLiteralToken.Value);
        Assert.Empty(fixedDeclaration.Documentation);
        Assert.Empty(fixedDeclaration.Annotations);

        var recordDeclaration = Assert.IsType<RecordDeclarationSyntax>(unit.Declarations[2]);
        Assert.Equal("User", recordDeclaration.Name.Identifier.SourceSpan.ToString());
        Assert.Equal(3, recordDeclaration.Fields.Count);
        Assert.Empty(recordDeclaration.Documentation);
        Assert.Empty(recordDeclaration.Annotations);

        var nameField = recordDeclaration.Fields[0];
        Assert.Equal(SyntaxKind.StringType, nameField.Type.SyntaxKind);
        Assert.Equal("name", nameField.Name.Identifier.SourceSpan.ToString());
        var order = Assert.IsType<OrderAnnotationSyntax>(Assert.Single(nameField.Annotations));
        Assert.Equal("ignore", order.Order);
        Assert.Equal("Ada", nameField.DefaultValueClause!.JsonValue.JsonNode?.GetValue<string>());

        var resultCodeField = recordDeclaration.Fields[1];
        Assert.Equal("result_code", resultCodeField.Name.Identifier.SourceSpan.ToString());
        Assert.Equal(-1, resultCodeField.DefaultValueClause!.JsonValue.JsonNode?.GetValue<int>());

        var configDefault = Assert.IsType<JsonObject>(recordDeclaration.Fields[2].DefaultValueClause!.JsonValue.JsonNode);
        Assert.Equal(1, configDefault["x"]!.GetValue<int>());
        Assert.True(configDefault["ok"]!.GetValue<bool>());
        Assert.Equal("b", Assert.IsType<JsonArray>(configDefault["tags"]).Last()!.GetValue<string>());

        var errorDeclaration = Assert.IsType<ErrorDeclarationSyntax>(unit.Declarations[3]);
        Assert.Equal("Problem", errorDeclaration.Name.Identifier.SourceSpan.ToString());
        Assert.Single(errorDeclaration.Fields);
    }

    [Fact]
    public void Parse_Protocol_ReturnsTypesMessagesAndClauses()
    {
        var unit = Parse(
            """
            @namespace("example")
            protocol Service {
                enum Kind { A }
                fixed MD5(16);
                record Request {
                    array<long> ids;
                    map<string> tags;
                    union { null, string } maybeName;
                    string? shorthand;
                    @aliases(["oldName"]) string aliased;
                }
                error Problem {
                    string message;
                }

                void ping() oneway;
                string hello(string greeting, int count = 1) throws Problem, Other;
            }
            """);

        var protocol = Assert.IsType<ProtocolDeclarationSyntax>(Assert.Single(unit.Declarations));
        Assert.Equal("Service", protocol.Name.Identifier.SourceSpan.ToString());
        var namespaceAnnotation = Assert.IsType<NamespaceAnnotationSyntax>(Assert.Single(protocol.Annotations));
        Assert.Equal("example", namespaceAnnotation.Namespace);
        Assert.Empty(protocol.Imports);
        Assert.Equal(4, protocol.Types.Count);
        Assert.Equal(2, protocol.Messages.Count);

        var request = Assert.IsType<RecordDeclarationSyntax>(protocol.Types[2]);
        Assert.Equal(5, request.Fields.Count);
        Assert.IsType<ArrayTypeSyntax>(request.Fields[0].Type);
        Assert.IsType<MapTypeSyntax>(request.Fields[1].Type);
        Assert.IsType<UnionTypeSyntax>(request.Fields[2].Type);
        Assert.IsType<OptionalTypeSyntax>(request.Fields[3].Type);
        Assert.Empty(request.Fields[4].Annotations);
        Assert.Single(Assert.IsType<AnnotatedTypeSyntax>(request.Fields[4].Type).Annotations);

        var ping = protocol.Messages[0];
        Assert.Equal(SyntaxKind.VoidType, ping.Type.SyntaxKind);
        Assert.Equal("ping", ping.Name.Identifier.SourceSpan.ToString());
        Assert.NotNull(ping.OneWayClause);
        Assert.Null(ping.ThrowsErrorClause);

        var hello = protocol.Messages[1];
        Assert.Equal(SyntaxKind.StringType, hello.Type.SyntaxKind);
        Assert.Equal("hello", hello.Name.Identifier.SourceSpan.ToString());
        Assert.Equal(2, hello.Parameters.Count);
        Assert.Equal("greeting", hello.Parameters[0].Name.Identifier.SourceSpan.ToString());
        Assert.Equal(1, hello.Parameters[1].DefaultValueClause!.JsonValue.JsonNode?.GetValue<int>());
        Assert.Equal(2, hello.ThrowsErrorClause!.Errors.Count);
    }

    [Fact]
    public void Parse_VerbatimIdentifiers_AllowsKeywordsAsNames()
    {
        var unit = Parse(
            """
            protocol P {
                void `error`(string `record`);
            }
            """);

        var message = Assert.IsType<ProtocolDeclarationSyntax>(Assert.Single(unit.Declarations)).Messages[0];

        Assert.Equal("`error`", message.Name.Identifier.SourceSpan.ToString());
        Assert.Equal("error", message.Name.FullName);
        Assert.Equal("error", message.Name.Identifier.ValueText);
        Assert.Equal("`record`", message.Parameters[0].Name.Identifier.SourceSpan.ToString());
        Assert.Equal("record", message.Parameters[0].Name.FullName);
    }

    [Fact]
    public void Parse_CustomAnnotation_AllowsDashInName()
    {
        var unit = Parse(
            """
            @java-class("com.example.User")
            record User {}
            """);

        var record = Assert.IsType<RecordDeclarationSyntax>(Assert.Single(unit.Declarations));
        var annotation = Assert.IsType<CustomAnnotationSyntax>(Assert.Single(record.Annotations));

        Assert.Equal("java-class", annotation.AnnotationName.FullName);
        Assert.Equal("com.example.User", annotation.JsonValue.JsonNode!.GetValue<string>());
    }

    [Fact]
    public void Parse_LogicalTypeAnnotation_ReturnsLogicalTypeAnnotation()
    {
        var unit = Parse(
            """
            record User {
                @logicalType("uuid") string id;
                @logicalType("timestamp-millis") long createdAt;
            }
            """);

        var record = Assert.IsType<RecordDeclarationSyntax>(Assert.Single(unit.Declarations));

        var id = record.Fields[0];
        Assert.Empty(id.Annotations);
        var idType = Assert.IsType<AnnotatedTypeSyntax>(id.Type);
        var uuid = Assert.IsType<LogicalTypeAnnotationSyntax>(Assert.Single(idType.Annotations));
        Assert.Equal("logicalType", uuid.AnnotationName.FullName);
        Assert.Equal("uuid", uuid.LogicalTypeName);
        Assert.Equal("uuid", uuid.JsonValue.JsonNode!.GetValue<string>());
        Assert.Equal(SyntaxKind.StringType, idType.Type.SyntaxKind);

        var createdAt = record.Fields[1];
        Assert.Empty(createdAt.Annotations);
        var createdAtType = Assert.IsType<AnnotatedTypeSyntax>(createdAt.Type);
        var timestampMillis = Assert.IsType<LogicalTypeAnnotationSyntax>(Assert.Single(createdAtType.Annotations));
        Assert.Equal("timestamp-millis", timestampMillis.LogicalTypeName);
        Assert.Equal(SyntaxKind.LongType, createdAtType.Type.SyntaxKind);
    }

    [Fact]
    public void Parse_MessageMetadata_ReturnsDocumentationAndAnnotationsOnMessage()
    {
        var tree = ParseTree(
            """
            protocol P {
                /** Ping doc */
                @timeout-ms(1000)
                void ping();
            }
            """);

        Assert.Empty(tree.Diagnostics);
        var protocol = Assert.IsType<ProtocolDeclarationSyntax>(Assert.Single(tree.Document.Declarations));
        var message = Assert.Single(protocol.Messages);
        Assert.Equal(" Ping doc ", Assert.Single(message.Documentation).DocumentationTrivia.SourceSpan.ToString());
        var annotation = Assert.IsType<CustomAnnotationSyntax>(Assert.Single(message.Annotations));
        Assert.Equal("timeout-ms", annotation.AnnotationName.FullName);
    }

    [Fact]
    public void Parse_CustomAnnotation_AllowsDottedAndDashedName()
    {
        var unit = Parse(
            """
            @foo.bar-baz(true)
            record User {}
            """);

        var record = Assert.IsType<RecordDeclarationSyntax>(Assert.Single(unit.Declarations));
        var annotation = Assert.IsType<CustomAnnotationSyntax>(Assert.Single(record.Annotations));

        Assert.Equal("foo.bar-baz", annotation.AnnotationName.FullName);
        Assert.True(annotation.JsonValue.JsonNode!.GetValue<bool>());
    }

    [Fact]
    public void Parse_NestedTypeAnnotation_ReturnsAnnotatedTypeSyntax()
    {
        var unit = Parse(
            """
            record Weights {
                array<@java-class("java.math.BigDecimal") string> weights;
            }
            """);

        var record = Assert.IsType<RecordDeclarationSyntax>(Assert.Single(unit.Declarations));
        var array = Assert.IsType<ArrayTypeSyntax>(Assert.Single(record.Fields).Type);
        var itemType = Assert.IsType<AnnotatedTypeSyntax>(array.ItemType);
        var annotation = Assert.IsType<CustomAnnotationSyntax>(Assert.Single(itemType.Annotations));

        Assert.Equal("java-class", annotation.AnnotationName.FullName);
        Assert.Equal(SyntaxKind.StringType, itemType.Type.SyntaxKind);
    }

    [Fact]
    public void Parse_LogicalTypes_ReturnsLogicalTypeSyntax()
    {
        var unit = Parse(
            """
            record Logical {
                date birthday;
                time_ms wakeUp;
                timestamp_ms createdAt;
                local_timestamp_ms localCreatedAt;
                uuid id;
                decimal(12, 4) amount;
                array<uuid> ids;
                map<decimal(9, 2)> amounts;
                union { null, date, decimal(18, 6) } maybe;
            }
            """);

        var record = Assert.IsType<RecordDeclarationSyntax>(Assert.Single(unit.Declarations));
        Assert.Equal(9, record.Fields.Count);

        AssertLogicalType(record.Fields[0].Type, SyntaxKind.DateKeyword);
        AssertLogicalType(record.Fields[1].Type, SyntaxKind.TimeMsKeyword);
        AssertLogicalType(record.Fields[2].Type, SyntaxKind.TimestampMsKeyword);
        AssertLogicalType(record.Fields[3].Type, SyntaxKind.LocalTimestampMsKeyword);
        AssertLogicalType(record.Fields[4].Type, SyntaxKind.UuidKeyword);

        var amount = Assert.IsType<DecimalLogicalTypeSyntax>(record.Fields[5].Type);
        Assert.Equal(SyntaxKind.DecimalKeyword, amount.DecimalKeyword.SyntaxKind);
        Assert.Equal(12, amount.PrecisionLiteralToken.Value);
        Assert.Equal(4, amount.ScaleLiteralToken.Value);

        var ids = Assert.IsType<ArrayTypeSyntax>(record.Fields[6].Type);
        AssertLogicalType(ids.ItemType, SyntaxKind.UuidKeyword);

        var amounts = Assert.IsType<MapTypeSyntax>(record.Fields[7].Type);
        var mapDecimal = Assert.IsType<DecimalLogicalTypeSyntax>(amounts.ValueType);
        Assert.Equal(9, mapDecimal.PrecisionLiteralToken.Value);
        Assert.Equal(2, mapDecimal.ScaleLiteralToken.Value);

        var maybe = Assert.IsType<UnionTypeSyntax>(record.Fields[8].Type);
        Assert.Equal(3, maybe.Types.Count);
        Assert.Equal(SyntaxKind.NullType, maybe.Types[0].SyntaxKind);
        AssertLogicalType(maybe.Types[1], SyntaxKind.DateKeyword);
        var unionDecimal = Assert.IsType<DecimalLogicalTypeSyntax>(maybe.Types[2]);
        Assert.Equal(18, unionDecimal.PrecisionLiteralToken.Value);
        Assert.Equal(6, unionDecimal.ScaleLiteralToken.Value);

        var kinds = AvdlTestHelpers.FlattenKinds(unit);
        Assert.Equal(10, kinds.Count(kind => kind == SyntaxKind.LogicalType));
    }

    [Fact]
    public void Parse_FlattensExpectedSyntaxKinds()
    {
        var unit = Parse("record User { string name; }");

        var kinds = AvdlTestHelpers.FlattenKinds(unit);

        Assert.Contains(SyntaxKind.Document, kinds);
        Assert.Contains(SyntaxKind.RecordDeclaration, kinds);
        Assert.Contains(SyntaxKind.FieldDeclaration, kinds);
        Assert.Contains(SyntaxKind.StringType, kinds);
        Assert.Contains(SyntaxKind.IdentifierToken, kinds);
    }

    [Fact]
    public void Parse_InvalidScannerInput_ReturnsSyntaxTreeDiagnostic()
    {
        var tree = ParseTree("$ record User { string name; }");

        Assert.Contains(tree.Diagnostics, diagnostic => diagnostic.Code == SyntaxDiagnosticCode.InvalidCharacter);
    }

    [Fact]
    public void Parse_MissingExpectedToken_ReturnsUnexpectedTokenDiagnosticAtCurrentTokenOffset()
    {
        const string text = "record User { string name }";
        var tree = ParseTree(text);

        var diagnostic = Assert.Single(tree.Diagnostics, diagnostic => diagnostic.Code == SyntaxDiagnosticCode.UnexpectedToken);
        Assert.Equal(text.IndexOf('}'), diagnostic.SourceSpan.Offset);
        Assert.Equal(1, diagnostic.SourceSpan.Length);
    }

    [Fact]
    public void Parse_InvalidJsonDefault_ReturnsUnexpectedJsonValueDiagnostic()
    {
        var tree = ParseTree("record User { string name = ; }");

        Assert.Contains(tree.Diagnostics, diagnostic => diagnostic.Code == SyntaxDiagnosticCode.UnexpectedJsonValue);
    }

    [Fact]
    public void Parse_KnownAnnotationsWithUnexpectedValues_DoesNotReturnDiagnostics()
    {
        var tree = ParseTree(
            """
            @namespace(1)
            @aliases(["OldUser", 2])
            record User {
                @order("sideways") string name;
                @logicalType(42) string id;
            }
            """);

        Assert.Empty(tree.Diagnostics);
    }

    [Fact]
    public void Parse_MisplacedMetadata_ReturnsDiagnosticsAndDoesNotLeakToNextDeclaration()
    {
        var tree = ParseTree(
            """
            protocol P {
                @order("ignore")
                void ping();

                /** Import doc */
                @namespace("example")
                import idl "common.avdl";

                record User {}
            }
            """);

        Assert.Contains(tree.Diagnostics, diagnostic => diagnostic.Code == SyntaxDiagnosticCode.MisplacedAnnotation && diagnostic.SourceSpan.ToString() == "@");
        Assert.Contains(tree.Diagnostics, diagnostic => diagnostic.Code == SyntaxDiagnosticCode.MisplacedDocumentation && diagnostic.SourceSpan.ToString() == " Import doc ");

        var protocol = Assert.IsType<ProtocolDeclarationSyntax>(Assert.Single(tree.Document.Declarations));
        Assert.Empty(Assert.IsType<RecordDeclarationSyntax>(Assert.Single(protocol.Types)).Annotations);
    }

    [Fact]
    public void Parse_MetadataAtEndOfFile_DoesNotReturnDiagnostics()
    {
        var tree = ParseTree(
            """
            record User {}
            /** trailing */
            @aliases(["OldUser"])
            """);

        Assert.Empty(tree.Diagnostics);
    }

    private static void AssertLogicalType(ITypeSyntax type, SyntaxKind logicalTypeKeyword)
    {
        var logicalType = Assert.IsType<LogicalTypeSyntax>(type);
        Assert.Equal(logicalTypeKeyword, logicalType.LogicalTypeNameKeyword.SyntaxKind);
    }

    private static DocumentSyntax Parse(string text) => Parser.Parse(AvdlTestHelpers.SourceText(text)).Document;

    private static SyntaxTree ParseTree(string text) => Parser.Parse(AvdlTestHelpers.SourceText(text));
}
