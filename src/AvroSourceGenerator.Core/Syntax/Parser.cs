using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Syntax.Annotations;
using AvroSourceGenerator.Syntax.Declarations;
using AvroSourceGenerator.Syntax.Directives;
using AvroSourceGenerator.Syntax.Json;
using AvroSourceGenerator.Syntax.Types;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Syntax;

internal sealed class Parser
{
    private const int MaxJsonDepth = 64;

    private readonly SourceText _sourceText;
    private readonly CancellationToken _cancellationToken;
    private readonly SyntaxTokenStream _stream;
    private readonly List<IAnnotationSyntax> _annotations = [];
    private readonly List<DocumentationSyntax> _documentation = [];
    private readonly List<AvroDiagnostic> _diagnostics = [];
    private int _jsonDepth;
    private bool _lastTokenWasSynthetic;

    public Parser(SourceText sourceText, CancellationToken cancellationToken, bool jsonOnly = false)
    {
        _sourceText = sourceText;
        _cancellationToken = cancellationToken;
        cancellationToken.ThrowIfCancellationRequested();
        var scanner = new Scanner(sourceText, cancellationToken, jsonOnly);
        _stream = new SyntaxTokenStream([.. scanner.ScanAllTokens()], cancellationToken);
        _diagnostics.AddRange(scanner.Diagnostics);
    }

    public static SyntaxTree<JsonValueSyntax> ParseJson(SourceText source, CancellationToken cancellationToken)
    {
        var parser = new Parser(source, cancellationToken, jsonOnly: true);
        if (parser._diagnostics.Count == 0 && parser._stream.IsAtEnd)
            parser._diagnostics.Add(AvroDiagnostic.EmptyJson(source.GetSourceSpan()));

        if (parser._diagnostics.Count > 0)
        {
            var placeholder = new JsonNullSyntax(new SyntaxToken(SyntaxKind.NullKeyword, source.GetSourceSpan(0, 0)));
            return new SyntaxTree<JsonValueSyntax>(source, placeholder, [.. parser._diagnostics]);
        }

        var root = parser.ParseJsonValue();
        if (parser._diagnostics.Count == 0 && !parser._stream.IsAtEnd)
            parser._diagnostics.Add(AvroDiagnostic.TrailingJson(parser._stream.Current.SourceSpan));

        return new SyntaxTree<JsonValueSyntax>(source, root, [.. parser._diagnostics]);
    }

    public static SyntaxTree<DocumentSyntax> ParseIdl(SourceText source, CancellationToken cancellationToken)
    {
        var parser = new Parser(source, cancellationToken);
        var root = parser._diagnostics.Count == 0 ? parser.ParseDocument() : new DocumentSyntax([], []);
        return new SyntaxTree<DocumentSyntax>(source, root, [.. parser._diagnostics]);
    }

    private DocumentSyntax ParseDocument()
    {
        var directives = ParseDirectives();
        var declarations = ParseDeclarations();
        return new DocumentSyntax(directives, declarations);
    }

    private SyntaxList<IDirectiveSyntax> ParseDirectives()
    {
        var directives = ImmutableArray.CreateBuilder<IDirectiveSyntax>();
        var hasNamespace = false;
        var hasSchema = false;
        while (_stream.Current.SyntaxKind is SyntaxKind.NamespaceKeyword or SyntaxKind.SchemaKeyword or SyntaxKind.ImportKeyword)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            switch (_stream.Current.SyntaxKind)
            {
                case SyntaxKind.NamespaceKeyword:
                    {
                        var directive = ParseNamespaceDirective();
                        if (hasNamespace)
                            _diagnostics.Add(AvroDiagnostic.DuplicateIdlDirective(directive.GetSourceSpan(), "namespace"));
                        hasNamespace = true;
                        directives.Add(directive);
                        break;
                    }
                case SyntaxKind.SchemaKeyword:
                    {
                        var directive = ParseSchemaDirective();
                        if (hasSchema)
                            _diagnostics.Add(AvroDiagnostic.DuplicateIdlDirective(directive.GetSourceSpan(), "schema"));
                        hasSchema = true;
                        directives.Add(directive);
                        break;
                    }
                default: directives.Add(ParseImportDirective()); break;
            }
        }

