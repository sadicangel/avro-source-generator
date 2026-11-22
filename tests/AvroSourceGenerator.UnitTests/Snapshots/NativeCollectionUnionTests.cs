using AvroSourceGenerator.Compiler.Configuration;

namespace AvroSourceGenerator.UnitTests.Snapshots;

public sealed class NativeCollectionUnionTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task Verify(bool nativeUnions) => Snapshot.Schema(
        File.ReadAllText(Path.Combine(TestSchemas.FixturesDirectory, "NativeCollectionUnions.avsc")),
        config => config with
        {
            LanguageFeatures = nameof(LanguageFeatures.CSharp15),
            NativeUnions = nativeUnions ? "true" : "false",
        }).UseParameters(nativeUnions);
}
