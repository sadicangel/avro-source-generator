using System.Text.Json;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Text;
namespace AvroSourceGenerator.Tests.Compiler;

public sealed class AvroDiagnosticTests
{
    [Fact]
    public void Equality_includes_arguments_and_their_order()
    {
        var first = new AvroDiagnostic(AvroDiagnosticCode.UnexpectedToken, SourceSpan.None, "a", "b");
        var same = new AvroDiagnostic(AvroDiagnosticCode.UnexpectedToken, default, "a", "b");
        Assert.Equal(first, same);
        Assert.Equal(first.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(first, new AvroDiagnostic(first.Code, first.SourceSpan, "b", "a"));
        Assert.NotEqual(first, new AvroDiagnostic(first.Code, first.SourceSpan, "c", "b"));
        Assert.True(SourceSpan.None.IsNone);
        Assert.Equal("", SourceSpan.None.ToString());
    }

    [Theory]
    [InlineData("é")]
    [InlineData("😀")]
    [InlineData("世界😀é")]
    public void Json_byte_positions_map_to_utf16_offsets(string value)
    {
        var source = new SourceText("test.avsc", "{\n\"value\":\"" + value + "\",\"bad\":!\n}");
        var exception = Assert.ThrowsAny<JsonException>(() => JsonDocument.Parse(source.Text));
        var span = SourceSpan.FromException(source, exception);
        Assert.Equal(source.Text.IndexOf('!'), span.Offset);
        Assert.Equal("!", span.ToString());
    }

    [Fact]
    public void Source_length_and_line_offsets_include_whitespace()
    {
        var source = new SourceText("blank.avsc", " \r\n\t");
        Assert.True(source.IsEmpty);
        Assert.Equal(4, source.Length);
        Assert.Equal(3, source.GetOffset(1, 0));
        Assert.False(source.Equals(null));
        Assert.Equal(source.Text, SourceSpan.FromSourceText(source).ToString());
    }

    [Fact]
    public void Diagnostics_expose_warning_severity_without_roslyn()
    {
        var diagnostic = new AvroDiagnostic(AvroDiagnosticCode.NoAvroLibraryDetected, SourceSpan.None, "Apache.Avro");
        Assert.Equal(AvroDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("Apache.Avro", diagnostic.GetMessage());
    }

    [Fact]
    public void Error_extensions_detect_individual_and_collection_errors()
    {
        var warning = new AvroDiagnostic(AvroDiagnosticCode.NoAvroLibraryDetected, SourceSpan.None, "Apache.Avro");
        var error = new AvroDiagnostic(AvroDiagnosticCode.InvalidSource, SourceSpan.None, "Invalid");

        Assert.False(warning.IsError);
        Assert.True(error.IsError);
        Assert.False(Array.Empty<AvroDiagnostic>().HasErrors);
        Assert.False(new[] { warning }.HasErrors);
        Assert.True(new[] { warning, error }.HasErrors);
    }
}
