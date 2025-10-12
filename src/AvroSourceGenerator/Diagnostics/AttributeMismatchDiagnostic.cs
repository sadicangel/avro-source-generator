using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;

internal static class AttributeMismatchDiagnostic
{
    private static readonly DiagnosticDescriptor s_descriptor = new(
        id: "AVROSG0003",
        title: "Attribute Mismatch",
        messageFormat: "No Avro schema found for type '{0}'. Expected a schema named '{1}' with namespace '{2}'.",
        category: "Configuration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: """
            A type attributed for Avro generation did not match any available Avro schema.
            Ensure the schema's name and namespace match the attributed type, and that the schema file is included in the project.
            """);

    public static Diagnostic Create(Location location, string name, string? @namespace) =>
        Diagnostic.Create(s_descriptor, location, @namespace is null ? name : $"{@namespace}.{name}", name, @namespace ?? string.Empty);
}
