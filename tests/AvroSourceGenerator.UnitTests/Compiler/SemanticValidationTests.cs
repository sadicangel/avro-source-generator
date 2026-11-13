using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Compiler.Configuration;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Schemas.Defaults;
using AvroSourceGenerator.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.UnitTests.Compiler;

public sealed class SemanticValidationTests
{
    public static TheoryData<string, int, string> IssueExamples => new TheoryData<string, int, string>
    {
        { """{"type":"record","name":"R","fields":[{"name":"Value","type":"int"},{"name":"Value","type":"int"}]}""", 2032, "\"Value\"" },
        { """{"type":"enum","name":"E","symbols":["A","A"]}""", 2033, "\"A\"" },
        { """{"type":"record","name":"R","fields":[{"name":"Value","type":["int","int"]}]}""", 2034, "\"int\"" },
        { """{"type":"record","name":"R","fields":[{"name":"Value","type":["null",["int","string"]]}]}""", 2035, "[\"int\",\"string\"]" },
    };

    public static TheoryData<string, string, AvroDiagnosticCode, string> InvalidInputs => new TheoryData<string, string, AvroDiagnosticCode, string>
    {
        { "avsc", """{"type":"error","name":"R","fields":[{"name":"class","type":"int"},{"name":"class","type":"long"}]}""", AvroDiagnosticCode.DuplicateField, "\"class\"" },
        { "avsc", """{"type":"record","name":"R","fields":[{"name":"f","type":[{"type":"array","items":"int"},{"type":"array","items":"string"}]}]}""", AvroDiagnosticCode.DuplicateUnionBranch, "{\"type\":\"array\",\"items\":\"string\"}" },
        { "avsc", """{"type":"record","name":"R","fields":[{"name":"f","type":[{"type":"map","values":"int"},{"type":"map","values":"string"}]}]}""", AvroDiagnosticCode.DuplicateUnionBranch, "{\"type\":\"map\",\"values\":\"string\"}" },
        { "avsc", """{"type":"record","name":"R","fields":[{"name":"f","type":["int",{"type":"int","logicalType":"date"}]}]}""", AvroDiagnosticCode.DuplicateUnionBranch, "{\"type\":\"int\",\"logicalType\":\"date\"}" },
        { "avsc", """{"type":"record","name":"R","namespace":"n","fields":[{"name":"f","type":["R","n.R"]}]}""", AvroDiagnosticCode.DuplicateUnionBranch, "\"n.R\"" },
        { "avsc", """{"type":"record","name":"R","fields":[{"name":"f","type":[{"type":"enum","name":"E","symbols":["A"]},"E"]}]}""", AvroDiagnosticCode.DuplicateUnionBranch, "\"E\"" },
        { "avpr", """{"protocol":"P","types":[{"type":"record","name":"R","fields":[{"name":"f","type":"int"},{"name":"f","type":"int"}]}],"messages":{}}""", AvroDiagnosticCode.DuplicateField, "\"f\"" },
        { "avpr", """{"protocol":"P","types":[{"type":"enum","name":"E","symbols":["A","A"]}],"messages":{}}""", AvroDiagnosticCode.DuplicateEnumSymbol, "\"A\"" },
        { "avpr", """{"protocol":"P","types":[],"messages":{"m":{"request":[{"name":"p","type":["int","int"]}],"response":"null"}}}""", AvroDiagnosticCode.DuplicateUnionBranch, "\"int\"" },
        { "avpr", """{"protocol":"P","types":[],"messages":{"m":{"request":[],"response":["null",["int","string"]]}}}""", AvroDiagnosticCode.NestedUnion, "[\"int\",\"string\"]" },
        { "avpr", """{"protocol":"P","types":[{"type":"error","name":"E","fields":[]}],"messages":{"m":{"request":[],"response":"null","errors":["E","E"]}}}""", AvroDiagnosticCode.DuplicateUnionBranch, "\"E\"" },
        { "avpr", """{"protocol":"P","types":[{"type":"error","name":"E","fields":[]}],"messages":{"m":{"request":[],"response":"null","errors":[["E"]]}}}""", AvroDiagnosticCode.NestedUnion, "[\"E\"]" },
        { "avpr", """{"protocol":"P","types":[],"messages":{"m":{"request":[],"response":"null","errors":["string"]}}}""", AvroDiagnosticCode.DuplicateUnionBranch, "\"string\"" },
        { "avdl", "schema R; record R { int Value; long Value; }", AvroDiagnosticCode.DuplicateField, "Value" },
        { "avdl", "protocol P { error R { int Value; long Value; } }", AvroDiagnosticCode.DuplicateField, "Value" },
        { "avdl", "schema E; enum E { A, A }", AvroDiagnosticCode.DuplicateEnumSymbol, "A" },
        { "avdl", "schema E; enum E { `int`, `int` }", AvroDiagnosticCode.DuplicateEnumSymbol, "`int`" },
        { "avdl", "schema R; record R { union { int, int } Value; }", AvroDiagnosticCode.DuplicateUnionBranch, "int" },
        { "avdl", "schema R; record R { union { int, date } Value; }", AvroDiagnosticCode.DuplicateUnionBranch, "date" },
        { "avdl", "schema R; record R { union { array<int>, array<string> } Value; }", AvroDiagnosticCode.DuplicateUnionBranch, "array<string>" },
        { "avdl", "schema R; record R { union { map<int>, map<string> } Value; }", AvroDiagnosticCode.DuplicateUnionBranch, "map<string>" },
        { "avdl", "namespace n; schema R; record R { union { R, n.R } Value; }", AvroDiagnosticCode.DuplicateUnionBranch, "n.R" },
        { "avdl", "protocol P { void m(union { int, int } p); }", AvroDiagnosticCode.DuplicateUnionBranch, "int" },
        { "avdl", "protocol P { error E {} void m() throws E, E; }", AvroDiagnosticCode.DuplicateUnionBranch, "E" },
        { "avdl", "schema R; record R { union { null, union { int, string } } Value; }", AvroDiagnosticCode.NestedUnion, "union { int, string }" },
        { "avdl", "schema R; record R { union { int, string }? Value; }", AvroDiagnosticCode.NestedUnion, "union { int, string }" },
        { "avdl", "schema R; record R { null? Value; }", AvroDiagnosticCode.DuplicateUnionBranch, "null" },
    };

