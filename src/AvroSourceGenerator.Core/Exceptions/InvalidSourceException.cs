using System.Collections.Immutable;
using AvroSourceGenerator.Avdl.Diagnostics;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Exceptions;

public sealed class InvalidSourceException : Exception
{
    public InvalidSourceException(string message, SourceSpan sourceSpan)
        : this([new SyntaxDiagnostic(SyntaxDiagnosticCode.InvalidSource, sourceSpan, message)])
    {
    }

    public InvalidSourceException(ImmutableArray<SyntaxDiagnostic> diagnostics)
        : base(GetMessage(diagnostics))
    {
        Diagnostics = diagnostics;
    }

    public ImmutableArray<SyntaxDiagnostic> Diagnostics { get; }

    private static string GetMessage(ImmutableArray<SyntaxDiagnostic> diagnostics) =>
        diagnostics.IsEmpty
            ? throw new ArgumentException("At least one syntax diagnostic is required.", nameof(diagnostics))
            : diagnostics[0].Message;
}
