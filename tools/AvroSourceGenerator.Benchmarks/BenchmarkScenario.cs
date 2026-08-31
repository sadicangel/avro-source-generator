using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.Benchmarks;

internal sealed record BenchmarkScenario(
    int SchemaCount,
    ImmutableArray<AdditionalText> Files,
    AdditionalText OriginalFile,
    AdditionalText ChangedFile)
{
    public const int DefaultSchemaCount = 250;
    private const int FieldGroupCount = 4;

    public static BenchmarkScenario Create(int schemaCount)
    {
        if (schemaCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaCount));
        }

        var files = Enumerable.Range(0, schemaCount)
            .Select(index => (AdditionalText)new InMemoryAdditionalText(
                GetPath(index),
                CreateSchema(index, includeChangedField: false)))
            .ToImmutableArray();
        var changedFile = new InMemoryAdditionalText(
            GetPath(0),
            CreateSchema(0, includeChangedField: true));

        return new BenchmarkScenario(schemaCount, files, files[0], changedFile);
    }

    private static string GetPath(int index) => $"Schemas/BenchmarkModel{index:D4}.avsc";

    private static string CreateSchema(int index, bool includeChangedField)
    {
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

    private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
    {
        private readonly SourceText _text = SourceText.From(text);

        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
    }
}
