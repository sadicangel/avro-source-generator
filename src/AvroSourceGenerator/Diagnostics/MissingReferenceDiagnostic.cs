using System.Collections.Immutable;
using AvroSourceGenerator.Parsing;
using AvroSourceGenerator.Schemas;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;

internal static class MissingReferenceDiagnostic
{
    private static readonly DiagnosticDescriptor s_descriptor = new DiagnosticDescriptor(id: "AVROSG0006", title: "Missing schema reference", messageFormat: "The following schema references could not be resolved: {0}", category: "Compiler", defaultSeverity: DiagnosticSeverity.Error, isEnabledByDefault: true, description: "One or more Avro schema references could not be resolved from the available schema inputs or declared subject references.");

    public static DiagnosticInfo Create(LocationInfo location, ImmutableArray<SchemaName> missingReferences) => new DiagnosticInfo(s_descriptor, location, string.Join(", ", missingReferences.Select(static x => x.FullName)));
}
