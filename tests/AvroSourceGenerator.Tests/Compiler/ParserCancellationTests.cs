using System.Text;
using AvroSourceGenerator.Avdl;
using AvroSourceGenerator.Avdl.Syntax;
using AvroSourceGenerator.Avsc;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests.Compiler;

public sealed class ParserCancellationTests
{
    private static readonly AvroParseOptions Options = new(GenerationTarget.Modern, true);

    [Fact]
    public void AvroFile_propagates_pre_cancelled_token()
    {
        var token = CreateCancelledToken();

        Assert.Throws<OperationCanceledException>(() =>
            AvroFile.Parse(new SourceText("test.avdl", "schema R; record R {}"), Options, token));
    }

    [Fact]
    public void Avdl_parser_propagates_pre_cancelled_token()
    {
        var token = CreateCancelledToken();

        Assert.Throws<OperationCanceledException>(() =>
            Parser.Parse(new SourceText("test.avdl", "schema R; record R {}"), token));
    }

    [Fact]
    public void Avdl_scanner_propagates_pre_cancelled_token()
    {
        var token = CreateCancelledToken();
        var scanner = new Scanner(new SourceText("test.avdl", "record R {}"), token);

        Assert.Throws<OperationCanceledException>(() => scanner.Scan());
    }

    [Fact]
    public void Avdl_schema_parser_propagates_pre_cancelled_token()
    {
        var token = CreateCancelledToken();

        Assert.Throws<OperationCanceledException>(() =>
            AvdlSchemaParser.Parse(new SourceText("test.avdl", "schema R; record R {}"), Options, token));
    }

    [Fact]
    public void Avsc_schema_parser_propagates_pre_cancelled_token()
    {
        var token = CreateCancelledToken();

        Assert.Throws<OperationCanceledException>(() =>
            AvscSchemaParser.Parse(new SourceText("test.avsc", "{\"type\":\"record\",\"name\":\"R\",\"fields\":[]}"), Options, token));
    }

    [Fact]
    public void Large_avdl_scan_observes_cancellation_between_tokens()
    {
        using var source = new CancellationTokenSource();
        var text = string.Join(' ', Enumerable.Repeat("identifier", 10_000));
        using var tokens = new Scanner(new SourceText("large.avdl", text), source.Token).ScanAllTokens().GetEnumerator();

        Assert.True(tokens.MoveNext());
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() => tokens.MoveNext());
    }

    [Fact]
    public void Large_avdl_parse_honors_pre_cancelled_token_without_traversal()
    {
        var text = new StringBuilder("schema R; record R {");
        for (var index = 0; index < 10_000; index++)
            text.Append(" string f").Append(index).Append(';');
        text.Append(" }");

        Assert.Throws<OperationCanceledException>(() =>
            Parser.Parse(new SourceText("large.avdl", text.ToString()), CreateCancelledToken()));
    }

    [Fact]
    public void Large_avsc_semantic_input_honors_pre_cancelled_token_without_traversal()
    {
        var text = new StringBuilder("{\"type\":\"record\",\"name\":\"R\",\"fields\":[");
        for (var index = 0; index < 10_000; index++)
        {
            if (index > 0)
                text.Append(',');
            text.Append("{\"name\":\"f").Append(index).Append("\",\"type\":\"string\"}");
        }
        text.Append("]}");

        Assert.Throws<OperationCanceledException>(() =>
            AvscSchemaParser.Parse(new SourceText("large.avsc", text.ToString()), Options, CreateCancelledToken()));
    }

    private static CancellationToken CreateCancelledToken()
    {
        var source = new CancellationTokenSource();
        source.Cancel();
        return source.Token;
    }
}
