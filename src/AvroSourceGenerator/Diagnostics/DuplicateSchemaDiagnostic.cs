using AvroSourceGenerator.Parsing;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;

internal static class DuplicateSchemaDiagnostic
{
    private static readonly DiagnosticDescriptor s_descriptor = new(id: "AVROSG0005", title: "Duplicate Avro schema detected", messageFormat: "Multiple Avro schema files would generate the same type '{0}'. " + "By default this is treated as an error to avoid ambiguous code generation. " + "You can allow one schema to be chosen arbitrarily by setting " + "<AvroSourceGeneratorDuplicateResolution>Ignore</AvroSourceGeneratorDuplicateResolution> " + "in the project file.", category: "Compiler", defaultSeverity: DiagnosticSeverity.Error, isEnabledByDefault: true, description: "Two or more Avro schema input files map to the same generated type. " + "This creates an ambiguous generation result. " + "By default, AvroSourceGenerator fails fast with an error. " + "If you intentionally want to allow duplicates and accept a nondeterministic " + "selection of the generated output, set the MSBuild property " + "<AvroSourceGeneratorDuplicateResolution>Ignore</AvroSourceGeneratorDuplicateResolution>.");

    public static DiagnosticInfo Create(LocationInfo location, string csharpName) => new(s_descriptor, location, csharpName);
}
