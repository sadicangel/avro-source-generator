using AvroSourceGenerator.AvroIDL.Text;

namespace AvroSourceGenerator.AvroIDL.Diagnostics;
public sealed record class Diagnostic(string Id, SourceSpan SourceSpan, DiagnosticSeverity Severity, string Message);
