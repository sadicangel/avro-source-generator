using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Schemas;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.Parsing;

internal readonly record struct AvroFile(
    string Path,
    string? Text,
    JsonElement Json,
    SchemaName SchemaName,
    ImmutableArray<Diagnostic> Diagnostics)
{
    public bool IsValid => Json.ValueKind is not JsonValueKind.Undefined;

    public Location GetLocation() => Path.GetLocation(Text);

    public Location GetLocation(TextSpan textSpan, LinePositionSpan lineSpan) => Path.GetLocation(textSpan, lineSpan);

    public Location GetLocation(JsonException exception) => Path.GetLocation(Text, exception);

    public bool Equals(AvroFile other) => Path == other.Path && Text == other.Text;

    public override int GetHashCode() => HashCode.Combine(Path, Text);
}
