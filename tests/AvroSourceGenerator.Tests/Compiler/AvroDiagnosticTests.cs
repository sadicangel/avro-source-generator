using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests.Compiler;

public sealed class AvroDiagnosticTests
{
    [Fact]
    public void Immutable_numeric_arguments_preserve_roslyn_culture_formatting()
    {
        var diagnostic = new AvroDiagnostic(AvroDiagnosticCode.InvalidIdlDeclaration, SourceSpan.None, 1234.5m);
        Assert.IsType<decimal>(Assert.Single(diagnostic.Arguments));
        var roslyn = diagnostic.ToDiagnostic();
        Assert.Contains("1234,5", roslyn.GetMessage(System.Globalization.CultureInfo.GetCultureInfo("fr-FR")));
        Assert.Contains("1234.5", roslyn.GetMessage(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Arguments_retain_the_supplied_immutable_array()
    {
        var arguments = ImmutableArray.Create<object?>("before", "second");
        var diagnostic = new AvroDiagnostic(AvroDiagnosticCode.UnexpectedToken, SourceSpan.None, arguments);
        var equal = new AvroDiagnostic(diagnostic.Code, diagnostic.SourceSpan, "before", "second");
        var message = diagnostic.GetMessage();
        var hash = diagnostic.GetHashCode();
        Assert.Equal(message, diagnostic.GetMessage());
        Assert.Equal(hash, diagnostic.GetHashCode());
        Assert.Equal(equal, diagnostic);
        Assert.Equal(message, diagnostic.ToDiagnostic().GetMessage());
        Assert.Equal(new object?[] { "before", "second" }, diagnostic.Arguments);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(4, 0)]
    [InlineData(2, 2)]
    [InlineData(1, int.MaxValue)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void Spans_reject_ranges_outside_the_source(int offset, int length)
    {
        var source = new SourceText("test.avsc", "abc");
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourceSpan(source, offset, length));
    }

    [Fact]
    public void Null_sources_require_the_explicit_none_sentinel()
    {
        Assert.Throws<ArgumentNullException>(() => new SourceSpan(null!, 0, 0));
        Assert.Throws<ArgumentNullException>(() => new SourceSpan(null!, 0));
        Assert.Equal(Microsoft.CodeAnalysis.Location.None, SourceSpan.None.ToLocation());
    }

    [Fact]
    public void None_or_empty_distinguishes_absent_spans_from_empty_source_text()
    {
        var emptyText = new SourceText("empty.avsc", "");
        var emptyPath = new SourceText("", "content");

        Assert.True(emptyText.IsEmpty);
        Assert.True(emptyPath.IsEmpty);
        Assert.True(SourceSpan.None.IsNone);
        Assert.True(SourceSpan.None.IsNoneOrEmpty);
        Assert.False(SourceSpan.FromSourceText(emptyText).IsNone);
        Assert.True(SourceSpan.FromSourceText(emptyText).IsNoneOrEmpty);
        Assert.False(SourceSpan.FromSourceText(emptyPath).IsNone);
        Assert.True(SourceSpan.FromSourceText(emptyPath).IsNoneOrEmpty);
    }

    [Fact]
    public void Lines_belong_to_the_original_source()
    {
        var source = new SourceText("test.avsc", "é\r\n😀\n");
        Assert.All(source.Lines, line => Assert.Same(source, line.SourceSpan.SourceText));
        Assert.Equal(source.Lines, source.Lines);
    }

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
    public void Source_file_span_uses_the_file_text()
    {
        var file = AvroFile.Parse(
            new SourceText("test.avsc", "abc"),
            new AvroParseOptions(),
            TestContext.Current.CancellationToken);

        ISourceFile sourceFile = file;
        var span = SourceSpan.FromSourceFile(sourceFile, 1, 1);

        Assert.Equal(file.Text, span.SourceText);
        Assert.Equal(file.Path, span.SourceText.Path);
        Assert.Equal("b", span.ToString());
    }

    [Fact]
    public void Diagnostics_expose_warning_severity_without_roslyn()
    {
        var diagnostic = new AvroDiagnostic(AvroDiagnosticCode.NoAvroLibraryDetected, SourceSpan.None, "Apache.Avro");
        Assert.Equal(AvroDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.NotEmpty(diagnostic.GetMessage());
    }

    [Fact]
    public void Error_extensions_detect_individual_and_collection_errors()
    {
        var warning = new AvroDiagnostic(AvroDiagnosticCode.NoAvroLibraryDetected, SourceSpan.None, "Apache.Avro");
        var error = new AvroDiagnostic(AvroDiagnosticCode.InvalidIdlDeclaration, SourceSpan.None, "Invalid");

        Assert.False(warning.IsError);
        Assert.True(error.IsError);
        Assert.False(Array.Empty<AvroDiagnostic>().HasErrors);
        Assert.False(new[] { warning }.HasErrors);
        Assert.True(new[] { warning, error }.HasErrors);
    }
}
