using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.UnitTests.Compiler;

public sealed class DiagnosticCompositionTests
{
    private static readonly AvroParseOptions s_options = new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.CSharp12);

    [Fact]
    public void Parse_errors_are_forwarded_through_linking_and_binding_once_in_order()
    {
        var token = TestContext.Current.CancellationToken;
        var file = AvroFile.Parse(new SourceText("source.avdl",
            "schema F; @namespace(1) @aliases(2) fixed F(0); fixed G(-1);"), s_options, token);
        var expected = file.GetDiagnostics().ToArray();
        Assert.True(expected.Length > 1);

        var linked = LinkedAvroFile.Link(file, SymbolTable.FromFiles([file], token), token);
        var bound = BoundAvroFile.Bind(linked, token);
        Assert.False(file.IsValid);
        Assert.False(linked.IsValid);
        Assert.False(bound.IsValid);
        Assert.Equal(expected, linked.GetDiagnostics());
        Assert.Equal(expected, bound.GetDiagnostics());
        Assert.Equal(expected, bound.GetDiagnostics());

        var compilation = AvroCompilation.Create([bound], new AvroCompilationOptions(ReferenceResolution.Deferred), token);
        Assert.Equal(expected, compilation.Diagnostics);
    }

    [Fact]
    public void Upstream_warnings_are_forwarded_once_and_enumeration_is_repeatable()
    {
        var token = TestContext.Current.CancellationToken;
        var parsed = AvroFile.Parse(new SourceText("consumer.avsc",
            """{"type":"record","name":"Consumer","fields":[{"name":"Value","type":"Pair","default":"A"}]}"""), s_options, token);
        var warning = new AvroDiagnostic(AvroDiagnosticCode.UnusedImport, SourceSpan.FromSourceFile(parsed), "unused.avsc");
        var file = new AvroFile(parsed.Text, parsed.RootSchema, parsed.Declarations, parsed.DeclarationSpans,
            parsed.References, parsed.ReferenceSpans, parsed.Dependencies, parsed.Imports, [warning], parsed.ParseOptions);
        var definition = AvroFile.Parse(new SourceText("definition.avsc",
            """{"type":"fixed","name":"Pair","size":2}"""), s_options, token);
        var symbols = SymbolTable.FromFiles([file, definition], token);
        var linked = LinkedAvroFile.Link(file, symbols, token);
        var bound = BoundAvroFile.Bind(linked, token);
        var hashCode = bound.GetHashCode();
        var diagnostics = bound.GetDiagnostics();

        Assert.True(file.IsValid);
        Assert.True(linked.IsValid);
        Assert.True(bound.IsValid);
        Assert.Equal(warning, diagnostics.First());
        var expected = diagnostics.ToArray();
        Assert.Equal([warning], expected);
        Assert.Equal(expected, diagnostics);
        Assert.Equal(expected, bound.GetDiagnostics());
        Assert.Equal([warning], file.GetDiagnostics());
        Assert.Equal([warning], linked.GetDiagnostics());
        Assert.Equal(hashCode, bound.GetHashCode());

        var compilation = AvroCompilation.Create([bound, BoundAvroFile.Bind(LinkedAvroFile.Link(definition, symbols, token), token)],
            new AvroCompilationOptions(ReferenceResolution.Deferred), token);
        Assert.Equal(expected, compilation.Diagnostics);
    }
}
