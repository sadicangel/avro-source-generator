using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.UnitTests.Infrastructure;

public static class DefaultPreservationAssertions
{
    public const string Schema = """
        {"type":"record","name":"RawDefaults","fields":[
          {"name":"Number","type":"long","default":7},
          {"name":"Text","type":"string","default":"hello"},
          {"name":"Bytes","type":"bytes","default":"NDI="},
          {"name":"Items","type":{"type":"array","items":"int"},"default":[1,2]},
          {"name":"Map","type":{"type":"map","values":"string"},"default":{"x":"y"}},
          {"name":"Choice","type":{"type":"enum","name":"RawChoice","symbols":["First","Second"]},"default":"Second"},
          {"name":"Fixed","type":{"type":"fixed","name":"RawFixed","size":2},"default":"AB"},
          {"name":"Record","type":{"type":"record","name":"RawChild","fields":[{"name":"Name","type":"string","default":"fallback"}]},"default":{}},
          {"name":"Optional","type":["null","string"],"default":"present"}
        ]}
        """;

    public static object Verify(LanguageVersion version, string library, ImmutableArray<MetadataReference> references)
    {
        var input = GeneratorInput.Create([ProjectFile.Schema(Schema)], references,
            new ProjectConfig(version) { AvroLibrary = library });
        var driver = input.GeneratorDriver.RunGeneratorsAndUpdateCompilation(input.Compilation, out var compilation, out var diagnostics,
            TestContext.Current.CancellationToken);
        Assert.Empty(diagnostics);
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error));
        var source = Assert.Single(driver.GetRunResult().Results.Single().GeneratedSources, source => source.HintName == "RawDefaults.Avro.g.cs").SourceText.ToString();
        foreach (var name in new[] { "Number", "Text", "Bytes", "Items", "Map", "Choice", "Fixed", "Record", "Optional" })
            Assert.EndsWith("}", Assert.Single(source.Split('\n'), line => line.Contains($" {name} {{", StringComparison.Ordinal)).TrimEnd());
        using var stream = new MemoryStream();
        var emit = compilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        var type = Assembly.Load(stream.ToArray()).GetType("RawDefaults", throwOnError: true)!;
        var instance = Activator.CreateInstance(type)!;
        Assert.Equal(0L, type.GetProperty("Number")!.GetValue(instance));
        Assert.Equal("First", type.GetProperty("Choice")!.GetValue(instance)!.ToString());
        foreach (var name in new[] { "Text", "Bytes", "Items", "Map", "Fixed", "Record", "Optional" })
            Assert.Null(type.GetProperty(name)!.GetValue(instance));
        return instance;
    }
}
