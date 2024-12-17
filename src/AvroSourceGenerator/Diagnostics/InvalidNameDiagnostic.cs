using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;

internal static class InvalidNameDiagnostic
{
    private static readonly DiagnosticDescriptor s_invalidNameDescriptor = new(
        id: "AVROSG0003",
        title: "Invalid Name",
        messageFormat: "The name '{0}' in the schema does not match the class name '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // TODO: Add class location also.
    public static Diagnostic Create(Location location, string schemaName, string className) =>
        Diagnostic.Create(s_invalidNameDescriptor, location, schemaName, className);
}

