using System.Xml.Linq;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AvroSourceGenerator.UnitTests;

public sealed class UnionDocumentationTests
{
    [Theory]
    [InlineData(false, LanguageFeatures.CSharp14)]
    [InlineData(true, LanguageFeatures.CSharp14)]
    [InlineData(false, LanguageFeatures.CSharp15)]
    [InlineData(true, LanguageFeatures.CSharp15)]
    public void Nullable_field_unions_preserve_the_underlying_type(bool nullFirst, LanguageFeatures features)
    {
        (string Schema, string CSharpName)[] cases =
        [
            ("\"int\"", "int?"),
            ("\"string\"", "string?"),
            ("\"bytes\"", "byte[]?"),
            ("""{"type":"record","name":"Child","fields":[]}""", "Child?"),
            ("""{"type":"array","items":"int"}""", "global::System.Collections.Generic.List<int>?"),
            ("""{"type":"map","values":"string"}""", "global::System.Collections.Generic.Dictionary<string, string>?"),
            ("""{"type":"array","items":{"type":"map","values":"bytes"}}""", "global::System.Collections.Generic.List<global::System.Collections.Generic.Dictionary<string, byte[]>>?"),
            ("""{"type":"map","values":{"type":"array","items":"int"}}""", "global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<int>>?"),
        ];
        var fields = cases.Select((test, index) => $$"""{"name":"Field{{index}}","type":{{Nullable(test.Schema, nullFirst)}}}""");
        var bound = Bind(Record(fields), features);
        var record = Assert.IsType<RecordSchema>(bound.RootSchema);

        Assert.DoesNotContain(bound.Declarations, schema => schema is UnionTypeSchema);
        for (var index = 0; index < cases.Length; index++)
        {
            var field = record.Fields[index];
            var union = Assert.IsType<UnionSchema>(field.Type);

            Assert.Equal(SchemaType.Null, union.Schemas[nullFirst ? 0 : 1].SchemaType);
            Assert.True(field.AllowsNull);
            Assert.Equal(cases[index].CSharpName, field.Type.CSharpName.FullName);
            Assert.Same(union.UnderlyingSchema, field.UnderlyingType);
            Assert.Null(field.Remarks);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Nested_nullable_unions_have_valid_documentation(bool nullFirst)
    {
        const string nullElement = "<see langword=\"null\"/>";
        const string intElement = "<see cref=\"int\"/>";
        const string stringElement = "<see cref=\"string\"/>";
        const string bytesElement = "<see cref=\"byte\"/>[]";
        const string childElement = "<see cref=\"Child\"/>";
        var nullableInt = Nullable("\"int\"", nullFirst);
        var nullableBytes = Nullable("\"bytes\"", nullFirst);
        var nullableChild = Nullable("\"Child\"", nullFirst);
        var nullableArray = Nullable($$"""{"type":"array","items":{{nullableInt}}}""", nullFirst);
        var nullableMap = Nullable($$"""{"type":"map","values":{{nullableBytes}}}""", nullFirst);
        (string Schema, string Documentation)[] cases =
        [
            ("\"bytes\"", bytesElement),
            ($$"""{"type":"array","items":{{nullableInt}}}""", List($"{nullElement} | {intElement}")),
            ($$"""{"type":"map","values":{{nullableBytes}}}""", Map($"{nullElement} | {bytesElement}")),
            ($$"""{"type":"array","items":{{nullableArray}}}""", List($"{nullElement} | {List($"{nullElement} | {intElement}")}")),
            ($$"""{"type":"map","values":{{nullableMap}}}""", Map($"{nullElement} | {Map($"{nullElement} | {bytesElement}")}")),
            ($$"""{"type":"array","items":{{nullableMap}}}""", List($"{nullElement} | {Map($"{nullElement} | {bytesElement}")}")),
            ($$"""{"type":"map","values":{{nullableArray}}}""", Map($"{nullElement} | {List($"{nullElement} | {intElement}")}")),
            ($$"""{"type":"array","items":{{nullableChild}}}""", List($"{nullElement} | {childElement}")),
        ];
        var fields = cases.Select((test, index) =>
        {
            var branches = nullFirst ? $"\"null\", {test.Schema}, \"string\"" : $"\"string\", {test.Schema}, \"null\"";
            return $$"""{"name":"Field{{index}}","type":[{{branches}}]}""";
        }).Prepend("""{"name":"Child","type":{"type":"record","name":"Child","fields":[]}}""");
        var schema = Record(fields);
        var bound = Bind(schema, LanguageFeatures.CSharp15);
        var record = Assert.IsType<RecordSchema>(bound.RootSchema);

        for (var index = 0; index < cases.Length; index++)
        {
            var field = record.Fields[index + 1];
            var union = Assert.IsType<UnionTypeSchema>(field.UnderlyingType);
            var members = index == 0
                ? new[] { cases[index].Documentation, stringElement }
                : new[] { stringElement, cases[index].Documentation };

            Assert.Equal(Documentation(members), union.Documentation!.ReplaceLineEndings("\n"));
            Assert.Equal(Documentation([nullElement, .. members]), field.Remarks!.ReplaceLineEndings("\n"));
            Assert.Equal(2, XElement.Parse($"<summary>{union.Documentation}</summary>").Element("list")!.Elements("item").Count());
            Assert.Equal(3, XElement.Parse($"<remarks>{field.Remarks}</remarks>").Element("list")!.Elements("item").Count());
        }

        var input = GeneratorInput.Create([ProjectFile.Schema(schema)], [], new ProjectConfig(LanguageVersion.Preview)
        {
            AvroLibrary = "None",
            LanguageFeatures = "CSharp15",
            PreviewFeatures = "Unions",
        });
        var driver = input.GeneratorDriver.RunGeneratorsAndUpdateCompilation(
            input.Compilation, out var compilation, out var diagnostics, TestContext.Current.CancellationToken);
        Assert.Empty(diagnostics);
        Assert.Equal(cases.Length, driver.GetRunResult().GeneratedTrees.Count(tree => tree.ToString().Contains("union EnvelopeField", StringComparison.Ordinal)));

        // The default parse options do not report XML documentation or cref warnings.
        var trees = compilation.SyntaxTrees.Select(tree => CSharpSyntaxTree.ParseText(
            tree.GetText(TestContext.Current.CancellationToken),
            ((CSharpParseOptions)tree.Options).WithDocumentationMode(DocumentationMode.Diagnose),
            tree.FilePath,
            TestContext.Current.CancellationToken));
        var documentedCompilation = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(trees);
        var compilerDiagnostics = documentedCompilation.GetDiagnostics(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(compilerDiagnostics, diagnostic => diagnostic.Severity is DiagnosticSeverity.Error);
        foreach (var tree in documentedCompilation.SyntaxTrees)
        {
            // Check warnings in the documentation, independently of declaration warnings such as CS8669.
            var comments = tree.GetRoot(TestContext.Current.CancellationToken).DescendantTrivia()
                .Where(trivia => trivia.GetStructure() is DocumentationCommentTriviaSyntax);
            foreach (var comment in comments)
                Assert.Empty(compilerDiagnostics.Where(diagnostic => diagnostic.Location.SourceTree == tree
                    && comment.FullSpan.IntersectsWith(diagnostic.Location.SourceSpan)));
        }
    }

    private static BoundAvroFile Bind(string schema, LanguageFeatures features)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var file = AvroFile.Parse(new SourceText("envelope.avsc", schema),
            new AvroParseOptions(GenerationTarget.Modern, features), cancellationToken);
        Assert.Empty(file.Diagnostics);
        var symbols = SymbolTable.FromFiles([file], cancellationToken);
        return BoundAvroFile.Bind(LinkedAvroFile.Link(file, symbols, cancellationToken), cancellationToken);
    }

    private static string Record(IEnumerable<string> fields) =>
        $$"""{"type":"record","name":"Envelope","fields":[{{string.Join(",", fields)}}]}""";

    private static string Nullable(string schema, bool nullFirst) =>
        nullFirst ? $"[\"null\",{schema}]" : $"[{schema},\"null\"]";

    private static string List(string item) =>
        $"<see cref=\"global::System.Collections.Generic.List{{T}}\">List</see>&lt;{item}&gt;";

    private static string Map(string value) =>
        $"<see cref=\"global::System.Collections.Generic.Dictionary{{TKey, TValue}}\">Dictionary</see>&lt;<see cref=\"string\"/>, {value}&gt;";

    private static string Documentation(string[] members) => string.Join("\n",
        ["Represents a union of the following types:", "<list type=\"bullet\">", .. members.Select(member => $"<item>{member}</item>"), "</list>"]);
}
