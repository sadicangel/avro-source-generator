using AvroSourceGenerator.Avdl.Text;

namespace AvroSourceGenerator.Tests.Avdl;

public sealed class SourceTextTests
{
    [Fact]
    public void Lines_EmptyText_ReturnsSingleEmptyLine()
    {
        var sourceText = new SourceText("test.avdl", "");

        var line = Assert.Single(sourceText.Lines);
        Assert.Equal(0, line.SourceSpan.Offset);
        Assert.Equal(0, line.SourceSpan.Length);
        Assert.Equal(0, line.SourceSpanWithLineBreak.Offset);
        Assert.Equal(0, line.SourceSpanWithLineBreak.Length);
        Assert.Equal("", line.ToString());
    }

    [Fact]
    public void Lines_MixedLineBreaks_ReturnsLineSpansWithAndWithoutLineBreaks()
    {
        var sourceText = new SourceText("test.avdl", "a\r\nb\rc\n");

        Assert.Collection(
            sourceText.Lines,
            line => AssertLine(line, 0, 1, 3, "a", "a\r\n"),
            line => AssertLine(line, 3, 1, 2, "b", "b\r"),
            line => AssertLine(line, 5, 1, 2, "c", "c\n"),
            line => AssertLine(line, 7, 0, 0, "", ""));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 1)]
    [InlineData(4, 1)]
    [InlineData(5, 2)]
    [InlineData(6, 2)]
    [InlineData(7, 3)]
    public void GetLineIndex_ReturnsLineContainingOffset(int offset, int expectedLineIndex)
    {
        var sourceText = new SourceText("test.avdl", "a\r\nb\rc\n");

        Assert.Equal(expectedLineIndex, sourceText.GetLineIndex(offset));
    }

    [Fact]
    public void GetSpan_ReturnsSpanOverSourceText()
    {
        var sourceText = new SourceText("test.avdl", "namespace example;");

        var span = sourceText.GetSpan(10, 7);

        Assert.Equal(sourceText, span.SourceText);
        Assert.Equal(10, span.Offset);
        Assert.Equal(7, span.Length);
        Assert.Equal("example", span.ToString());
    }

    private static void AssertLine(SourceLine line, int offset, int length, int lengthWithLineBreak, string text, string textWithLineBreak)
    {
        Assert.Equal(offset, line.SourceSpan.Offset);
        Assert.Equal(length, line.SourceSpan.Length);
        Assert.Equal(offset, line.SourceSpanWithLineBreak.Offset);
        Assert.Equal(lengthWithLineBreak, line.SourceSpanWithLineBreak.Length);
        Assert.Equal(text, line.ToString());
        Assert.Equal(textWithLineBreak, line.SourceSpanWithLineBreak.ToString());
    }
}
