using AvroSourceGenerator.Avdl.Text;

namespace AvroSourceGenerator.Avdl.Diagnostics;

public enum SyntaxDiagnosticCode
{
    UnexpectedToken,
}

public readonly record struct SyntaxDiagnostic(SyntaxDiagnosticCode Code, SourceSpan SourceSpan, string Message);
