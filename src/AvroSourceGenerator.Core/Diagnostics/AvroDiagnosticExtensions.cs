namespace AvroSourceGenerator.Diagnostics;

public static class AvroDiagnosticExtensions
{
    extension(AvroDiagnostic diagnostic)
    {
        public bool IsError => diagnostic.Severity is AvroDiagnosticSeverity.Error;
    }

    extension(IEnumerable<AvroDiagnostic> diagnostics)
    {
        public bool HasErrors => diagnostics.Any(static diagnostic => diagnostic.IsError);
    }
}
