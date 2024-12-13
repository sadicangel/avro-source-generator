using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;
internal static class InvalidNamespaceDiagnostic
{
    private static readonly DiagnosticDescriptor s_invalidNamespaceDescriptor = new(
        id: "AVROSG0004",
        title: "Invalid Namespace",
        messageFormat: "The namespace '{0}' in the schema does not match the class namespace '{1}'. If this behaviour is intended, set 'UseCSharpNamespace' to true.",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static Diagnostic Create(Location location, string schemaNamespace, string classNamespace) =>
        Diagnostic.Create(s_invalidNamespaceDescriptor, location, schemaNamespace, classNamespace);
}
