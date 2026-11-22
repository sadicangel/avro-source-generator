using AvroSourceGenerator.Compiler.Configuration;

namespace AvroSourceGenerator.UnitTests.Apache.Snapshots;

public sealed class NativeCollectionUnionTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task Verify(bool nativeUnions) => Snapshot.Schema(
        File.ReadAllText(Path.Combine(TestSchemas.FixturesDirectory, "NativeCollectionUnions.avsc")),
        config => config with
        {
            AvroLibrary = "Apache",
            LanguageFeatures = nameof(LanguageFeatures.CSharp15),
            NativeUnions = nativeUnions ? "true" : "false",
        }).UseParameters(nativeUnions);
}
