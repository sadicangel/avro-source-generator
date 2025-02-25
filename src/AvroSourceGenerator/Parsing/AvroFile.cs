using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.Parsing;

internal readonly record struct AvroFile(
    string Path,
    string? Text,
    JsonElement Json,
    string Name,
    string? Namespace,
    ImmutableArray<Diagnostic> Diagnostics)
    : IEquatable<AvroFile>
{
    public bool IsValid => Json.ValueKind is not JsonValueKind.Undefined && !string.IsNullOrEmpty(Name);

    public Location GetLocation() => Path.GetLocation(Text);

    public Location GetLocation(TextSpan textSpan, LinePositionSpan lineSpan) => Path.GetLocation(textSpan, lineSpan);

    public bool Equals(AvroFile other) => Path == other.Path && Text == other.Text;

    public override int GetHashCode() => HashCode.Combine(Path, Text);
}
