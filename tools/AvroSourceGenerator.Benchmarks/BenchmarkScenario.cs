using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.Benchmarks;

internal sealed record BenchmarkScenario(
    int SchemaCount,
    ImmutableArray<AdditionalText> Files,
    AdditionalText OriginalFile,
    AdditionalText ChangedFile,
    IncrementalChangeScenario ChangeScenario)
{
    public const int DefaultSchemaCount = 250;
    private const int FieldGroupCount = 4;

    public static BenchmarkScenario Create(int schemaCount, IncrementalChangeScenario changeScenario = IncrementalChangeScenario.IndependentContent)
    {
        if (schemaCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaCount));
        }

        var files = Enumerable.Range(0, schemaCount)
            .Select(index => (AdditionalText)new InMemoryAdditionalText(GetPath(index), CreateSchema(index, changeScenario)))
            .ToImmutableArray();
        var changedFile = new InMemoryAdditionalText(GetPath(0), CreateChangedSchema(changeScenario));
        if (files[0].GetText()!.ContentEquals(changedFile.GetText()))
        {
            throw new InvalidOperationException($"The {changeScenario} scenario did not change its selected file.");
        }

        return new BenchmarkScenario(schemaCount, files, files[0], changedFile, changeScenario);
    }

    private static string GetPath(int index) => $"Schemas/BenchmarkModel{index:D4}.avsc";

    private static string CreateSchema(
        int index,
        IncrementalChangeScenario changeScenario,
        bool includeChangedField = false)
    {
        if (changeScenario == IncrementalChangeScenario.ReferencedSchemaContent && index % 2 != 0)
        {
            return CreateDependentSchema(index);
        }

        var fields = new List<string>(FieldGroupCount * 9 + 1);

        for (var group = 0; group < FieldGroupCount; group++)
        {
            fields.Add($"{{\"name\": \"Sequence{group}\", \"type\": \"int\"}}");
            fields.Add($"{{\"name\": \"Offset{group}\", \"type\": \"long\"}}");
            fields.Add($"{{\"name\": \"Name{group}\", \"type\": \"string\"}}");
            fields.Add($"{{\"name\": \"Enabled{group}\", \"type\": \"boolean\", \"default\": true}}");
            fields.Add($"{{\"name\": \"OptionalText{group}\", \"type\": [\"null\", \"string\"], \"default\": null}}");
            fields.Add($"{{\"name\": \"Tags{group}\", \"type\": {{\"type\": \"array\", \"items\": \"string\"}}}}");
            fields.Add($"{{\"name\": \"Attributes{group}\", \"type\": {{\"type\": \"map\", \"values\": \"string\"}}}}");
            fields.Add($"{{\"name\": \"CreatedAt{group}\", \"type\": {{\"type\": \"long\", \"logicalType\": \"timestamp-millis\"}}}}");
            fields.Add($"{{\"name\": \"Amount{group}\", \"type\": {{\"type\": \"bytes\", \"logicalType\": \"decimal\", \"precision\": 18, \"scale\": 4}}}}");
        }

        if (includeChangedField)
        {
            fields.Add("""{"name": "RevisionMarker", "type": "string"}""");
        }

        return $$"""
            {
              "type": "record",
              "name": "BenchmarkModel{{index:D4}}",
              "namespace": "AvroSourceGenerator.BenchmarkModels",
              "doc": "A deterministic, moderately complex model used by the source-generator benchmarks.",
              "fields": [
                {{string.Join(",\n    ", fields)}}
              ]
            }
            """;
    }

    private static string CreateChangedSchema(IncrementalChangeScenario changeScenario) => changeScenario switch
    {
        IncrementalChangeScenario.IndependentContent or IncrementalChangeScenario.ReferencedSchemaContent =>
            CreateRootSchema(includeChangedField: true, renamed: false),
        IncrementalChangeScenario.SchemaIdentity => CreateRootSchema(includeChangedField: false, renamed: true),
        _ => throw new ArgumentOutOfRangeException(nameof(changeScenario)),
    };

    private static string CreateRootSchema(bool includeChangedField, bool renamed)
    {
        var schema = CreateSchema(0, IncrementalChangeScenario.IndependentContent, includeChangedField);

        return renamed
            ? schema.Replace("BenchmarkModel0000", "RenamedBenchmarkModel0000", StringComparison.Ordinal)
            : schema;
    }

    private static string CreateDependentSchema(int index) => $$"""
        {
          "type": "record",
          "name": "BenchmarkModel{{index:D4}}",
          "namespace": "AvroSourceGenerator.BenchmarkModels",
          "fields": [
            {"name": "Value", "type": "string"},
            {"name": "Shared", "type": "BenchmarkModel0000"}
          ]
        }
        """;

    private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
    {
        private readonly SourceText _text = SourceText.From(text);

        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
    }
}

public enum IncrementalChangeScenario
{
    IndependentContent,
    ReferencedSchemaContent,
    SchemaIdentity,
}
