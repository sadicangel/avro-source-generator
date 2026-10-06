using AvroSourceGenerator.Avdl;
using AvroSourceGenerator.Diagnostics;

namespace AvroSourceGenerator.UnitTests.Avdl;

public sealed class ScannerTests
{
    public static TheoryData<string, SyntaxKind> Punctuation =>
        new TheoryData<string, SyntaxKind>
        {
            { "{", SyntaxKind.BraceOpenToken },
            { "}", SyntaxKind.BraceCloseToken },
            { "(", SyntaxKind.ParenthesisOpenToken },
            { ")", SyntaxKind.ParenthesisCloseToken },
            { "[", SyntaxKind.BracketOpenToken },
            { "]", SyntaxKind.BracketCloseToken },
            { "<", SyntaxKind.LessThanToken },
            { ">", SyntaxKind.GreaterThanToken },
            { "@", SyntaxKind.AtSignToken },
            { ",", SyntaxKind.CommaToken },
            { ".", SyntaxKind.DotToken },
            { ":", SyntaxKind.ColonToken },
            { ";", SyntaxKind.SemicolonToken },
            { "=", SyntaxKind.EqualsToken },
            { "?", SyntaxKind.QuestionMarkToken },
        };

    public static TheoryData<string, SyntaxKind> Keywords =>
        new TheoryData<string, SyntaxKind>
        {
            { "true", SyntaxKind.TrueKeyword },
            { "false", SyntaxKind.FalseKeyword },
            { "void", SyntaxKind.VoidKeyword },
            { "null", SyntaxKind.NullKeyword },
            { "int", SyntaxKind.IntKeyword },
            { "long", SyntaxKind.LongKeyword },
            { "string", SyntaxKind.StringKeyword },
            { "boolean", SyntaxKind.BooleanKeyword },
            { "float", SyntaxKind.FloatKeyword },
            { "double", SyntaxKind.DoubleKeyword },
            { "bytes", SyntaxKind.BytesKeyword },
            { "array", SyntaxKind.ArrayKeyword },
            { "map", SyntaxKind.MapKeyword },
            { "union", SyntaxKind.UnionKeyword },
            { "enum", SyntaxKind.EnumKeyword },
            { "fixed", SyntaxKind.FixedKeyword },
            { "record", SyntaxKind.RecordKeyword },
            { "error", SyntaxKind.ErrorKeyword },
            { "protocol", SyntaxKind.ProtocolKeyword },
            { "namespace", SyntaxKind.NamespaceKeyword },
            { "schema", SyntaxKind.SchemaKeyword },
            { "import", SyntaxKind.ImportKeyword },
            { "idl", SyntaxKind.IdlKeyword },
            { "typedef", SyntaxKind.TypedefKeyword },
            { "decimal", SyntaxKind.DecimalKeyword },
            { "date", SyntaxKind.DateKeyword },
            { "time_ms", SyntaxKind.TimeMsKeyword },
            { "timestamp_ms", SyntaxKind.TimestampMsKeyword },
            { "local_timestamp_ms", SyntaxKind.LocalTimestampMsKeyword },
            { "uuid", SyntaxKind.UuidKeyword },
            { "throws", SyntaxKind.ThrowsKeyword },
            { "oneway", SyntaxKind.OneWayKeyword },
        };

    public static TheoryData<string, object> IntegerLiterals =>
        new TheoryData<string, object>
        {
            { "0", 0 },
            { "42", 42 },
            { "-1", -1 },
            { "2147483648", 2147483648L },
        };

    public static TheoryData<string, double> FloatLiterals =>
        new TheoryData<string, double>
        {
            { "0.5", 0.5D },
            { ".5", 0.5D },
            { "-0.5", -0.5D },
            { "1e2", 100D },
            { "1e+2", 100D },
            { "1E-2", 0.01D },
        };

    public static TheoryData<string> InvalidInputs =>
    [
        "$",
        "\"unterminated",
        "\"bad\\q\"",
        "/** unterminated",
        "`unterminated",
        "1e"
    ];

