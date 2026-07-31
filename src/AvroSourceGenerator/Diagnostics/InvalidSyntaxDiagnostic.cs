using AvroSourceGenerator.Avdl.Diagnostics;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;

internal static class InvalidSyntaxDiagnostic
{
    private static readonly DiagnosticDescriptor s_descriptor = new(
        id: "AVROSG1000",
        title: "Invalid Avro IDL",
        messageFormat: "The provided Avro IDL source is invalid: {0}",
        category: "Compiler",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The Avro IDL source contains syntax or source-level validation errors. Fix the IDL definition.");

    public static DiagnosticInfo Create(SyntaxDiagnostic diagnostic) => new(s_descriptor, LocationInfo.FromSourceSpan(diagnostic.SourceSpan), diagnostic.Message);

    public static DiagnosticInfo Create(LocationInfo location, string message) => new(s_descriptor, location, message);
}
