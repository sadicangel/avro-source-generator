using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;
internal static class InvalidAvroSchemaDiagnostic
{
    private static readonly DiagnosticDescriptor s_invalidAvroSchemaDescriptor = new(
        id: "AVROSG0004",
        title: "Invalid Avro Schema",
        messageFormat: "The schema defined in the JSON is invalid: {0}",
        category: "Semantic",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static Diagnostic Create(Location location, string message) =>
        Diagnostic.Create(s_invalidAvroSchemaDescriptor, location, message);
}
