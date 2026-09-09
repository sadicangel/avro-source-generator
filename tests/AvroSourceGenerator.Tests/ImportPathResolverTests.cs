using AvroSourceGenerator.Compiler;

namespace AvroSourceGenerator.Tests;

public sealed class ImportPathResolverTests
{
    [Theory]
    [InlineData("service.avdl", "", "")]
    [InlineData("project/service.avdl", ".", "project")]
    [InlineData("project/./service.avdl", "child.avsc", "project/./child.avsc")]
    [InlineData("project/service.avdl", "a//b/.././child.avsc", "project/a/child.avsc")]
    [InlineData("../service.avdl", "../../child.avsc", "../../../child.avsc")]
    [InlineData("project/service.avdl", "a/../../..", "..")]
    public void Resolve_preserves_relative_path_edge_cases(string importer, string import, string expected)
    {
        Assert.Equal(expected, ImportPathResolver.Resolve(importer, import));
    }

    [Fact]
    public void Resolve_normalizes_rooted_imports_and_trailing_separators()
    {
        var root = Path.GetPathRoot(Environment.CurrentDirectory)!;
        var imported = Path.Combine(root, "schemas", "..", "shared") + Path.DirectorySeparatorChar;
        Assert.Equal(Path.Combine(root, "shared"), ImportPathResolver.Resolve("service.avdl", imported));
    }

    [Fact]
    public void Resolve_preserves_windows_drive_unc_and_mixed_separators()
    {
        if (Path.DirectorySeparatorChar == Path.AltDirectorySeparatorChar) return;
        Assert.Equal("C:/shared/file.avsc", ImportPathResolver.Resolve("C:/idl/service.avdl", @"..\shared\file.avsc"));
        // Preserve the existing root concatenation: UNC roots lack a trailing separator.
        Assert.Equal(@"\\server\sharefile.avsc", ImportPathResolver.Resolve(@"\\server\share\idl\service.avdl", @"..\..\file.avsc"));
    }

    [Fact]
    public void Resolve_NormalizesRelativeSegments()
    {
        var importerPath = Path.Combine("project", "idl", "service.avdl");
        var importPath = Path.Combine("..", "schemas", ".", "example.avsc");

        var resolved = ImportPathResolver.Resolve(importerPath, importPath);

        Assert.Equal(Path.Combine("project", "schemas", "example.avsc"), resolved);
        Assert.False(Path.IsPathRooted(resolved));
    }

    [Fact]
    public void Resolve_PreservesExistingParentSegmentsInImporterPath()
    {
        var importerPath = Path.Combine("project", "tests", "consumer", "..", "schemas", "service.avdl");

        var resolved = ImportPathResolver.Resolve(importerPath, "example.avsc");

        Assert.Equal(
            Path.Combine("project", "tests", "consumer", "..", "schemas", "example.avsc"),
            resolved);
    }

    [Fact]
    public void Resolve_PreservesLeadingParentSegments()
    {
        var importerPath = Path.Combine("idl", "service.avdl");
        var importPath = Path.Combine("..", "..", "schemas", "example.avsc");

        var resolved = ImportPathResolver.Resolve(importerPath, importPath);

        Assert.Equal(Path.Combine("..", "schemas", "example.avsc"), resolved);
        Assert.False(Path.IsPathRooted(resolved));
    }

    [Fact]
    public void Resolve_DoesNotEscapeRoot()
    {
        var root = Path.GetPathRoot(Environment.CurrentDirectory)!;
        var importerPath = Path.Combine(root, "service.avdl");
        var importPath = Path.Combine("..", "schemas", "example.avsc");

        var resolved = ImportPathResolver.Resolve(importerPath, importPath);

        Assert.Equal(Path.Combine(root, "schemas", "example.avsc"), resolved);
    }

    [Fact]
    public void Resolve_PreservesImporterSeparatorStyle()
    {
        var resolved = ImportPathResolver.Resolve(
            "schemas/imports/service.avdl",
            "../../shared/example.avsc");

        Assert.Equal("shared/example.avsc", resolved);
    }
}
