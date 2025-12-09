using System.Collections.Immutable;
using AvroSourceGenerator.Diagnostics;

namespace AvroSourceGenerator.Emit;

internal readonly record struct RenderResult(ImmutableArray<RenderedSchema> Schemas, ImmutableArray<DiagnosticInfo> Diagnostics)
{
    public bool Equals(RenderResult other) => Schemas.OrderBy(x => x.HintName).SequenceEqual(other.Schemas.OrderBy(x => x.HintName));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var schema in Schemas)
            hash.Add(schema.GetHashCode());
        return hash.ToHashCode();
    }
}
