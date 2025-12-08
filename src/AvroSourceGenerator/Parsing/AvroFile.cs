using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Parsing;

internal readonly record struct AvroFile(
    string Path,
    string? Text,
    JsonElement Json,
    SchemaName SchemaName,
    ImmutableArray<DiagnosticInfo> Diagnostics)
{
    public bool IsValid => Json.ValueKind is not JsonValueKind.Undefined;

    public bool Equals(AvroFile other) => Path == other.Path && Text == other.Text;

    public override int GetHashCode() => HashCode.Combine(Path, Text);
}
