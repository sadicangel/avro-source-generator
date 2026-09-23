using System.Collections.Immutable;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Avdl.Syntax;

public sealed record class SyntaxTree(SourceText SourceText, DocumentSyntax Document, ImmutableArray<AvroDiagnostic> Diagnostics);
