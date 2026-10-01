using System.Collections.Immutable;
using Avro;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Tests.Apache.Helpers;

internal sealed class Snapshot : ISnapshot<Snapshot>
{
    public static ImmutableArray<MetadataReference> References { get; } = [MetadataReference.CreateFromFile(typeof(Schema).Assembly.Location)];
    public static ProjectConfig ProjectConfig => new ProjectConfig();
}
