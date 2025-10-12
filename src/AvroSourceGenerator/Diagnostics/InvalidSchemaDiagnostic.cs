using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;

internal static class InvalidSchemaDiagnostic
{
    private static readonly DiagnosticDescriptor s_descriptor = new(
        id: "AVROSG0002",
        title: "Invalid Schema",
        messageFormat: "The schema defined in the JSON is invalid: {0}",
        category: "Compiler",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: """
            The JSON parsed, but the Avro schema is not valid according to the Avro specification
            (e.g., duplicate field names, invalid union members, unresolved references, etc.).
            """);

    public static Diagnostic Create(Location location, string message) =>
        Diagnostic.Create(s_descriptor, location, message);
}
