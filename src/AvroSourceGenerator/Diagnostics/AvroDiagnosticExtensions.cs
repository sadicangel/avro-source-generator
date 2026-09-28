using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Diagnostics;

internal static class AvroDiagnosticExtensions
{
    extension(AvroDiagnostic)
    {
        public static AvroDiagnostic NoAvroLibraryDetected(SourceSpan sourceSpan) =>
            new(AvroDiagnosticCode.NoAvroLibraryDetected, sourceSpan, AvroLibraryReference.SupportedPackageList);

        public static AvroDiagnostic MultipleAvroLibrariesDetected(SourceSpan sourceSpan, IReadOnlyCollection<AvroLibraryReference> references) =>
            new(AvroDiagnosticCode.MultipleAvroLibrariesDetected, sourceSpan, string.Join(", ", references.Select(static reference => $"'{reference.PackageName}'")), AvroLibraryReference.SupportedPackageList);
    }
}
