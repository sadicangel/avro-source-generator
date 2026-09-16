using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests.Compiler;

public sealed class SourceTypeTests
{
    [Theory]
    [InlineData("schema.avsc", SourceType.Avsc)]
    [InlineData("protocol.AVPR", SourceType.Avpr)]
    [InlineData("schema.AvDl", SourceType.Avdl)]
    public void Classifies_supported_source_paths(string path, SourceType expected)
    {
        Assert.True(new SourcePath(path).TryGetSourceType(out var sourceType));
        Assert.Equal(expected, sourceType);
    }

    [Theory]
    [InlineData("schema")]
    [InlineData("schema.json")]
    [InlineData("")]
    public void Unsupported_source_paths_are_not_exceptional(string path)
    {
        Assert.False(new SourcePath(path).TryGetSourceType(out _));
    }

    [Fact]
    public void Source_text_does_not_expose_source_type()
    {
        Assert.Null(typeof(SourceText).GetProperty("Type"));
    }
}
