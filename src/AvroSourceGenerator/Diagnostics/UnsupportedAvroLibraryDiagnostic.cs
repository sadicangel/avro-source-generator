using AvroSourceGenerator.Configuration;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;

internal static class MultipleAvroLibrariesDetectedDiagnostic
{
    public static readonly DiagnosticDescriptor Descriptor = new(
        id: "AVROSG0004",
        title: "Multiple Avro libraries detected",
        messageFormat:
        "Multiple Avro libraries are referenced: {0}. " +
        "Generation will fall back to 'None' (no library-specific code). " +
        "To target a specific library, set <AvroSourceGeneratorAvroLibrary> property to one of the following: {1} " +
        "in your .csproj or remove extra packages.",
        category: "Configuration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
        "The generator found more than one supported Avro library (e.g., Apache.Avro and Chr.Avro). " +
        "To avoid ambiguous generation, it will disable library-specific code (AvroLibrary=None). " +
        "Choose one library via the <AvroLibrary> property or uninstall the extras.");

    public static Diagnostic Create(Location location, IReadOnlyList<AvroLibraryReference> libraries) =>
        Diagnostic.Create(
            Descriptor,
            location,
            string.Join(", ", libraries.Select(x => x.PackageName)),
            string.Join(", ", libraries.Select(x => x.ToString())));
}
