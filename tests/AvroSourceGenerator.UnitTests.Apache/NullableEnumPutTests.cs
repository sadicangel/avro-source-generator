using System.Reflection;
using Avro.Specific;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.UnitTests.Apache;

public sealed class NullableEnumPutTests
{
    [Theory]
    [InlineData(LanguageVersion.CSharp7_3)]
    [InlineData(LanguageVersion.CSharp12)]
    public void Put_accepts_boxed_ordinals_enums_and_null(LanguageVersion version)
    {
        const string Schema = """
            {"type":"record","name":"NullableEnumRecord","fields":[
              {"name":"Choice","type":["null",{"type":"enum","name":"Status","symbols":["First","Second"]}]}
            ]}
            """;
        var input = GeneratorInput.Create(
            [ProjectFile.Schema(Schema)],
            [MetadataReference.CreateFromFile(typeof(Avro.Schema).Assembly.Location)],
            new ProjectConfig(version) { AvroLibrary = "Apache" });
        input.GeneratorDriver.RunGeneratorsAndUpdateCompilation(
            input.Compilation,
            out var compilation,
            out var diagnostics,
            TestContext.Current.CancellationToken);
        Assert.Empty(diagnostics);
        Assert.Empty(
            compilation.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning));

        using var stream = new MemoryStream();
        var emit = compilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        var assembly = Assembly.Load(stream.ToArray());
        var record = Assert.IsAssignableFrom<ISpecificRecord>(Activator.CreateInstance(assembly.GetType("NullableEnumRecord", throwOnError: true)!));
        var expected = Enum.ToObject(assembly.GetType("Status", throwOnError: true)!, 1);

        // Apache.Avro can supply an enum's ordinal as a boxed int for union members.
        record.Put(0, 1);
        Assert.Equal(expected, record.Get(0));
        record.Put(0, expected);
        Assert.Equal(expected, record.Get(0));
        record.Put(0, null!);
        Assert.Null(record.Get(0));
    }
}
