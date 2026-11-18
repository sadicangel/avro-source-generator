using System.Collections.Immutable;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Syntax;

internal sealed record class SyntaxTree<T>(SourceText SourceText, T Root, ImmutableArray<AvroDiagnostic> Diagnostics)
    where T : ISyntaxNode;
