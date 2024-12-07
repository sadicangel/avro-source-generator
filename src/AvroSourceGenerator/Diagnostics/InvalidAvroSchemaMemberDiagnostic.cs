using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;
internal static class InvalidAvroSchemaMemberDiagnostic
{
    private static readonly DiagnosticDescriptor s_invalidAvroSchemaMemberDescriptor = new(
        id: "AVROSG0002",
        title: "Invalid AvroSchema Member",
        messageFormat: "The 'AvroSchema' member must be a constant string field",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static Diagnostic Create(Location location) =>
        Diagnostic.Create(s_invalidAvroSchemaMemberDescriptor, location);
}