    public static TheoryData<string, AvroDiagnosticCode, int, int> InvalidInputDiagnostics =>
        new TheoryData<string, AvroDiagnosticCode, int, int>
        {
            { "$", AvroDiagnosticCode.InvalidCharacter, 0, 1 },
            { "\"bad\\q\"", AvroDiagnosticCode.InvalidEscapeSequence, 4, 2 },
            { "1e", AvroDiagnosticCode.InvalidNumber, 0, 2 },
            { "\"unterminated", AvroDiagnosticCode.UnterminatedString, 0, 13 },
            { "/** unterminated", AvroDiagnosticCode.UnterminatedDocumentation, 0, 16 },
            { "/* unterminated", AvroDiagnosticCode.UnterminatedComment, 0, 15 },
            { "`unterminated", AvroDiagnosticCode.UnterminatedVerbatimIdentifier, 0, 13 },
        };

    [Theory]
    [MemberData(nameof(Punctuation))]
    public void Scan_ReturnsPunctuationToken(string text, SyntaxKind expected)
    {
        var scanner = CreateScanner(text);

        var tokens = scanner.ScanAllTokens().ToArray();

        Assert.Empty(scanner.BadTokens);
        Assert.Equal(expected, tokens[0].SyntaxKind);
        Assert.Equal(SyntaxKind.EofToken, tokens[1].SyntaxKind);
    }

    [Theory]
    [MemberData(nameof(Keywords))]
    public void Scan_ReturnsKeywordToken(string text, SyntaxKind expected)
    {
        var scanner = CreateScanner(text);

        var tokens = scanner.ScanAllTokens().ToArray();

        Assert.Empty(scanner.BadTokens);
        Assert.Equal(expected, tokens[0].SyntaxKind);
        Assert.Equal(text, tokens[0].SourceSpan.ToString());
    }

    [Fact]
    public void Scan_IdentifierAtEof_ReturnsIdentifier()
    {
        var scanner = CreateScanner("name_1");

        var tokens = scanner.ScanAllTokens().ToArray();

        Assert.Empty(scanner.BadTokens);
        Assert.Equal(SyntaxKind.IdentifierToken, tokens[0].SyntaxKind);
        Assert.Equal("name_1", tokens[0].SourceSpan.ToString());
        Assert.Equal("name_1", tokens[0].ValueText);
    }

    [Fact]
    public void Scan_VerbatimIdentifier_ReturnsIdentifierWithValueTextWithoutBackticks()
    {
        var scanner = CreateScanner("`error`");

        var tokens = scanner.ScanAllTokens().ToArray();

        Assert.Empty(scanner.BadTokens);
        Assert.Equal(SyntaxKind.IdentifierToken, tokens[0].SyntaxKind);
        Assert.Equal("`error`", tokens[0].SourceSpan.ToString());
        Assert.Equal("error", tokens[0].Value);
        Assert.Equal("error", tokens[0].ValueText);
    }

    [Fact]
    public void Scan_AnnotationIdentifier_AllowsDash()
    {
        var scanner = CreateScanner("@java-class(true)");

        var tokens = scanner.ScanAllTokens().ToArray();

        Assert.Empty(scanner.BadTokens);
        Assert.Equal(SyntaxKind.AtSignToken, tokens[0].SyntaxKind);
        Assert.Equal(SyntaxKind.IdentifierToken, tokens[1].SyntaxKind);
        Assert.Equal("java-class", tokens[1].SourceSpan.ToString());
        Assert.Equal("java-class", tokens[1].ValueText);
    }

