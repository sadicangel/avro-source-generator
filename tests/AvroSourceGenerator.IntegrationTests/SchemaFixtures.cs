using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;
using Xunit;

namespace AvroSourceGenerator.IntegrationTests;

public static class SchemaFixtures
{
    public static string ReadJson(Type type, GenerationTarget target)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Schemas");
        var schemaPath = Path.Combine(directory, type.Name + ".avsc");
        if (File.Exists(schemaPath)) return File.ReadAllText(schemaPath);

        // IDL roots reuse supplied fixtures and their import closure, without duplicating definitions.
        var sources = Directory.EnumerateFiles(directory)
            .Where(path => Path.GetExtension(path) is ".avsc" or ".avdl" or ".avpr")
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => new SourceText(path, File.ReadAllText(path)));
        var compilation = AvroCompiler.Compile(sources, new AvroParseOptions(target, LanguageFeatures.CSharp15),
            cancellationToken: TestContext.Current.CancellationToken);
        if (!compilation.IsValid)
            throw new InvalidOperationException($"Invalid schema fixtures: {string.Join(Environment.NewLine, compilation.Diagnostics)}");
        var schema = compilation.Schemas[new SchemaName(type.Name, type.Namespace)];
        return schema.ToJsonString(compilation.Schemas);
    }
}
