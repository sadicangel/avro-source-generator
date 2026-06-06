using AvroSourceGenerator.Avdl;
using AvroSourceGenerator.Avdl.Syntax;
using AvroSourceGenerator.Avdl.Syntax.Declarations;
using AvroSourceGenerator.Avdl.Syntax.Directives;
using AvroSourceGenerator.Avdl.Syntax.Types;

namespace AvroSourceGenerator.Tests.Avdl;

public sealed class ParserTests
{
    [Fact]
    public void Parse_DirectivesAndProtocol_ReturnsCompilationUnit()
    {
        var unit = Parse("""
            namespace example.avro;
            schema Main;
            import idl "dep.avdl";
            import protocol "dep.avpr";
            import schema "dep.avsc";
            protocol P {}
            """);

        Assert.Equal(5, unit.Directives.Count);
        Assert.IsType<QualifiedNameSyntax>(Assert.IsType<NamespaceDirectiveSyntax>(unit.Directives[0]).NamespaceName);
        Assert.Equal("Main", Assert.IsType<SchemaDirectiveSyntax>(unit.Directives[1]).MainSchemaName.Identifier.SourceSpan.ToString());

        var idlImport = Assert.IsType<ImportDirectiveSyntax>(unit.Directives[2]);
        Assert.Equal(SyntaxKind.IdlKeyword, idlImport.ImportTypeKeyword.SyntaxKind);
        Assert.Equal("dep.avdl", idlImport.ImportPathLiteralToken.Value);

        var protocolImport = Assert.IsType<ImportDirectiveSyntax>(unit.Directives[3]);
        Assert.Equal(SyntaxKind.ProtocolKeyword, protocolImport.ImportTypeKeyword.SyntaxKind);
        Assert.Equal("dep.avpr", protocolImport.ImportPathLiteralToken.Value);

        var schemaImport = Assert.IsType<ImportDirectiveSyntax>(unit.Directives[4]);
        Assert.Equal(SyntaxKind.SchemaKeyword, schemaImport.ImportTypeKeyword.SyntaxKind);
        Assert.Equal("dep.avsc", schemaImport.ImportPathLiteralToken.Value);

        var protocol = Assert.Single(unit.Declarations);
        Assert.Equal("P", Assert.IsType<ProtocolDeclarationSyntax>(protocol).Name.Identifier.SourceSpan.ToString());
    }

    [Fact]
    public void Parse_TopLevelDeclarations_ReturnsDeclarationShapesAndMetadata()
    {
        var unit = Parse("""
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
        Assert.Equal(2, Assert.IsType<JsonArray>(Assert.Single(enumDeclaration.Annotations).Json!.Json).Count);
        Assert.Equal(2, enumDeclaration.Symbols.Count);
        Assert.Equal("A", enumDeclaration.DefaultValue!.JsonValue.Json!.GetValue<string>());

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
        Assert.Equal("ignore", Assert.Single(nameField.Annotations).Json!.Json!.GetValue<string>());
        Assert.Equal("Ada", nameField.DefaultValueClause!.JsonValue.Json!.GetValue<string>());

        var resultCodeField = recordDeclaration.Fields[1];
        Assert.Equal("result_code", resultCodeField.Name.Identifier.SourceSpan.ToString());
        Assert.Equal(-1, resultCodeField.DefaultValueClause!.JsonValue.Json!.GetValue<int>());

        var configDefault = Assert.IsType<JsonObject>(recordDeclaration.Fields[2].DefaultValueClause!.JsonValue.Json);
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
        var unit = Parse("""
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
        Assert.Equal("example", Assert.Single(protocol.Annotations).Json!.Json!.GetValue<string>());
        Assert.Equal(4, protocol.Types.Count);
        Assert.Equal(2, protocol.Messages.Count);

        var request = Assert.IsType<RecordDeclarationSyntax>(protocol.Types[2]);
        Assert.Equal(5, request.Fields.Count);
        Assert.IsType<ArrayTypeSyntax>(request.Fields[0].Type);
        Assert.IsType<MapTypeSyntax>(request.Fields[1].Type);
        Assert.IsType<UnionTypeSyntax>(request.Fields[2].Type);
        Assert.IsType<OptionalTypeSyntax>(request.Fields[3].Type);
        Assert.Single(request.Fields[4].Annotations);

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
        Assert.Equal(1, hello.Parameters[1].DefaultValueClause!.JsonValue.Json!.GetValue<int>());
        Assert.Equal(2, hello.ThrowsErrorClause!.Errors.Count);
    }

    [Fact]
    public void Parse_VerbatimIdentifiers_AllowsKeywordsAsNames()
    {
        var unit = Parse("""
            protocol P {
                void `error`(string `record`);
            }
            """);

        var message = Assert.IsType<ProtocolDeclarationSyntax>(Assert.Single(unit.Declarations)).Messages[0];

        Assert.Equal("error", message.Name.Identifier.SourceSpan.ToString());
        Assert.Equal("record", message.Parameters[0].Name.Identifier.SourceSpan.ToString());
    }

    [Fact]
    public void Parse_FlattensExpectedSyntaxKinds()
    {
        var unit = Parse("record User { string name; }");

        var kinds = AvdlTestHelpers.FlattenKinds(unit);

        Assert.Contains(SyntaxKind.CompilationUnit, kinds);
        Assert.Contains(SyntaxKind.RecordDeclaration, kinds);
        Assert.Contains(SyntaxKind.FieldDeclaration, kinds);
        Assert.Contains(SyntaxKind.StringType, kinds);
        Assert.Contains(SyntaxKind.IdentifierToken, kinds);
    }

    private static CompilationUnitSyntax Parse(string text) => Parser.Parse(AvdlTestHelpers.SourceText(text));
}
