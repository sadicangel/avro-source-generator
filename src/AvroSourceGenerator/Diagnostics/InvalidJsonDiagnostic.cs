using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;

internal static class InvalidJsonDiagnostic
{
    private static readonly DiagnosticDescriptor s_invalidJsonSchemaDescriptor = new(
        id: "AVROSG0001",
        title: "Invalid JSON",
        messageFormat: "The provided JSON is invalid: {0}",
        category: "Syntax",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static Diagnostic Create(Location location, string message) =>
        Diagnostic.Create(s_invalidJsonSchemaDescriptor, location, message);
}
