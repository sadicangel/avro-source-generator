using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests;

public sealed class SourcePathTests
{
    [Theory]
    [InlineData("schemas/./nested/../common.avsc", "schemas/common.avsc")]
    [InlineData(@"schemas\.\nested\..\common.avsc", "schemas/common.avsc")]
    [InlineData("../../schemas/../common.avsc", "../../common.avsc")]
    [InlineData("/schemas/../../common.avsc", "/common.avsc")]
    [InlineData(@"C:\schemas\..\common.avsc", "C:/common.avsc")]
    [InlineData(@"\\server\share\schemas\..\common.avsc", "//server/share/common.avsc")]
    public void Create_produces_a_canonical_identity(string path, string expected)
    {
        Assert.Equal(expected, new SourcePath(path).CanonicalPath);
    }

    [Theory]
    [InlineData(@"C:\Schemas\Common.avsc", "c:/schemas/common.avsc")]
    [InlineData("/Schemas/Common.avsc", "/schemas/common.avsc")]
    [InlineData("Schemas/Common.avsc", "schemas/common.avsc")]
    public void Ordinal_identity_is_case_sensitive(string firstPath, string secondPath)
    {
        var first = new SourcePath(firstPath);
        var second = new SourcePath(secondPath);

        Assert.False(first.Equals(second, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(@"C:\Schemas\Common.avsc", "c:/schemas/common.avsc")]
    [InlineData("/Schemas/Common.avsc", "/schemas/common.avsc")]
    [InlineData("Schemas/Common.avsc", "schemas/common.avsc")]
    public void Ordinal_ignore_case_identity_is_case_insensitive(string firstPath, string secondPath)
    {
        var first = new SourcePath(firstPath);
        var second = new SourcePath(secondPath);

        Assert.True(first.Equals(second, StringComparer.OrdinalIgnoreCase));
        Assert.Equal(
            first.GetHashCode(StringComparer.OrdinalIgnoreCase),
            second.GetHashCode(StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void Public_identity_uses_the_host_platform_comparer()
    {
        var first = new SourcePath("Schemas/Common.avsc");
        var second = new SourcePath("schemas/common.avsc");

        Assert.Equal(OperatingSystem.IsWindows(), first.Equals(second));
    }

    [Fact]
    public void CompareTo_orders_by_canonical_path()
    {
        var first = new SourcePath("z/../a.avsc");
        var second = new SourcePath("b.avsc");

        Assert.True(first.CompareTo(second) < 0);
    }

    [Fact]
    public void Source_text_equality_uses_identity_but_preserves_display_path()
    {
        var first = new SourceText(@"schemas\.\common.avsc", "{}");
        var second = new SourceText("schemas/common.avsc", "{}");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Equal(@"schemas\.\common.avsc", first.Path.OriginalPath);
        Assert.Equal("schemas/common.avsc", first.Path.CanonicalPath);
        Assert.Equal("schemas/common.avsc", second.Path.OriginalPath);
        Assert.Equal(@"schemas\.\common.avsc", first.Path.ToString());
        Assert.Equal(@"schemas\.\common.avsc", (string)first.Path);
    }

    [Fact]
    public void Constructor_rejects_a_null_original_path()
    {
        Assert.Throws<ArgumentNullException>(() => new SourcePath(null!));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("schema.avsc", false)]
    public void IsEmpty_reports_missing_display_paths(string path, bool expected)
    {
        Assert.Equal(expected, new SourcePath(path).IsEmpty);
    }

    [Fact]
    public void Default_value_is_safe_and_distinct_from_an_empty_path()
    {
        var path = default(SourcePath);

        Assert.Equal(string.Empty, path.OriginalPath);
        Assert.Equal(string.Empty, path.CanonicalPath);
        Assert.True(path.IsEmpty);
        Assert.False(path.IsRooted);
        Assert.Equal(string.Empty, path.ToString());
        Assert.Equal(string.Empty, (string)path);
        Assert.Equal(default, path);
        Assert.NotEqual(new SourcePath(string.Empty), path);
        Assert.True(path.CompareTo(new SourcePath(string.Empty)) < 0);
    }

}
