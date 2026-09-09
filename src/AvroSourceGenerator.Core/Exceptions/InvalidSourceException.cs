using System.Collections.Immutable;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Exceptions;

public sealed class InvalidSourceException(ImmutableArray<AvroDiagnostic> diagnostics) : Exception(GetMessage(diagnostics))
{
    public InvalidSourceException(string message, SourceSpan sourceSpan)
        : this([AvroDiagnostic.InvalidSource(sourceSpan, message)]) { }

    public ImmutableArray<AvroDiagnostic> Diagnostics { get; } = diagnostics;

    private static string GetMessage(ImmutableArray<AvroDiagnostic> diagnostics) =>
        diagnostics.IsEmpty
            ? throw new ArgumentException("At least one syntax diagnostic is required.", nameof(diagnostics))
            : diagnostics[0].GetMessage();
}