        return new SyntaxList<IDirectiveSyntax>(directives.DrainToImmutable());
    }

    private NamespaceDirectiveSyntax ParseNamespaceDirective()
    {
        var namespaceKeyword = Match(SyntaxKind.NamespaceKeyword);
        var namespaceName = ParseName();
        var semicolonToken = Match(SyntaxKind.SemicolonToken);
        return new NamespaceDirectiveSyntax(namespaceKeyword, namespaceName, semicolonToken);
    }

    private SchemaDirectiveSyntax ParseSchemaDirective()
    {
        var schemaKeyword = Match(SyntaxKind.SchemaKeyword);
        var mainSchemaType = ParseType();
        var semicolonToken = Match(SyntaxKind.SemicolonToken);
        return new SchemaDirectiveSyntax(schemaKeyword, mainSchemaType, semicolonToken);
    }

    private ImportDirectiveSyntax ParseImportDirective()
    {
        var importKeyword = Match(SyntaxKind.ImportKeyword);
        var importTypeKeyword = _stream.Current.SyntaxKind switch
        {
            SyntaxKind.ProtocolKeyword => Match(SyntaxKind.ProtocolKeyword),
            SyntaxKind.SchemaKeyword => Match(SyntaxKind.SchemaKeyword),
            _ => Match(SyntaxKind.IdlKeyword),
        };
        var importPathLiteralToken = Match(SyntaxKind.StringLiteralToken);
        var semicolonToken = Match(SyntaxKind.SemicolonToken);
        return new ImportDirectiveSyntax(importKeyword, importTypeKeyword, importPathLiteralToken, semicolonToken);
    }

    private SyntaxList<ITopLevelDeclarationSyntax> ParseDeclarations()
    {
        var declarations = ImmutableArray.CreateBuilder<ITopLevelDeclarationSyntax>();
        while (!_stream.IsAtEnd)
        {
            EnqueueMetadata();
            if (_stream.IsAtEnd)
            {
                break;
            }

            if (TrySkipUnexpectedBlock(SyntaxKind.ProtocolKeyword))
            {
                ReportAndClearMisplacedMetadata("document body");
                continue;
            }

            using (EnsureProgress())
            {
                declarations.Add(ParseDeclaration());
            }
        }

        return new SyntaxList<ITopLevelDeclarationSyntax>(declarations.DrainToImmutable());
    }

    private ITopLevelDeclarationSyntax ParseDeclaration()
    {
        return _stream.Current.SyntaxKind switch
        {
            SyntaxKind.EnumKeyword => ParseEnumDeclaration(),
            SyntaxKind.FixedKeyword => ParseFixedDeclaration(),
            SyntaxKind.RecordKeyword => ParseRecordDeclaration(),
            SyntaxKind.ErrorKeyword => ParseErrorDeclaration(),
            _ => ParseProtocolDeclaration(),
        };
    }

    private EnumDeclarationSyntax ParseEnumDeclaration()
    {
        var enumKeyword = Match(SyntaxKind.EnumKeyword);
        var name = ParseSimpleName();
        var (documentation, annotations) = DequeueMetadata();
        var braceOpenToken = Match(SyntaxKind.BraceOpenToken);
        var symbols = ParseSeparatedList(
            parseNode: ParseSimpleName,
            separator: SyntaxKind.CommaToken,
            terminator: SyntaxKind.BraceCloseToken,
            expectedElement: SyntaxKind.IdentifierToken);
        var braceCloseToken = Match(SyntaxKind.BraceCloseToken);
        var defaultValue = ParseEnumDefaultClause();
        var semicolonToken = defaultValue is not null ? Match(SyntaxKind.SemicolonToken) : null;
        return new EnumDeclarationSyntax(enumKeyword, name, documentation, annotations, braceOpenToken, symbols, braceCloseToken, defaultValue, semicolonToken);
    }

    private FixedDeclarationSyntax ParseFixedDeclaration()
    {
        var fixedKeyword = Match(SyntaxKind.FixedKeyword);
        var name = ParseSimpleName();
        var (documentation, annotations) = DequeueMetadata();
        var parenthesisOpenToken = Match(SyntaxKind.ParenthesisOpenToken);
        var sizeLiteralToken = Match(SyntaxKind.IntegerLiteralToken);
        var parenthesisCloseToken = Match(SyntaxKind.ParenthesisCloseToken);
        var semicolonToken = Match(SyntaxKind.SemicolonToken);
        return new FixedDeclarationSyntax(fixedKeyword, name, documentation, annotations, parenthesisOpenToken, sizeLiteralToken, parenthesisCloseToken, semicolonToken);
    }

    private RecordDeclarationSyntax ParseRecordDeclaration()
    {
        var recordKeyword = Match(SyntaxKind.RecordKeyword);
        var name = ParseSimpleName();
        var (documentation, annotations) = DequeueMetadata();
        var braceOpenToken = Match(SyntaxKind.BraceOpenToken);
        var fields = ParseList(
            parseNode: ParseFieldDeclaration,
            terminators: SyntaxKind.BraceCloseToken);
        var braceCloseToken = Match(SyntaxKind.BraceCloseToken);
        return new RecordDeclarationSyntax(recordKeyword, name, documentation, annotations, braceOpenToken, fields, braceCloseToken);
    }

    private ErrorDeclarationSyntax ParseErrorDeclaration()
    {
        var errorKeyword = Match(SyntaxKind.ErrorKeyword);
        var name = ParseSimpleName();
        var (documentation, annotations) = DequeueMetadata();
        var braceOpenToken = Match(SyntaxKind.BraceOpenToken);
        var fields = ParseList(
            parseNode: ParseFieldDeclaration,
            terminators: SyntaxKind.BraceCloseToken);
        var braceCloseToken = Match(SyntaxKind.BraceCloseToken);
        return new ErrorDeclarationSyntax(errorKeyword, name, documentation, annotations, braceOpenToken, fields, braceCloseToken);
    }

    private FieldDeclarationSyntax ParseFieldDeclaration()
    {
        EnqueueDocumentation();
        var type = ParseType();
        EnqueueMetadata();
        var name = ParseSimpleName();
        var (documentation, annotations) = DequeueMetadata();
        var defaultValue = ParseDefaultValueClause();
        var semicolonToken = Match(SyntaxKind.SemicolonToken);
        return new FieldDeclarationSyntax(type, name, documentation, annotations, defaultValue, semicolonToken);
    }

    private ProtocolDeclarationSyntax ParseProtocolDeclaration()
    {
        var protocolKeyword = Match(SyntaxKind.ProtocolKeyword);
        var name = ParseSimpleName();
        var (documentation, annotations) = DequeueMetadata();
        var braceOpenToken = Match(SyntaxKind.BraceOpenToken);
        var imports = ImmutableArray.CreateBuilder<ImportDirectiveSyntax>();
        var types = ImmutableArray.CreateBuilder<ISchemaDeclarationSyntax>();
        var messages = ImmutableArray.CreateBuilder<MessageDeclarationSyntax>();
        while (!_stream.IsAtEnd && _stream.Current.SyntaxKind != SyntaxKind.BraceCloseToken)
        {
            EnqueueMetadata();
            if (_stream.Current.SyntaxKind is SyntaxKind.BraceCloseToken)
            {
                ReportAndClearMisplacedMetadata("protocol body");
                break;
            }

            if (TrySkipUnexpectedBlock(SyntaxKind.IdentifierToken))
            {
                ReportAndClearMisplacedMetadata("protocol body");
                continue;
            }

            using (EnsureProgress())
            {
                switch (_stream.Current.SyntaxKind)
                {
                    case SyntaxKind.ImportKeyword:
                        ReportAndClearMisplacedMetadata("import directive");
                        imports.Add(ParseImportDirective());
                        break;
                    case SyntaxKind.EnumKeyword:
                        types.Add(ParseEnumDeclaration());
                        break;
                    case SyntaxKind.FixedKeyword:
                        types.Add(ParseFixedDeclaration());
                        break;
                    case SyntaxKind.RecordKeyword:
                        types.Add(ParseRecordDeclaration());
                        break;
                    case SyntaxKind.ErrorKeyword:
                        types.Add(ParseErrorDeclaration());
                        break;
                    default:
                        messages.Add(ParseMessageDeclaration());
                        break;
                }
            }
        }

        var braceCloseToken = Match(SyntaxKind.BraceCloseToken);

        return new ProtocolDeclarationSyntax(
            protocolKeyword,
            name,
            documentation,
            annotations,
            braceOpenToken,
            new SyntaxList<ImportDirectiveSyntax>(imports.DrainToImmutable()),
            new SyntaxList<ISchemaDeclarationSyntax>(types.DrainToImmutable()),
            new SyntaxList<MessageDeclarationSyntax>(messages.DrainToImmutable()),
            braceCloseToken);
    }

    private MessageDeclarationSyntax ParseMessageDeclaration()
    {
        var type = ParseType();
        EnqueueMetadata();
        var name = ParseSimpleName();
        var (documentation, annotations) = DequeueMetadata();
        var parenthesisOpenToken = Match(SyntaxKind.ParenthesisOpenToken);
        var parameters = ParseSeparatedList(ParseParameterDeclaration, SyntaxKind.CommaToken, SyntaxKind.ParenthesisCloseToken, SyntaxKind.IdentifierToken);
        var parenthesisCloseToken = Match(SyntaxKind.ParenthesisCloseToken);
        var oneWayClause = ParseOneWayClause();
        var throwsErrorClause = ParseThrowsErrorClause();
        var semicolonToken = Match(SyntaxKind.SemicolonToken);
        return new MessageDeclarationSyntax(
            type,
            name,
            documentation,
            annotations,
            parenthesisOpenToken,
            parameters,
            parenthesisCloseToken,
            oneWayClause,
            throwsErrorClause,
            semicolonToken);
    }

    private ParameterDeclarationSyntax ParseParameterDeclaration()
    {
        var type = ParseType();
        var name = ParseSimpleName();
        var (documentation, annotations) = DequeueMetadata();
        var defaultValue = ParseDefaultValueClause();
        return new ParameterDeclarationSyntax(type, name, documentation, annotations, defaultValue);
    }

    private OneWayClauseSyntax? ParseOneWayClause()
    {
        if (_stream.Current.SyntaxKind is not SyntaxKind.OneWayKeyword) return null;
        var oneWayKeyword = Next();
        return new OneWayClauseSyntax(oneWayKeyword);
    }

    private ThrowsErrorClauseSyntax? ParseThrowsErrorClause()
    {
        if (_stream.Current.SyntaxKind is not SyntaxKind.ThrowsKeyword) return null;
        var throwsKeyword = Next();
        var errors = ParseSeparatedList(ParseNamedType, SyntaxKind.CommaToken, SyntaxKind.SemicolonToken, SyntaxKind.IdentifierToken);
        return new ThrowsErrorClauseSyntax(throwsKeyword, errors);
    }

    private EnumDefaultClauseSyntax? ParseEnumDefaultClause()
    {
        if (_stream.Current.SyntaxKind != SyntaxKind.EqualsToken) return null;
        var equalsToken = Next();
        var symbol = _stream.Current.SyntaxKind switch
        {
            SyntaxKind.IdentifierToken or SyntaxKind.StringLiteralToken or SyntaxKind.NullKeyword
                or SyntaxKind.TrueKeyword or SyntaxKind.FalseKeyword or SyntaxKind.IntegerLiteralToken or SyntaxKind.FloatLiteralToken => Next(),
            _ => Match(SyntaxKind.IdentifierToken)
        };
        return new EnumDefaultClauseSyntax(equalsToken, symbol);
    }

    private DefaultValueClauseSyntax? ParseDefaultValueClause()
    {
        if (_stream.Current.SyntaxKind != SyntaxKind.EqualsToken) return null;
        var equalsToken = Match(SyntaxKind.EqualsToken);
        var json = ParseJsonValue();
        return new DefaultValueClauseSyntax(equalsToken, json);
    }

    private void EnqueueMetadata()
    {
        while (!_stream.IsAtEnd)
        {
            switch (_stream.Current.SyntaxKind)
            {
                case SyntaxKind.DocumentationTrivia:
                    _documentation.Add(ParseDocumentation());
                    break;
                case SyntaxKind.AtSignToken:
                    _annotations.Add(ParseAnnotation());
                    break;
                default:
                    return;
            }
        }
    }

    private void EnqueueDocumentation()
    {
        while (_stream.Current.SyntaxKind is SyntaxKind.DocumentationTrivia)
            _documentation.Add(ParseDocumentation());
    }

    private (SyntaxList<DocumentationSyntax> Documentation, SyntaxList<IAnnotationSyntax> Annotations) DequeueMetadata()
    {
        var documentation = new SyntaxList<DocumentationSyntax>([.. _documentation]);
        _documentation.Clear();
        var annotations = new SyntaxList<IAnnotationSyntax>([.. _annotations]);
        _annotations.Clear();
        return (documentation, annotations);
    }

    private DocumentationSyntax ParseDocumentation()
    {
        var documentationTrivia = Match(SyntaxKind.DocumentationTrivia);
        return new DocumentationSyntax(documentationTrivia);
    }

    private IAnnotationSyntax ParseAnnotation()
    {
        var atSignToken = Match(SyntaxKind.AtSignToken);
        var name = ParseAnnotationName();
        var parenthesisOpenToken = Match(SyntaxKind.ParenthesisOpenToken);
        var json = ParseJsonValue();
        var parenthesisCloseToken = Match(SyntaxKind.ParenthesisCloseToken);
        return name.FullName switch
        {
            "namespace" => new NamespaceAnnotationSyntax(atSignToken, name, parenthesisOpenToken, json, parenthesisCloseToken),
            "aliases" => new AliasesAnnotationSyntax(atSignToken, name, parenthesisOpenToken, json, parenthesisCloseToken),
            "order" => new OrderAnnotationSyntax(atSignToken, name, parenthesisOpenToken, json, parenthesisCloseToken),
            "logicalType" => new LogicalTypeAnnotationSyntax(atSignToken, name, parenthesisOpenToken, json, parenthesisCloseToken),

            _ => new CustomAnnotationSyntax(atSignToken, name, parenthesisOpenToken, json, parenthesisCloseToken),
        };
    }

    private AnnotationNameSyntax ParseAnnotationName()
    {
        var separatedIdentifiers = ParseSeparatedList(
            parseNode: () => _stream.Current.SyntaxKind is SyntaxKind.NamespaceKeyword ? Next() : Match(SyntaxKind.IdentifierToken),
            separator: SyntaxKind.DotToken,
            terminator: SyntaxKind.ParenthesisOpenToken);

        return new AnnotationNameSyntax(separatedIdentifiers);
    }

    public JsonValueSyntax ParseJsonValue()
    {
        _cancellationToken.ThrowIfCancellationRequested();

        return _stream.Current.SyntaxKind switch
        {
            SyntaxKind.NullKeyword => new JsonNullSyntax(Next()),
            SyntaxKind.TrueKeyword or SyntaxKind.FalseKeyword => new JsonBooleanSyntax(Next()),
            SyntaxKind.IntegerLiteralToken or SyntaxKind.FloatLiteralToken => new JsonNumberSyntax(ParseJsonNumber()),
            SyntaxKind.StringLiteralToken => new JsonStringSyntax(ParseJsonString()),
            SyntaxKind.BracketOpenToken => ParseJsonArray(),
            SyntaxKind.BraceOpenToken => ParseJsonObject(),
            _ => ParseUnexpectedJsonValue()
        };
    }

    private SyntaxToken ParseJsonNumber()
    {
        var literal = Next();
        var text = literal.SourceSpan.AsSpan();
        if (text[0] == '-') text = text[1..];

        var point = text.IndexOf('.');
        if (text is ['0', >= '0' and <= '9', ..] || point == 0 ||
            (point > 0 && text[(point + 1)..] is not [>= '0' and <= '9', ..]))
            _diagnostics.Add(AvroDiagnostic.InvalidNumber(literal.SourceSpan));

        return literal;
    }

    private SyntaxToken ParseJsonString(bool isPropertyName = false)
    {
        var literal = Match(SyntaxKind.StringLiteralToken);
        var text = literal.SourceSpan.AsSpan();
        var invalid = JsonStringSyntax.IndexOfInvalidCharacter(text, allowControlCharacters: false);
        if (invalid >= 0)
            _diagnostics.Add(AvroDiagnostic.InvalidCharacter(_sourceText.GetSourceSpan(literal.SourceSpan.Offset + invalid, 1)));
        else if (isPropertyName && JsonStringSyntax.IndexOfInvalidCharacter(literal.ValueText.AsSpan(), allowControlCharacters: true) >= 0)
            _diagnostics.Add(AvroDiagnostic.JsonPropertyNameUnpairedSurrogate(literal.SourceSpan));

        return literal;
    }

    private readonly ref struct JsonDepthGuard : IDisposable
    {
        private readonly Parser _parser;

        public JsonDepthGuard(Parser parser)
        {
            _parser = parser;
            _parser._jsonDepth++;
        }

        public void Dispose() => _parser._jsonDepth--;

        public bool IsAtMaxJsonDepth([MaybeNullWhen(false)] out JsonValueSyntax @null)
        {
            if (_parser._jsonDepth <= MaxJsonDepth)
            {
                @null = null;
                return false;
            }

            var open = _parser.Next();
            _parser._diagnostics.Add(AvroDiagnostic.JsonNestingLimitExceeded(open.SourceSpan, MaxJsonDepth));
            var depth = 1;
            while (depth > 0 && !_parser._stream.IsAtEnd)
            {
                switch (_parser.Next().SyntaxKind)
                {
                    case SyntaxKind.BracketOpenToken or SyntaxKind.BraceOpenToken:
                        depth++;
                        break;
                    case SyntaxKind.BracketCloseToken or SyntaxKind.BraceCloseToken:
                        depth--;
                        break;
                }
            }
            @null = new JsonNullSyntax(open);
            return true;
        }
    }

    private JsonDepthGuard IncreaseDepth() => new JsonDepthGuard(this);

    private JsonValueSyntax ParseJsonArray()
    {
        using var scope = IncreaseDepth();
        if (scope.IsAtMaxJsonDepth(out var @null)) return @null;
        var open = Match(SyntaxKind.BracketOpenToken);
        var elements = ParseSeparatedList(ParseJsonValue, SyntaxKind.CommaToken, SyntaxKind.BracketCloseToken);
        var close = Match(SyntaxKind.BracketCloseToken);
        return new JsonArraySyntax(open, [.. elements], close);
    }

    private JsonValueSyntax ParseJsonObject()
    {
        using var scope = IncreaseDepth();
        if (scope.IsAtMaxJsonDepth(out var @null)) return @null;
        var open = Match(SyntaxKind.BraceOpenToken);
        var members = ParseSeparatedList(ParseJsonProperty, SyntaxKind.CommaToken, SyntaxKind.BraceCloseToken, SyntaxKind.StringLiteralToken,
            static (parser, offset) => parser._stream.Peek(offset).SyntaxKind == SyntaxKind.CommaToken
                && parser._stream.Peek(offset + 1).SyntaxKind == SyntaxKind.StringLiteralToken
                && parser._stream.Peek(offset + 2).SyntaxKind == SyntaxKind.ColonToken);
        var close = Match(SyntaxKind.BraceCloseToken);
        return new JsonObjectSyntax(open, [.. members], close);
    }

    private JsonPropertySyntax ParseJsonProperty()
    {
        var name = ParseJsonString(isPropertyName: true);
        var colon = Match(SyntaxKind.ColonToken);
        return new JsonPropertySyntax(name, colon, ParseJsonValue());
    }

    private JsonNullSyntax ParseUnexpectedJsonValue()
    {
        var token = _stream.Current;
        if (!_lastTokenWasSynthetic)
            _diagnostics.Add(AvroDiagnostic.UnexpectedJsonValue(token));

        if (GetClosingDelimiter(token.SyntaxKind) is { } closing)
        {
            SkipBlock(closing);
            return new JsonNullSyntax(token);
        }

        if (token.SyntaxKind is SyntaxKind.CommaToken or SyntaxKind.BracketCloseToken or SyntaxKind.BraceCloseToken
            or SyntaxKind.ParenthesisCloseToken or SyntaxKind.SemicolonToken or SyntaxKind.EofToken)
        {
            _lastTokenWasSynthetic = true;
            return new JsonNullSyntax(new SyntaxToken(SyntaxKind.NullKeyword, _sourceText.GetSourceSpan(token.SourceSpan.Offset, 0)));
        }

        return new JsonNullSyntax(Next());
    }

    private void ReportAndClearMisplacedMetadata(string target)
    {
        foreach (var documentation in _documentation)
            _diagnostics.Add(AvroDiagnostic.MisplacedDocumentation(documentation.DocumentationTrivia.SourceSpan, target));
        foreach (var annotation in _annotations)
            _diagnostics.Add(AvroDiagnostic.MisplacedAnnotation(GetAnnotationSpan(annotation), GetAnnotationName(annotation), target));

        _documentation.Clear();
        _annotations.Clear();
    }

    private static SourceSpan GetAnnotationSpan(IAnnotationSyntax annotation) =>
        annotation switch
        {
            NamespaceAnnotationSyntax namespaceAnnotation => namespaceAnnotation.AtSignToken.SourceSpan,
            AliasesAnnotationSyntax aliasesAnnotation => aliasesAnnotation.AtSignToken.SourceSpan,
            OrderAnnotationSyntax orderAnnotation => orderAnnotation.AtSignToken.SourceSpan,
            LogicalTypeAnnotationSyntax logicalTypeAnnotation => logicalTypeAnnotation.AtSignToken.SourceSpan,
            CustomAnnotationSyntax customAnnotation => customAnnotation.AtSignToken.SourceSpan,
            _ => annotation.Children().OfType<SyntaxToken>().First().SourceSpan,
        };

    private static string GetAnnotationName(IAnnotationSyntax annotation) =>
        annotation.AnnotationName.FullName;

    private ITypeSyntax ParseType()
    {
        SyntaxList<IAnnotationSyntax> annotations = [];
        if (_stream.Current.SyntaxKind is SyntaxKind.AtSignToken)
        {
            var builder = ImmutableArray.CreateBuilder<IAnnotationSyntax>();
            while (_stream.Current.SyntaxKind is SyntaxKind.AtSignToken)
                builder.Add(ParseAnnotation());
            annotations = new SyntaxList<IAnnotationSyntax>(builder.DrainToImmutable());
        }

        ITypeSyntax type = _stream.Current.SyntaxKind switch
        {
            SyntaxKind.VoidKeyword
                or SyntaxKind.NullKeyword
                or SyntaxKind.IntKeyword
                or SyntaxKind.LongKeyword
                or SyntaxKind.StringKeyword
                or SyntaxKind.BooleanKeyword
                or SyntaxKind.FloatKeyword
                or SyntaxKind.DoubleKeyword
                or SyntaxKind.BytesKeyword => new PrimitiveTypeSyntax(Next()),

            SyntaxKind.ArrayKeyword => ParseArrayType(),
            SyntaxKind.MapKeyword => ParseMapType(),
            SyntaxKind.UnionKeyword => ParseUnionType(),

            SyntaxKind.DecimalKeyword => ParseDecimalLogicalType(),
            SyntaxKind.DateKeyword
                or SyntaxKind.TimeMsKeyword
                or SyntaxKind.TimestampMsKeyword
                or SyntaxKind.LocalTimestampMsKeyword
                or SyntaxKind.UuidKeyword => new LogicalTypeSyntax(Next()),

            _ => ParseNamedType(),
        };

        if (type is NamedTypeSyntax && !_lastTokenWasSynthetic)
        {
            foreach (var annotation in annotations)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                _diagnostics.Add(AvroDiagnostic.MisplacedAnnotation(
                    GetAnnotationSpan(annotation), GetAnnotationName(annotation), "a named type reference"));
            }
        }

        if (_stream.Current.SyntaxKind is SyntaxKind.QuestionMarkToken)
        {
            type = new OptionalTypeSyntax(type, Next());
        }

        return annotations.Count > 0 ? new AnnotatedTypeSyntax(annotations, type) : type;
    }

    private ArrayTypeSyntax ParseArrayType()
    {
        var arrayKeyword = Match(SyntaxKind.ArrayKeyword);
        var lessThanToken = Match(SyntaxKind.LessThanToken);
        var elementType = ParseType();
        var greaterThanToken = Match(SyntaxKind.GreaterThanToken);
        return new ArrayTypeSyntax(arrayKeyword, lessThanToken, elementType, greaterThanToken);
    }

    private MapTypeSyntax ParseMapType()
    {
        var mapKeyword = Match(SyntaxKind.MapKeyword);
        var lessThanToken = Match(SyntaxKind.LessThanToken);
        var valueType = ParseType();
        var greaterThanToken = Match(SyntaxKind.GreaterThanToken);
        return new MapTypeSyntax(mapKeyword, lessThanToken, valueType, greaterThanToken);
    }

    private UnionTypeSyntax ParseUnionType()
    {
        var unionKeyword = Match(SyntaxKind.UnionKeyword);
        var braceOpenToken = Match(SyntaxKind.BraceOpenToken);
        var types = ParseSeparatedList(ParseType, SyntaxKind.CommaToken, SyntaxKind.BraceCloseToken, SyntaxKind.IdentifierToken);
        var braceCloseToken = Match(SyntaxKind.BraceCloseToken);
        return new UnionTypeSyntax(unionKeyword, braceOpenToken, types, braceCloseToken);
    }

    private DecimalLogicalTypeSyntax ParseDecimalLogicalType()
    {
        var decimalKeyword = Match(SyntaxKind.DecimalKeyword);
        var parenthesisOpenToken = Match(SyntaxKind.ParenthesisOpenToken);
        var precisionLiteralToken = ParseDecimalArgument();
        var commaToken = Match(SyntaxKind.CommaToken);
        var scaleLiteralToken = ParseDecimalArgument();
        var parenthesisCloseToken = Match(SyntaxKind.ParenthesisCloseToken);
        return new DecimalLogicalTypeSyntax(
            decimalKeyword,
            parenthesisOpenToken,
            precisionLiteralToken,
            commaToken,
            scaleLiteralToken,
            parenthesisCloseToken);
    }

    private SyntaxToken ParseDecimalArgument() => _stream.Current.SyntaxKind switch
    {
        SyntaxKind.FloatLiteralToken or SyntaxKind.StringLiteralToken or SyntaxKind.TrueKeyword or SyntaxKind.FalseKeyword or SyntaxKind.NullKeyword => Next(),
        _ => Match(SyntaxKind.IntegerLiteralToken)
    };

    private NamedTypeSyntax ParseNamedType()
    {
        var name = ParseName();
        return new NamedTypeSyntax(name);
    }

    private INameSyntax ParseName() => _stream.Peek(1).SyntaxKind == SyntaxKind.DotToken
        ? ParseQualifiedName()
        : ParseSimpleName();

    private SimpleNameSyntax ParseSimpleName()
    {
        var identifierToken = Match(SyntaxKind.IdentifierToken);
        return new SimpleNameSyntax(identifierToken);
    }

    private QualifiedNameSyntax ParseQualifiedName()
    {
        var separatedIdentifiers = ParseSeparatedList(
            parseNode: () => Match(SyntaxKind.IdentifierToken),
            separator: SyntaxKind.DotToken,
            terminator: SyntaxKind.SemicolonToken);

        return new QualifiedNameSyntax(separatedIdentifiers);
    }

    private SyntaxList<T> ParseList<T>(Func<T> parseNode, params ReadOnlySpan<SyntaxKind> terminators) where T : ISyntaxNode
    {
        var nodes = ImmutableArray.CreateBuilder<T>();
        while (!_stream.IsAtEnd && !terminators.Contains(_stream.Current.SyntaxKind))
        {
            if (TrySkipUnexpectedBlock(SyntaxKind.IdentifierToken))
            {
                if (IsClosingDelimiter(_stream.Current.SyntaxKind) || _stream.Current.SyntaxKind is
                    SyntaxKind.RecordKeyword or SyntaxKind.ErrorKeyword or SyntaxKind.EnumKeyword or SyntaxKind.FixedKeyword or SyntaxKind.ProtocolKeyword)
                    break;

                continue;
            }

            using (EnsureProgress(terminators))
            {
                nodes.Add(parseNode());
            }
        }

        return new SyntaxList<T>(nodes.DrainToImmutable());
    }

    private SeparatedSyntaxList<T> ParseSeparatedList<T>(
        Func<T> parseNode, SyntaxKind separator, SyntaxKind terminator, SyntaxKind? expectedElement = null,
        Func<Parser, int, bool>? canResumeAtSeparator = null) where T : ISyntaxNode
    {
        var nodes = ImmutableArray.CreateBuilder<ISyntaxNode>();

        while (!_stream.IsAtEnd && _stream.Current.SyntaxKind != terminator)
        {
            if (expectedElement is { } expected && TrySkipUnexpectedBlock(expected, terminator, canResumeAtSeparator))
            {
                // Leave an ambiguous separator for the enclosing list to handle.
                if (_stream.Current.SyntaxKind != separator || canResumeAtSeparator?.Invoke(this, 0) == false)
                    break;

                Next();
                continue;
            }

            nodes.Add(parseNode());
            if (_stream.Current.SyntaxKind != separator) break;
            var separatorAlreadyReported = _lastTokenWasSynthetic;
            var separatorToken = Next();
            nodes.Add(separatorToken);
            if (!separatorAlreadyReported && separator == SyntaxKind.CommaToken && _stream.Current.SyntaxKind == terminator)
                ReportUnexpectedToken(_stream.Current.SyntaxKind, separatorToken);
        }

        return new SeparatedSyntaxList<T>(nodes.DrainToImmutable());
    }

    private bool TrySkipUnexpectedBlock(SyntaxKind expected, SyntaxKind? enclosingClose = null, Func<Parser, int, bool>? canResumeAtSeparator = null)
    {
        if (GetClosingDelimiter(_stream.Current.SyntaxKind) is not { } closing)
            return false;

        ReportUnexpectedToken(expected, _stream.Current);
        SkipBlock(closing, enclosingClose, canResumeAtSeparator);
        return true;
    }

    private void SkipBlock(SyntaxKind closing, SyntaxKind? enclosingClose = null, Func<Parser, int, bool>? canResumeAtSeparator = null)
    {
        Next();
        var closers = new Stack<SyntaxKind>();
        closers.Push(closing);
        while (!_stream.IsAtEnd)
        {
            var kind = _stream.Current.SyntaxKind;
            if (GetClosingDelimiter(kind) is { } nestedClosing)
                closers.Push(nestedClosing);
            else if (IsClosingDelimiter(kind))
            {
                if (closers.Peek() != kind)
                    break;

                // Give an ambiguous closer to the enclosing list unless a valid sibling follows it.
                if (closers.Count == 1 && kind == enclosingClose && canResumeAtSeparator?.Invoke(this, 1) == false)
                    break;

                closers.Pop();
            }

            Next();
            if (closers.Count == 0)
                break;
        }

        // A missing token immediately after the skipped block belongs to the same recovery.
        _lastTokenWasSynthetic = true;
    }

    private static SyntaxKind? GetClosingDelimiter(SyntaxKind kind) => kind switch
    {
        SyntaxKind.BraceOpenToken => SyntaxKind.BraceCloseToken,
        SyntaxKind.BracketOpenToken => SyntaxKind.BracketCloseToken,
        SyntaxKind.ParenthesisOpenToken => SyntaxKind.ParenthesisCloseToken,
        SyntaxKind.LessThanToken => SyntaxKind.GreaterThanToken,
        _ => null,
    };

    private static bool IsClosingDelimiter(SyntaxKind kind) =>
        kind is SyntaxKind.BraceCloseToken or SyntaxKind.BracketCloseToken or SyntaxKind.ParenthesisCloseToken or SyntaxKind.GreaterThanToken;

    private SyntaxToken Match(SyntaxKind syntaxKind)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        // Skip documentation trivia if we're not trying to match it. This allows us to ignore documentation comments in
        // places where they are not expected without causing syntax errors, while still allowing us to capture them when we do want them.
        while (syntaxKind != SyntaxKind.DocumentationTrivia && _stream.Current.SyntaxKind == SyntaxKind.DocumentationTrivia)
        {
            Next();
        }

        if (_stream.Current.SyntaxKind == syntaxKind)
        {
            return Next();
        }

        if (CanSkipCurrentToMatch(syntaxKind))
        {
            ReportUnexpectedToken(syntaxKind, _stream.Current);
            _ = Next();
            return Next();
        }

        return CreateSynthetic(syntaxKind);
    }

    private SyntaxToken CreateSynthetic(SyntaxKind expectedSyntaxKind)
    {
        ReportUnexpectedToken(expectedSyntaxKind, _stream.Current);
        _lastTokenWasSynthetic = true;
        return new SyntaxToken(expectedSyntaxKind, new SourceSpan(_sourceText, _stream.Current.SourceSpan.Offset, 0));
    }

    private void ReportUnexpectedToken(SyntaxKind expectedSyntaxKind, SyntaxToken actual)
    {
        if (!_lastTokenWasSynthetic)
            _diagnostics.Add(AvroDiagnostic.UnexpectedToken(expectedSyntaxKind, actual));
    }

    private bool CanSkipCurrentToMatch(SyntaxKind syntaxKind) =>
        !_stream.IsAtEnd
        && _stream.Peek(1).SyntaxKind == syntaxKind
        && !IsStructuralRecoveryAnchor(_stream.Current.SyntaxKind);

    private static bool IsStructuralRecoveryAnchor(SyntaxKind syntaxKind) =>
        syntaxKind is
            SyntaxKind.EofToken
            or SyntaxKind.BraceOpenToken
            or SyntaxKind.BraceCloseToken
            or SyntaxKind.ParenthesisOpenToken
            or SyntaxKind.ParenthesisCloseToken
            or SyntaxKind.BracketOpenToken
            or SyntaxKind.BracketCloseToken
            or SyntaxKind.LessThanToken
            or SyntaxKind.GreaterThanToken
            or SyntaxKind.AtSignToken
            or SyntaxKind.CommaToken
            or SyntaxKind.DotToken
            or SyntaxKind.ColonToken
            or SyntaxKind.SemicolonToken
            or SyntaxKind.EqualsToken
            or SyntaxKind.QuestionMarkToken;

    private SyntaxToken Next()
    {
        var token = _stream.Next();
        _lastTokenWasSynthetic = false;
        return token;
    }

    private ProgressTracker EnsureProgress(ReadOnlySpan<SyntaxKind> terminators = default) => new ProgressTracker(this, terminators);

    private readonly ref struct ProgressTracker(Parser parser, ReadOnlySpan<SyntaxKind> terminators)
    {
        private readonly int _startPosition = parser._stream.Position;
        private readonly ReadOnlySpan<SyntaxKind> _terminators = terminators;

        public void Dispose()
        {
            if (parser._stream.Position != _startPosition || parser._stream.IsAtEnd || _terminators.Contains(parser._stream.Current.SyntaxKind))
                return;

            _ = parser.Next();
        }
    }
}

file static class SpanExtensions
{
    extension(ReadOnlySpan<SyntaxKind> span)
    {
        public bool Contains(SyntaxKind syntaxKind)
        {
            foreach (var item in span)
            {
                if (item == syntaxKind) return true;
            }

            return false;
        }
    }
}
