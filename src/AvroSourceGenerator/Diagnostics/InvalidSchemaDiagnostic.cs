using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;
internal static class InvalidSchemaDiagnostic
{
    private static readonly DiagnosticDescriptor s_invalidSchemaDescriptor = new(
        id: "AVROSG0002",
        title: "Invalid Schema",
        messageFormat: "The schema defined in the JSON is invalid: {0}",
        category: "Semantic",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static Diagnostic Create(Location location, string message) =>
        Diagnostic.Create(s_invalidSchemaDescriptor, location, message);
}
