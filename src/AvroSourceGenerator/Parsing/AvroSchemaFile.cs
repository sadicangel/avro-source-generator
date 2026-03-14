using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Diagnostics;

namespace AvroSourceGenerator.Parsing;

internal sealed record class AvroSchemaFile(string Path, string Text) : IAvroFile
{
    public JsonElement Json { get; init; } = ParseJson(Text);

    public ImmutableArray<DiagnosticInfo> Diagnostics => [];

    private static JsonElement ParseJson(string text)
    {
        using var jsonDocument = JsonDocument.Parse(text!);
        return jsonDocument.RootElement.Clone();
    }

    public bool Equals(AvroSchemaFile? other) => other is not null && Path == other.Path && Text == other.Text;

    public override int GetHashCode() => HashCode.Combine(Path, Text);
}
