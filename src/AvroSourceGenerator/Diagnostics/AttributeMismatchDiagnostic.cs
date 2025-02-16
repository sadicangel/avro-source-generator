using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;

internal static class AttributeMismatchDiagnostic
{
    private static readonly DiagnosticDescriptor s_invalidAttributeDescriptor = new(
        id: "AVROSG0003",
        title: "Attribute Mismatch",
        messageFormat: "No Avro schema found for class '{0}'. Expected a file containing a schema with name '{1}' and namespace '{2}'.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // TODO: Add class location also.
    public static Diagnostic Create(Location location, QualifiedName name) =>
        Diagnostic.Create(s_invalidAttributeDescriptor, location, name.PartiallyQualifiedName, name.LocalName, name.Namespace ?? string.Empty);
}

