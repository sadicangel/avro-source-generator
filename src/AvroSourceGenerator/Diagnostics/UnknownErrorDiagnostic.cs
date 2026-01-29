using AvroSourceGenerator.Parsing;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;

internal static class UnknownErrorDiagnostic
{
    private static readonly DiagnosticDescriptor s_descriptor = new DiagnosticDescriptor(id: "AVROSG9999", title: "Unknown error in Avro Source Generator", messageFormat: "An unknown error occurred in Avro Source Generator: {0}", category: "Compiler", defaultSeverity: DiagnosticSeverity.Error, isEnabledByDefault: true, description: "An unexpected error occurred during the execution of the Avro Source Generator. Please report this issue with details about how to reproduce it.");

    public static DiagnosticInfo Create(LocationInfo location, string errorMessage) => new DiagnosticInfo(s_descriptor, location, errorMessage);
}
