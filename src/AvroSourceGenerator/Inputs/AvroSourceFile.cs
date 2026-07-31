using System.Collections.Immutable;
using AvroSourceGenerator.Avdl.Syntax;
using AvroSourceGenerator.Avdl.Text;
using AvroSourceGenerator.Diagnostics;

namespace AvroSourceGenerator.Inputs;

internal sealed record class AvroSourceFile(string Path, string Text) : IAvroFile
{
    public SyntaxTree SyntaxTree => field ??= Parser.Parse(new SourceText(Path, Text));

    public ImmutableArray<DiagnosticInfo> Diagnostics =>
        !field.IsDefault ? field : field = [.. SyntaxTree.Diagnostics.Select(InvalidSyntaxDiagnostic.Create)];

    public bool Equals(AvroSourceFile? other) => other is not null && Path == other.Path && Text == other.Text;

    public override int GetHashCode() => HashCode.Combine(Path, Text);
}
