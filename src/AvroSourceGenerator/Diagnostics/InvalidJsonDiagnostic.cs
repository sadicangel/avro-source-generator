using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;

internal static class InvalidJsonDiagnostic
{
    public static readonly DiagnosticDescriptor Descriptor = new(
        id: "AVROSG0001",
        title: "Invalid JSON",
        messageFormat: "The provided JSON is invalid: {0}",
        category: "Compiler",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
        "The JSON supplied for an Avro schema could not be parsed. " +
        "Fix the JSON syntax (quotes, commas, braces, etc.). If the error reports a path, check that location first.");

    public static Diagnostic Create(Location location, string message) =>
        Diagnostic.Create(Descriptor, location, message);
}
