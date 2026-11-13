using AvroSourceGenerator.Avdl;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Compiler.Configuration;
using AvroSourceGenerator.Compiler.Parsing;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.UnitTests;

public sealed class AvdlDirectiveTests
{
    private static readonly AvroParseOptions s_options = new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.NullableReferenceTypes);

    public static TheoryData<string, string[]> DuplicateDirectives => new TheoryData<string, string[]>
    {
        { "schema R; schema R; record R {}", ["schema R;"] },
        { "schema R; schema S; record R {}", ["schema S;"] },
        { "schema R; schema R; schema S; schema R; record R {}", ["schema R;", "schema S;", "schema R;"] },
        { "namespace a; namespace a; schema R; record R {}", ["namespace a;"] },
        { "namespace a; namespace b; schema R; record R {}", ["namespace b;"] },
        { "namespace a; namespace a; namespace b; namespace a; schema R; record R {}", ["namespace a;", "namespace b;", "namespace a;"] },
        { "namespace a; namespace a; protocol P {}", ["namespace a;"] },
        { "namespace a; namespace b; protocol P {}", ["namespace b;"] },
        { "namespace a; schema R; namespace a; schema R; namespace b; schema S; record R {}", ["namespace a;", "schema R;", "namespace b;", "schema S;"] },
        { "namespace a;\r\n// repeated below\r\nnamespace b;\r\nschema R; record R {}", ["namespace b;"] },
    };

    [Theory]
    [MemberData(nameof(DuplicateDirectives))]
    public void Repeated_directives_are_reported_before_root_span_calculation_and_lowering(string text, string[] repeated)
    {
        var source = new SourceText("schemas/invalid.avdl", text);
        var token = TestContext.Current.CancellationToken;
        var tree = Parser.Parse(source, token);
        var file = AvroFile.Parse(source, s_options, token);
        var compilation = AvroCompiler.Compile([source], s_options, cancellationToken: token);

        Assert.False(file.IsValid);
        Assert.Empty(file.Declarations);
        Assert.False(compilation.IsValid);
        Assert.Equal(tree.Diagnostics, file.GetDiagnostics());
        Assert.Equal(tree.Diagnostics, compilation.Diagnostics);
        Assert.Equal(tree.Diagnostics, Parser.Parse(source, token).Diagnostics);
        Assert.Equal(repeated.Length, tree.Diagnostics.Length);
        var offset = text.IndexOf(';') + 1;
        for (var index = 0; index < repeated.Length; index++)
        {
            var directive = repeated[index];
            offset = text.IndexOf(directive, offset, StringComparison.Ordinal);
            var diagnostic = tree.Diagnostics[index];
            Assert.Equal(AvroDiagnosticCode.DuplicateIdlDirective, diagnostic.Code);
            Assert.Equal(AvroDiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Same(source, diagnostic.SourceSpan.SourceText);
            Assert.Equal(new SourceSpan(source, offset, directive.Length), diagnostic.SourceSpan);
            Assert.Equal($"IDL document permits only one '{directive.Split(' ')[0]}' directive; remove the repeated directive", diagnostic.GetMessage());
            offset += directive.Length;
        }

        var input = GeneratorInput.Create(
            [ProjectFile.Source(text, source.Path.OriginalPath)], [],
            new ProjectConfig { AvroLibrary = "None", LanguageVersion = LanguageVersion.CSharp10 });
        var result = input.GeneratorDriver.RunGenerators(input.Compilation, token).GetRunResult();
        Assert.All(result.Results, generator => Assert.Null(generator.Exception));
        Assert.Empty(result.GeneratedTrees);
        Assert.Equal(tree.Diagnostics.Select(diagnostic => diagnostic.ToDiagnostic()), result.Diagnostics);
    }

    [Theory]
    [InlineData("schema R; record R {}", "R")]
    [InlineData("namespace a; schema R; record R {}", "R")]
    [InlineData("schema R; namespace a; record R {}", "R")]
    [InlineData("protocol P {}", "protocol P {}")]
    [InlineData("namespace a; protocol P {}", "namespace a; protocol P {}")]
    [InlineData("@namespace(\"a\") protocol P {}", "protocol P {}")]
    [InlineData("schema string;", "string")]
    public void Valid_document_forms_keep_their_root_spans(string text, string root)
    {
        var source = new SourceText("valid.avdl", text);
        var token = TestContext.Current.CancellationToken;
        var tree = Parser.Parse(source, token);
        var (file, rootSpan) = AvdlParser.ParseFile(source, s_options, token);

        Assert.Empty(tree.Diagnostics);
        Assert.True(file.IsValid);
        Assert.Empty(file.GetDiagnostics());
        Assert.Equal(new SourceSpan(source, text.IndexOf(root, StringComparison.Ordinal), root.Length), rootSpan);
        Assert.Same(source, rootSpan.SourceText);
    }

    [Theory]
    [InlineData("namespace a;", AvroDiagnosticCode.InvalidIdlDocument, "namespace a;")]
    [InlineData("record R {}", AvroDiagnosticCode.InvalidIdlDocument, "record R {}")]
    [InlineData("protocol P {} protocol Q {}", AvroDiagnosticCode.InvalidIdlDocument, "protocol P {} protocol Q {}")]
    [InlineData("protocol P {} record R {}", AvroDiagnosticCode.InvalidIdlDocument, "protocol P {} record R {}")]
    [InlineData("schema R; protocol P {}", AvroDiagnosticCode.InvalidIdlDeclaration, "protocol P {}")]
    public void Invalid_document_forms_keep_their_existing_diagnostics(string text, AvroDiagnosticCode code, string span)
    {
        var source = new SourceText("invalid.avdl", text);
        var file = AvroFile.Parse(source, s_options, TestContext.Current.CancellationToken);

        Assert.False(file.IsValid);
        var diagnostic = Assert.Single(file.GetDiagnostics());
        Assert.Equal(code, diagnostic.Code);
        Assert.Same(source, diagnostic.SourceSpan.SourceText);
        Assert.Equal(span, diagnostic.SourceSpan.ToString());
    }

    [Theory]
    [MemberData(nameof(DuplicateDirectives))]
    public void Repeated_directives_do_not_hide_cancellation(string text, string[] repeated)
    {
        Assert.NotEmpty(repeated);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var source = new SourceText("invalid.avdl", text);

        Assert.Throws<OperationCanceledException>(() => Parser.Parse(source, cancellation.Token));
        Assert.Throws<OperationCanceledException>(() => AvroFile.Parse(source, s_options, cancellation.Token));
    }
}
