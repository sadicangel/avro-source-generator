using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;

internal static class MissingAvroSchemaMemberDiagnostic
{
    private static readonly DiagnosticDescriptor s_missingAvroSchemaConstantDescriptor = new(
        id: "AVROSG0001",
        title: "Missing AvroSchema Member",
        messageFormat: "The class must define a constant string field named 'AvroSchema' containing the Avro JSON schema",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static Diagnostic Create(Location location) =>
        Diagnostic.Create(s_missingAvroSchemaConstantDescriptor, location);
}
