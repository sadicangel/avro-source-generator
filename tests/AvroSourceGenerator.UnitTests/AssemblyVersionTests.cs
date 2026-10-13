using System.Reflection;
using AvroSourceGenerator.Cli;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Templating;

namespace AvroSourceGenerator.UnitTests;

public sealed class AssemblyVersionTests
{
    private static string ExpectedVersion => typeof(CliApplication).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

    [Fact]
    public void Source_assemblies_share_the_same_version()
    {
        var numeric = Version.Parse(ExpectedVersion.Split('-', '+')[0]);
        var expectedNumeric = new Version(numeric.Major, numeric.Minor, numeric.Build, Math.Max(numeric.Revision, 0)).ToString();

        foreach (var assembly in new[]
        {
            typeof(AvroCompiler).Assembly,
            typeof(AvroTemplate).Assembly,
            typeof(AvroSourceGenerator).Assembly,
            typeof(CliApplication).Assembly,
        })
        {
            Assert.Equal(expectedNumeric, assembly.GetName().Version!.ToString());
            Assert.Equal(expectedNumeric, assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version);
            Assert.Equal(ExpectedVersion, assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion);
        }
    }

    [Fact]
    public void Generated_code_uses_the_actual_version_before_snapshot_normalization()
    {
        var output = GeneratorOutput.Create(GeneratorInput.Create(
            [ProjectFile.Schema("""{"type":"record","name":"Versioned","fields":[]}""")],
            Snapshot.References,
            Snapshot.ProjectConfig));

        Assert.Empty(output.Diagnostics);
        var version = typeof(AvroTemplate).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        Assert.Equal(ExpectedVersion, version);
        Assert.Contains(
            $"[global::System.CodeDom.Compiler.GeneratedCode(\"AvroSourceGenerator\", \"{version}\")]",
            Assert.Single(output.Documents).Content,
            StringComparison.Ordinal);
    }
}