    public static TheoryData<string, string> ValidInputs => new TheoryData<string, string>
    {
        { "avsc", """{"type":"enum","name":"E","symbols":["class","A","a"],"default":"class"}""" },
        { "avsc", """{"type":"enum","name":"E","symbols":["A"]}""" },
        { "avdl", "schema E; enum E { class, A, a } = class;" },
        { "avdl", "schema E; enum E { A }" },
        { "avsc", """{"type":"record","name":"R","fields":[{"name":"f","type":[{"type":"record","name":"A","fields":[{"name":"value","type":"int"}]},{"type":"record","name":"B","fields":[{"name":"value","type":"int"}]}]},{"name":"F","type":[{"type":"enum","name":"E","symbols":["A"]},{"type":"enum","name":"Other","symbols":["A"]}]}]}""" },
        { "avsc", """{"type":"record","name":"R","fields":[{"name":"f","type":["bytes",{"type":"fixed","name":"A","size":4},{"type":"fixed","name":"B","size":4}]}]}""" },
        { "avsc", """{"type":"record","name":"R","fields":[{"name":"f","type":[{"type":"fixed","name":"A","size":4,"logicalType":"decimal","precision":4,"scale":2},{"type":"fixed","name":"B","size":4,"logicalType":"decimal","precision":4,"scale":2}]}]}""" },
        { "avpr", """{"protocol":"P","types":[{"type":"record","name":"A","namespace":"one","fields":[]},{"type":"record","name":"A","namespace":"two","fields":[]},{"type":"record","name":"R","fields":[{"name":"f","type":["one.A","two.A"]}]}],"messages":{}}""" },
        { "avdl", "schema R; fixed A(4); fixed B(4); record R { union { bytes, A, B } f; }" },
        { "avdl", "protocol P { record A { int value; } record B { int value; } enum E { A } = A; enum Other { A } record R { union { A, B } f; union { E, Other } F; } }" },
        { "avpr", """{"protocol":"P","types":[{"type":"error","name":"A","fields":[]},{"type":"error","name":"B","fields":[]}],"messages":{"m":{"request":[],"response":"null","errors":["A","B"]}}}""" },
        { "avdl", "protocol P { error A {} error B {} void m() throws A, B; }" },
        { "avsc", """{"type":"record","name":"R","fields":[{"name":"value","type":{"type":"array","items":["int","string"]}},{"name":"Value","type":{"type":"map","values":["int","string"]}}]}""" },
    };

