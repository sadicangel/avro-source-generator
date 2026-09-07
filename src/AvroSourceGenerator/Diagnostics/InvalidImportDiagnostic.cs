using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;

internal static class InvalidImportDiagnostic
{
    private static readonly DiagnosticDescriptor s_descriptor = new(
        id: "AVROSG1001",
        title: "Invalid Avro import",
        messageFormat: "The Avro import is invalid: {0}",
        category: "Compiler",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An Avro IDL import path could not be resolved to a compatible AdditionalFile.");

    public static DiagnosticInfo Create(LocationInfo location, string message) => new(s_descriptor, location, message);
}