    [Fact]
    public void Scan_Identifier_DoesNotAllowDashOutsideAnnotationName()
    {
        var scanner = CreateScanner("java-class");

        var tokens = scanner.ScanAllTokens().ToArray();

        var item = Assert.Single(scanner.BadTokens);
        Assert.Equal("-", item.SourceSpan.ToString());
        Assert.Equal(SyntaxKind.IdentifierToken, tokens[0].SyntaxKind);
        Assert.Equal("java", tokens[0].ValueText);
        Assert.Equal(SyntaxKind.IdentifierToken, tokens[1].SyntaxKind);
        Assert.Equal("class", tokens[1].ValueText);
    }

    [Fact]
    public void Scan_SkipsWhitespaceAndCommentsAtEof()
    {
        var scanner = CreateScanner("  \t // comment");

        var tokens = scanner.ScanAllTokens().ToArray();

        Assert.Empty(scanner.BadTokens);
        var item = Assert.Single(tokens);
        Assert.Equal(SyntaxKind.EofToken, item.SyntaxKind);
    }

    [Fact]
    public void Scan_DocumentationComment_ReturnsDocumentationTrivia()
    {
        var scanner = CreateScanner("/** Documentation */ record");

        var tokens = scanner.ScanAllTokens().ToArray();

        Assert.Empty(scanner.BadTokens);
        Assert.Equal(SyntaxKind.DocumentationTrivia, tokens[0].SyntaxKind);
        Assert.Equal(" Documentation ", tokens[0].SourceSpan.ToString());
        Assert.Equal(SyntaxKind.RecordKeyword, tokens[1].SyntaxKind);
    }

    [Fact]
    public void Scan_StringLiteral_DecodesJsonEscapes()
    {
        var scanner = CreateScanner("\"a\\n\\t\\\"\\\\\\/\\u0041\"");

        var tokens = scanner.ScanAllTokens().ToArray();

        Assert.Empty(scanner.BadTokens);
        Assert.Equal(SyntaxKind.StringLiteralToken, tokens[0].SyntaxKind);
        Assert.Equal("a\n\t\"\\/A", tokens[0].Value);
        Assert.Equal("a\n\t\"\\/A", tokens[0].ValueText);
    }

    [Theory]
    [MemberData(nameof(IntegerLiterals))]
    public void Scan_IntegerLiteral_ReturnsIntegerValue(string text, object expected)
    {
        var scanner = CreateScanner(text);

        var tokens = scanner.ScanAllTokens().ToArray();

        Assert.Empty(scanner.BadTokens);
        Assert.Equal(SyntaxKind.IntegerLiteralToken, tokens[0].SyntaxKind);
        Assert.Equal(expected, tokens[0].Value);
    }

    [Theory]
    [MemberData(nameof(FloatLiterals))]
    public void Scan_FloatLiteral_ReturnsDoubleValue(string text, double expected)
    {
        var scanner = CreateScanner(text);

        var tokens = scanner.ScanAllTokens().ToArray();

        Assert.Empty(scanner.BadTokens);
        Assert.Equal(SyntaxKind.FloatLiteralToken, tokens[0].SyntaxKind);
        Assert.Equal(expected, Assert.IsType<double>(tokens[0].Value), precision: 10);
    }

    [Theory]
    [MemberData(nameof(InvalidInputs))]
    public void Scan_InvalidInput_RecordsBadTokenAndContinuesToEof(string text)
    {
        var scanner = CreateScanner(text);

        var tokens = scanner.ScanAllTokens().ToArray();

        var item = Assert.Single(scanner.BadTokens);
        Assert.Equal(SyntaxKind.InvalidSyntax, item.SyntaxKind);
        Assert.Equal(SyntaxKind.EofToken, Assert.Single(tokens).SyntaxKind);
    }

    [Theory]
    [MemberData(nameof(InvalidInputDiagnostics))]
    public void Scan_InvalidInput_RecordsDiagnostic(string text, AvroDiagnosticCode expectedCode, int expectedOffset, int expectedLength)
    {
        var scanner = CreateScanner(text);

        _ = scanner.ScanAllTokens().ToArray();

        var diagnostic = Assert.Single(scanner.Diagnostics);
        Assert.Equal(expectedCode, diagnostic.Code);
        Assert.Equal(expectedOffset, diagnostic.SourceSpan.Offset);
        Assert.Equal(expectedLength, diagnostic.SourceSpan.Length);
    }