    public static TheoryData<string, string, string?> PreservedEnumDefaults => new TheoryData<string, string, string?>
    {
        { "avsc", """{"type":"enum","name":"E","symbols":["A"],"default":"B"}""", "B" },
        { "avsc", """{"type":"enum","name":"E","symbols":["A"],"default":null}""", null },
        { "avdl", "schema E; enum E { A } = B;", "B" },
        { "avdl", "schema E; enum E { A } = null;", null },
    };

    [Theory]
    [MemberData(nameof(IssueExamples))]
    public void Issue_examples_report_the_offending_occurrence(string text, int code, string value)
    {
        var source = new SourceText("invalid.avsc", text);
        var compilation = AvroCompiler.Compile([source], new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.CSharp12), cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(compilation.IsValid);
        var diagnostic = Assert.Single(compilation.Diagnostics, diagnostic => diagnostic.Code == (AvroDiagnosticCode)code);
        Assert.Equal((AvroDiagnosticCode)code, diagnostic.Code);
        Assert.Equal(source.GetSourceSpan(text.LastIndexOf(value, StringComparison.Ordinal), value.Length), diagnostic.SourceSpan);
        Assert.Same(source, diagnostic.SourceSpan.SourceText);
        Assert.NotEmpty(diagnostic.GetMessage());
        Assert.False(compilation.IsFileValid(Assert.Single(compilation.Files), TestContext.Current.CancellationToken));
    }

    [Theory]
    [MemberData(nameof(InvalidInputs))]
    public void Core_and_generator_agree_on_semantics_and_source_occurrences(string extension, string text, AvroDiagnosticCode code, string value)
    {
        var path = "invalid." + extension;
        var token = TestContext.Current.CancellationToken;
        var source = new SourceText(path, text);
        var compiled = AvroCompiler.Compile([source], new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.CSharp12), cancellationToken: token);
        var diagnostic = Assert.Single(compiled.Diagnostics, diagnostic => diagnostic.Code == code);
        Assert.Equal(code, diagnostic.Code);
        Assert.Equal(source.GetSourceSpan(text.LastIndexOf(value, StringComparison.Ordinal), value.Length), diagnostic.SourceSpan);
        Assert.False(compiled.IsFileValid(Assert.Single(compiled.Files), token));

