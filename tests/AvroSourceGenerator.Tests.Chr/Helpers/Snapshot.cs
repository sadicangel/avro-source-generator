using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Tests.Chr.Helpers;

internal sealed class Snapshot : ISnapshot<Snapshot>
{
    public static ImmutableArray<MetadataReference> References { get; } =
    [
        MetadataReference.CreateFromFile(typeof(global::Chr.Avro.Abstract.Schema).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(global::Chr.Avro.Serialization.IBinarySerializerBuilder).Assembly.Location),
    ];

    public static ProjectConfig ProjectConfig => new ProjectConfig();

    // Ignore assembly reference mismatches because Chr.Avro references .NET 6, while we're targeting .NET 10.
    public static ImmutableArray<Diagnostic> FilterDiagnostics(ImmutableArray<Diagnostic> diagnostics) =>
        diagnostics.RemoveAll(d => d.Descriptor.Id is "CS1701");
}
