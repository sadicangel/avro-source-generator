using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Parsing;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;

internal static class NoAvroLibraryDetectedDiagnostic
{
    private static readonly DiagnosticDescriptor s_descriptor = new(
        id: "AVROSG0003",
        title: "No Avro library detected (Auto)",
        messageFormat:
        "AvroLibrary is set to 'Auto', but no supported Avro library was found. " +
        "AvroSourceGenerator will fall back to 'None' (no library-specific code). " +
        "To target a specific library, install one of: {0}. " +
        "To keep this behavior without warnings, set AvroSourceGeneratorAvroLibrary to 'None' in your .csproj.",
        category: "Configuration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
        "No supported Avro libraries were detected in the project while AvroLibrary=Auto. " +
        "The generator will disable library-specific code (AvroLibrary=None). " +
        "Install a supported library to enable generation, or set <AvroSourceGeneratorAvroLibrary>None</AvroSourceGeneratorAvroLibrary> " +
        "explicitly to silence this warning.");

    public static DiagnosticInfo Create(LocationInfo location) => new(s_descriptor, location, AvroLibraryReference.SupportedPackageList);
}
