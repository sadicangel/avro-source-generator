using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests;

public sealed class SourcePathResolutionTests
{
    [Theory]
    [InlineData("service.avdl", "", "")]
    [InlineData("project/service.avdl", ".", "project")]
    [InlineData("project/./service.avdl", "child.avsc", "project/child.avsc")]
    [InlineData("project/service.avdl", "a//b/.././child.avsc", "project/a/child.avsc")]
    [InlineData("../service.avdl", "../../child.avsc", "../../../child.avsc")]
    [InlineData("project/service.avdl", "a/../../..", "..")]
    [InlineData(@"project\idl/service.avdl", @"..\schemas/./example.avsc", "project/schemas/example.avsc")]
    public void Resolve_normalizes_separators_and_dot_segments(string importer, string import, string expected)
    {
        var resolved = new SourcePath(importer).Resolve(import);

        Assert.Equal(expected, resolved.CanonicalPath);
    }

    [Theory]
    [InlineData("C:/idl/service.avdl", @"..\shared\file.avsc", "C:/shared/file.avsc")]
    [InlineData(@"C:\idl\service.avdl", "../shared/file.avsc", "C:/shared/file.avsc")]
    [InlineData(@"\\server\share\idl\service.avdl", @"..\..\file.avsc", "//server/share/file.avsc")]
    [InlineData("/idl/service.avdl", "../../shared/file.avsc", "/shared/file.avsc")]
    public void Resolve_preserves_windows_and_unix_roots(string importer, string import, string expected)
    {
        var resolved = new SourcePath(importer).Resolve(import);

        Assert.True(resolved.IsRooted);
        Assert.Equal(expected, resolved.CanonicalPath);
    }

    [Fact]
    public void Rooted_import_replaces_the_importer_root()
    {
        var resolved = new SourcePath("C:/idl/service.avdl").Resolve("/shared/example.avsc");

        Assert.Equal("/shared/example.avsc", resolved.CanonicalPath);
    }
}
