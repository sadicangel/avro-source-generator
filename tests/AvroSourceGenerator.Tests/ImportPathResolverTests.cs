using AvroSourceGenerator.Output;

namespace AvroSourceGenerator.Tests;

public sealed class ImportPathResolverTests
{
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