        var input = GeneratorInput.Create([new ProjectFile(text, extension, path)], [], new ProjectConfig(LanguageVersion.CSharp12) { AvroLibrary = "None" });
        var result = input.GeneratorDriver.RunGenerators(input.Compilation, token).GetRunResult();
        Assert.Equal(
            compiled.Diagnostics.Select(diagnostic => (diagnostic.ToDiagnostic().Id, diagnostic.GetMessage(), diagnostic.SourceSpan.Offset, diagnostic.SourceSpan.Length)),
            result.Diagnostics.Select(diagnostic => (diagnostic.Id, diagnostic.GetMessage(), diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length)));
        var generatedDiagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Id == $"AVROSG{(int)code:D4}");
        Assert.Equal($"AVROSG{(int)code:D4}", generatedDiagnostic.Id);
        Assert.Equal(diagnostic.GetMessage(), generatedDiagnostic.GetMessage());
        Assert.Equal(diagnostic.SourceSpan.Offset, generatedDiagnostic.Location.SourceSpan.Start);
        Assert.Equal(value.Length, generatedDiagnostic.Location.SourceSpan.Length);
        Assert.Equal(path, generatedDiagnostic.Location.GetLineSpan().Path);
        Assert.Empty(result.GeneratedTrees);
        Assert.All(result.Results, result => Assert.Null(result.Exception));
    }

    [Theory]
    [MemberData(nameof(ValidInputs))]
    public void Legal_Avro_names_and_branches_generate_compilable_CSharp(string extension, string text)
    {
        var input = GeneratorInput.Create([new ProjectFile(text, extension, "valid." + extension)], [], new ProjectConfig(LanguageVersion.CSharp12) { AvroLibrary = "None" });
        var driver = input.GeneratorDriver.RunGeneratorsAndUpdateCompilation(input.Compilation, out var compilation, out var diagnostics, TestContext.Current.CancellationToken);
        Assert.Empty(diagnostics);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        Assert.NotEmpty(driver.GetRunResult().GeneratedTrees);
        using var stream = new MemoryStream();
        var emitted = compilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
    }

    [Theory]
    [InlineData("avsc", """{"type":"record","name":"R","fields":[{"name":"f","type":"int","default":"raw"}]}""")]
    [InlineData("avdl", "schema R; record R { int f = \"raw\"; }")]
    public void Field_defaults_remain_unbound(string extension, string text)
    {
        var compiled = AvroCompiler.Compile([new SourceText("valid." + extension, text)], new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.CSharp12), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(compiled.IsValid);
        var record = Assert.IsType<RecordSchema>(compiled.Schemas[new SchemaName("R")]);
        Assert.Equal("raw", Assert.IsType<UnboundDefaultValue>(Assert.Single(record.Fields).Default).Json.GetString());
    }

    [Theory]
    [MemberData(nameof(PreservedEnumDefaults))]
    public void Enum_schema_defaults_keep_existing_behavior_pending_issue_191(string extension, string text, string? expected)
    {
        var compiled = AvroCompiler.Compile([new SourceText("preserved." + extension, text)], new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.CSharp12), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(compiled.IsValid);
        Assert.Empty(compiled.Diagnostics);
        Assert.Equal(expected, Assert.IsType<EnumSchema>(compiled.Schemas[new SchemaName("E")]).Default);
        var input = GeneratorInput.Create([new ProjectFile(text, extension, "preserved." + extension)], [], new ProjectConfig(LanguageVersion.CSharp12) { AvroLibrary = "None" });
        var generated = input.GeneratorDriver.RunGenerators(input.Compilation, TestContext.Current.CancellationToken).GetRunResult();
        Assert.Empty(generated.Diagnostics);
        Assert.Single(generated.GeneratedTrees);
    }

    [Theory]
    [InlineData("avpr", """{"protocol":"P","types":[],"messages":{"m":{"request":[{"name":"p","type":"int","default":"raw"}],"response":"null"}}}""")]
    [InlineData("avdl", "protocol P { void m(int p = \"raw\"); }")]
    public void Protocol_parameter_defaults_remain_unbound(string extension, string text)
    {
        var compiled = AvroCompiler.Compile([new SourceText("valid." + extension, text)], new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.CSharp12), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(compiled.IsValid);
        var protocol = Assert.IsType<ProtocolSchema>(compiled.Schemas[new SchemaName("P")]);
        Assert.Equal("raw", Assert.IsType<UnboundDefaultValue>(Assert.Single(Assert.Single(protocol.Messages).RequestParameters).Default).Json.GetString());
    }

    [Theory]
    [InlineData(ReferenceResolution.Strict)]
    [InlineData(ReferenceResolution.Deferred)]
    public void Duplicate_named_branches_are_rejected_before_reference_resolution(ReferenceResolution resolution)
    {
        const string Text = """{"type":"record","name":"R","fields":[{"name":"f","type":["Missing","Missing"]}]}""";
        var compilation = AvroCompiler.Compile([new SourceText("invalid.avsc", Text)], new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.CSharp12), new AvroCompilationOptions(resolution), TestContext.Current.CancellationToken);
        var diagnostic = Assert.Single(compilation.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.DuplicateUnionBranch, diagnostic.Code);
        Assert.Equal(Text.LastIndexOf("\"Missing\"", StringComparison.Ordinal), diagnostic.SourceSpan.Offset);
        Assert.False(Assert.Single(compilation.Files).File.IsValid);
    }

    [Theory]
    [InlineData(ReferenceResolution.Strict)]
    [InlineData(ReferenceResolution.Deferred)]
    public void Distinct_unresolved_branches_keep_the_missing_reference_diagnostic(ReferenceResolution resolution)
    {
        const string Text = """{"type":"record","name":"R","fields":[{"name":"f","type":["Missing","Other"]}]}""";
        var compilation = AvroCompiler.Compile([new SourceText("invalid.avsc", Text)], new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.CSharp12), new AvroCompilationOptions(resolution), TestContext.Current.CancellationToken);
        var diagnostic = Assert.Single(compilation.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.MissingReferences, diagnostic.Code);
        Assert.Equal(Text.IndexOf("\"Missing\"", StringComparison.Ordinal), diagnostic.SourceSpan.Offset);
    }

    [Fact]
    public void Duplicate_forward_references_are_rejected_in_both_resolution_modes()
    {
        const string Text = "protocol P { record R { union { E, E } f; } enum E { A } }";
        var strict = SchemaCompilerTestHelpers.Bind(ReferenceResolution.Strict, DuplicateResolution.Error, ("invalid.avdl", Text));
        Assert.Equal(AvroDiagnosticCode.DuplicateUnionBranch, Assert.Single(strict.Diagnostics).Code);
        var deferred = SchemaCompilerTestHelpers.Bind(ReferenceResolution.Deferred, DuplicateResolution.Error, ("invalid.avdl", Text));
        Assert.Equal(AvroDiagnosticCode.DuplicateUnionBranch, Assert.Single(deferred.Diagnostics).Code);
    }

    [Theory]
    [MemberData(nameof(IssueExamples))]
    public void Invalid_declarations_suppress_dependents_and_leave_independent_files(string text, int code, string value)
    {
        Assert.NotEmpty(value);
        var name = text.StartsWith("{\"type\":\"enum\"", StringComparison.Ordinal) ? "E" : "R";
        var dependent = $"protocol Dependent {{ import schema \"invalid.avsc\"; record Wrapper {{ {name} f; }} }}";
        var input = GeneratorInput.Create(
            [ProjectFile.Schema(text, "invalid.avsc"), ProjectFile.Source(dependent, "dependent.avdl"), ProjectFile.Schema("""{"type":"record","name":"Independent","fields":[]}""", "independent.avsc")],
            [], new ProjectConfig(LanguageVersion.CSharp12) { AvroLibrary = "None" });
        var result = input.GeneratorDriver.RunGenerators(input.Compilation, TestContext.Current.CancellationToken).GetRunResult();
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == $"AVROSG{code:D4}");
        Assert.Equal("Independent.Avro.g.cs", Assert.Single(Assert.Single(result.Results).GeneratedSources).HintName);
        Assert.All(result.Results, result => Assert.Null(result.Exception));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Imported_named_duplicates_suppress_transitive_dependents(bool idl)
    {
        var invalid = idl
            ? "schema R; record R { union { R, R } f; }"
            : """{"type":"record","name":"R","fields":[{"name":"f","type":["R","R"]}]}""";
        var extension = idl ? "avdl" : "avsc";
        var kind = idl ? "idl" : "schema";
        var compiled = SchemaCompilerTestHelpers.Compile(GenerationTarget.Modern, ReferenceResolution.Strict, DuplicateResolution.Error,
            ("invalid." + extension, invalid),
            ("middle.avdl", $"import {kind} \"invalid.{extension}\"; schema Middle; record Middle {{ R f; }}"),
            ("last.avdl", "import idl \"middle.avdl\"; schema Last; record Last { Middle f; }"),
            ("independent.avdl", "schema Independent; record Independent {}"));
        var diagnostic = Assert.Single(compiled.Compilation.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.DuplicateUnionBranch, diagnostic.Code);
        Assert.Equal("invalid." + extension, diagnostic.SourceSpan.SourceText.Path.OriginalPath);
        Assert.All(compiled.RenderableFiles.Take(3), file => Assert.Empty(file.EmittedSchemas));
        Assert.Single(compiled.RenderableFiles[3].EmittedSchemas);
    }

    [Theory]
    [InlineData("avsc", """{"type":"enum","name":"E","symbols":["A","A","B","B"]}""", "\"A\"", "\"B\"")]
    [InlineData("avdl", "schema E; enum E { A, A, B, B }", "A", "B")]
    [InlineData("avsc", """{"type":"record","name":"R","fields":[{"name":"f","type":["int","int","null","null"]}]}""", "\"int\"", "\"null\"")]
    [InlineData("avdl", "schema R; record R { union { int, int, null, null } f; }", "int", "null")]
    [InlineData("avsc", """{"type":"record","name":"R","fields":[{"name":"f","type":"int"},{"name":"f","type":"int"},{"name":"g","type":"int"},{"name":"g","type":"int"}]}""", "\"f\"", "\"g\"")]
    [InlineData("avdl", "schema R; record R { int f; int f; int g; int g; }", "f", "g")]
    [InlineData("avsc", """{"type":"record","name":"R","fields":[{"name":"f","type":["R","R",{"type":"enum","name":"E","symbols":["A"]},"E"]}]}""", "\"R\"", "\"E\"")]
    [InlineData("avdl", "protocol P { enum E { A } record R { union { R, R, E, E } f; } }", "R", "E")]
    public void Repeated_occurrences_are_all_reported_in_stable_order(string extension, string text, string first, string second)
    {
        var source = new SourceText("invalid." + extension, text);
        var options = new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.CSharp12);
        var token = TestContext.Current.CancellationToken;
        var compilation = AvroCompiler.Compile([source], options, cancellationToken: token);
        var expected = new[] { first, second };
        var duplicates = compilation.Diagnostics.Where(diagnostic => diagnostic.Code is AvroDiagnosticCode.DuplicateField or AvroDiagnosticCode.DuplicateEnumSymbol or AvroDiagnosticCode.DuplicateUnionBranch);
        Assert.Equal(expected, duplicates.Select(diagnostic => diagnostic.SourceSpan.ToString()));
        Assert.Equal(expected.Select(value => text.LastIndexOf(value, StringComparison.Ordinal)), duplicates.Select(diagnostic => diagnostic.SourceSpan.Offset));
        Assert.All(compilation.Diagnostics.Except(duplicates), diagnostic => Assert.True(diagnostic.Code is AvroDiagnosticCode.InvalidSymbols));
        var repeated = AvroCompiler.Compile([source], options, cancellationToken: token);
        Assert.Equal(compilation.Diagnostics, repeated.Diagnostics);
        Assert.Equal(compilation, repeated);
        Assert.Equal(compilation.GetHashCode(), repeated.GetHashCode());
    }

    [Fact]
    public void Legal_mapped_types_preserve_all_serialized_branches_in_declaration_order()
    {
        const string Text = """{"type":"record","name":"R","fields":[{"name":"f","type":[{"type":"fixed","name":"B","size":4},"bytes",{"type":"fixed","name":"A","size":4}]}]}""";
        var compiled = AvroCompiler.Compile([new SourceText("valid.avsc", Text)], new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.CSharp12), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(compiled.IsValid);
        var record = Assert.IsType<RecordSchema>(compiled.Schemas[new SchemaName("R")]);
        var union = Assert.IsType<UnionSchema>(Assert.Single(record.Fields).Type);
        Assert.Equal(["B", "bytes", "A"], union.Schemas.Select(schema => schema.SchemaName.FullName));
        using var document = System.Text.Json.JsonDocument.Parse(record.ToJsonString(compiled.Schemas));
        var branches = document.RootElement.GetProperty("fields")[0].GetProperty("type");
        Assert.Equal("B", branches[0].GetProperty("name").GetString());
        Assert.Equal("bytes", branches[1].GetString());
        Assert.Equal("A", branches[2].GetProperty("name").GetString());
    }

    [Theory]
    [MemberData(nameof(IssueExamples))]
    public void Semantic_validation_preserves_cancellation(string text, int code, string value)
    {
        Assert.True(code > 0);
        Assert.NotEmpty(value);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => AvroCompiler.Compile([new SourceText("invalid.avsc", text)], new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.CSharp12), cancellationToken: cancellation.Token));
    }
}
