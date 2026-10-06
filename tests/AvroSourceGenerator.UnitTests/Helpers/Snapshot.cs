using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.UnitTests.Helpers;

internal class Snapshot : ISnapshot<Snapshot>
{
    public static ImmutableArray<MetadataReference> References => [];
    public static ProjectConfig ProjectConfig => new ProjectConfig() { AvroLibrary = "None" };
}