    [Fact]
    public void SyntaxFacts_GetText_HandlesEverySyntaxKind()
    {
        foreach (var syntaxKind in Enum.GetValues<SyntaxKind>())
            _ = SyntaxFacts.GetText(syntaxKind);
    }

    [Theory]
    [InlineData("\"\"", "")]
    [InlineData("\"Olá 世界 😀\"", "Olá 世界 😀")]
    // Deliberate escaped text exercises scanner decoding and error locations.
    // ReSharper disable StringLiteralTypo
    [InlineData("\"before\\nafter\\tmiddle\"", "before\nafter\tmiddle")]
    // ReSharper restore StringLiteralTypo
    [InlineData("\"\\b\\f\\r\"", "\b\f\r")]
    [InlineData("\"a\\uD83D\\uDE00z\"", "a😀z")]
    public void Scan_strings_preserves_content_and_source_span(string text, string expected)
    {
        var scanner = CreateScanner(text);
        var tokens = scanner.ScanAllTokens().ToArray();
        Assert.Empty(scanner.Diagnostics);
        Assert.Empty(scanner.BadTokens);
        Assert.Equal(SyntaxKind.StringLiteralToken, tokens[0].SyntaxKind);
        Assert.Equal(expected, tokens[0].Value);
        Assert.Equal(text, tokens[0].SourceSpan.ToString());
        Assert.Equal(0, tokens[0].SourceSpan.Offset);
        Assert.Equal(text.Length, tokens[0].SourceSpan.Length);
    }

    [Theory]
    [InlineData("\"prefix\\u12xz\"", "\\u12xz")]
    // Deliberate escaped text exercises scanner decoding and error locations.
    // ReSharper disable once StringLiteralTypo
    [InlineData("\"prefix\\ntext\\q\"", "\\q")]
    [InlineData("\"é😀\\q\"", "\\q")]
    [InlineData("\"prefix\\u12\"", "\\u12")]
    [InlineData("\"prefix\\u", "\\u")]
    [InlineData("\"prefix\\", "\\")]
    public void Scan_invalid_escapes_reports_only_the_escape_and_recovers_the_full_string(string text, string escape)
    {
        var scanner = CreateScanner(text);
        var tokens = scanner.ScanAllTokens().ToArray();
        var diagnostic = Assert.Single(scanner.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.InvalidEscapeSequence, diagnostic.Code);
        Assert.Equal(text.IndexOf(escape, StringComparison.Ordinal), diagnostic.SourceSpan.Offset);
        Assert.Equal(escape.Length, diagnostic.SourceSpan.Length);
        Assert.Equal(escape, diagnostic.SourceSpan.ToString());
        Assert.Equal($"Invalid escape sequence '{escape}'", diagnostic.GetMessage());
        Assert.Equal(text, Assert.Single(scanner.BadTokens).SourceSpan.ToString());
        Assert.Equal(SyntaxKind.EofToken, Assert.Single(tokens).SyntaxKind);
    }

    [Fact]
    public void Scan_unterminated_strings_keeps_the_full_diagnostic_span()
    {
        // Deliberate unterminated escaped text exercises the full diagnostic span.
        // ReSharper disable once StringLiteralTypo
        const string Text = "\"prefix\\ntext";
        var scanner = CreateScanner(Text);
        _ = scanner.ScanAllTokens().ToArray();
        var diagnostic = Assert.Single(scanner.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.UnterminatedString, diagnostic.Code);
        Assert.Equal(0, diagnostic.SourceSpan.Offset);
        Assert.Equal(Text.Length, diagnostic.SourceSpan.Length);
    }

    private static Scanner CreateScanner(string text) => new Scanner(AvdlTestHelpers.SourceText(text), TestContext.Current.CancellationToken);
}
