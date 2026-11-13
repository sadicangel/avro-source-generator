using System.Collections.Immutable;
using System.Text.Json.Nodes;
using AvroSourceGenerator.Avdl.Annotations;
using AvroSourceGenerator.Avdl.Declarations;
using AvroSourceGenerator.Avdl.Directives;
using AvroSourceGenerator.Avdl.Types;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Avdl;

internal sealed class Parser
{
    private readonly SourceText _sourceText;
    private readonly CancellationToken _cancellationToken;
    private readonly SyntaxTokenStream _stream;
    private readonly List<IAnnotationSyntax> _annotations = [];
    private readonly List<DocumentationSyntax> _documentation = [];
    private readonly List<AvroDiagnostic> _diagnostics = [];
    private bool _lastTokenWasSynthetic;

    public Parser(SourceText sourceText, CancellationToken cancellationToken)
    {
        _sourceText = sourceText;
        _cancellationToken = cancellationToken;
        cancellationToken.ThrowIfCancellationRequested();
        var scanner = new Scanner(sourceText, cancellationToken);
        _stream = new SyntaxTokenStream([.. scanner.ScanAllTokens()], cancellationToken);
        _diagnostics.AddRange(scanner.Diagnostics);
    }

    public static SyntaxTree Parse(SourceText sourceText, CancellationToken cancellationToken) => new Parser(sourceText, cancellationToken).Parse();

    public SyntaxTree Parse()
    {
        var document = ParseDocument();
        return new SyntaxTree(_sourceText, document, [.. _diagnostics]);
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
            terminators: SyntaxKind.BraceCloseToken);
        var braceCloseToken = Match(SyntaxKind.BraceCloseToken);
        var defaultValue = ParseDefaultValueClause();
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
        var parameters = ParseSeparatedList(ParseParameterDeclaration, SyntaxKind.CommaToken, SyntaxKind.ParenthesisCloseToken);
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
        var errors = ParseSeparatedList(ParseNamedType, SyntaxKind.CommaToken, SyntaxKind.SemicolonToken);
        return new ThrowsErrorClauseSyntax(throwsKeyword, errors);
    }

    private DefaultValueClauseSyntax? ParseDefaultValueClause()
    {
        if (_stream.Current.SyntaxKind != SyntaxKind.EqualsToken) return null;
        var equalsToken = Match(SyntaxKind.EqualsToken);
        var jsonValue = ParseJsonValue();
        return new DefaultValueClauseSyntax(equalsToken, jsonValue);
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
        var jsonValue = ParseJsonValue();
        var parenthesisCloseToken = Match(SyntaxKind.ParenthesisCloseToken);
        return name.FullName switch
        {
            "namespace" => new NamespaceAnnotationSyntax(atSignToken, name, parenthesisOpenToken, jsonValue, parenthesisCloseToken),
            "aliases" => new AliasesAnnotationSyntax(atSignToken, name, parenthesisOpenToken, jsonValue, parenthesisCloseToken),
            "order" => new OrderAnnotationSyntax(atSignToken, name, parenthesisOpenToken, jsonValue, parenthesisCloseToken),
            "logicalType" => new LogicalTypeAnnotationSyntax(atSignToken, name, parenthesisOpenToken, jsonValue, parenthesisCloseToken),

            _ => new CustomAnnotationSyntax(atSignToken, name, parenthesisOpenToken, jsonValue, parenthesisCloseToken),
        };
    }

    private AnnotationNameSyntax ParseAnnotationName()
    {
        var separatedIdentifiers = ParseSeparatedList(
            parseNode: () => _stream.Current.SyntaxKind is SyntaxKind.NamespaceKeyword ? Next() : Match(SyntaxKind.IdentifierToken),
            separator: SyntaxKind.DotToken,
            terminators: SyntaxKind.ParenthesisOpenToken);

        return new AnnotationNameSyntax(separatedIdentifiers);
    }

    private JsonValueSyntax ParseJsonValue()
    {
        var index = _stream.Position;
        var json = new JsonParser(this).Parse();
        var count = _stream.Position - index;
        return new JsonValueSyntax(new SyntaxList<SyntaxToken>([.. _stream.GetTokens(index, count)]), json);
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

        if (_stream.Current.SyntaxKind is SyntaxKind.QuestionMarkToken)
        {
            type = new OptionalTypeSyntax(type, Next());
        }

        if (annotations.Count > 0)
        {
            type = new AnnotatedTypeSyntax(annotations, type);
            // TODO:
            // Do we want to emit diagnostics for annotations on named type references?
            // The annotations should be on the declaration itself, not on the reference.
        }

        return type;
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
        var types = ParseSeparatedList(ParseType, SyntaxKind.CommaToken, SyntaxKind.BraceCloseToken);
        var braceCloseToken = Match(SyntaxKind.BraceCloseToken);
        return new UnionTypeSyntax(unionKeyword, braceOpenToken, types, braceCloseToken);
    }

    private DecimalLogicalTypeSyntax ParseDecimalLogicalType()
    {
        var decimalKeyword = Match(SyntaxKind.DecimalKeyword);
        var parenthesisOpenToken = Match(SyntaxKind.ParenthesisOpenToken);
        var precisionLiteralToken = Match(SyntaxKind.IntegerLiteralToken);
        var commaToken = Match(SyntaxKind.CommaToken);
        var scaleLiteralToken = Match(SyntaxKind.IntegerLiteralToken);
        var parenthesisCloseToken = Match(SyntaxKind.ParenthesisCloseToken);
        return new DecimalLogicalTypeSyntax(
            decimalKeyword,
            parenthesisOpenToken,
            precisionLiteralToken,
            commaToken,
            scaleLiteralToken,
            parenthesisCloseToken);
    }

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
            terminators: SyntaxKind.SemicolonToken);

        return new QualifiedNameSyntax(separatedIdentifiers);
    }

    private SyntaxList<T> ParseList<T>(Func<T> parseNode, params ReadOnlySpan<SyntaxKind> terminators) where T : ISyntaxNode
    {
        var nodes = ImmutableArray.CreateBuilder<T>();
        while (!_stream.IsAtEnd && !terminators.Contains(_stream.Current.SyntaxKind))
        {
            using (EnsureProgress(terminators))
            {
                nodes.Add(parseNode());
            }
        }

        return new SyntaxList<T>(nodes.DrainToImmutable());
    }

    private SeparatedSyntaxList<T> ParseSeparatedList<T>(Func<T> parseNode, SyntaxKind separator, params ReadOnlySpan<SyntaxKind> terminators) where T : ISyntaxNode
    {
        var nodes = ImmutableArray.CreateBuilder<ISyntaxNode>();

        while (!_stream.IsAtEnd && !terminators.Contains(_stream.Current.SyntaxKind))
        {
            nodes.Add(parseNode());
            if (_stream.Current.SyntaxKind != separator) break;
            nodes.Add(Next());
        }

        return new SeparatedSyntaxList<T>(nodes.DrainToImmutable());
    }

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

    private sealed class JsonParser(Parser parser)
    {
        public JsonNode? Parse()
        {
            parser._cancellationToken.ThrowIfCancellationRequested();
            return parser._stream.Current.SyntaxKind switch
            {
                SyntaxKind.NullKeyword => ParseNull(),
                SyntaxKind.TrueKeyword => ParseTrue(),
                SyntaxKind.FalseKeyword => ParseFalse(),
                SyntaxKind.IntegerLiteralToken or SyntaxKind.FloatLiteralToken => ParseNumber(),
                SyntaxKind.StringLiteralToken => ParseString(),
                SyntaxKind.BracketOpenToken => ParseArray(),
                SyntaxKind.BraceOpenToken => ParseObject(),
                SyntaxKind.IdentifierToken => ParseSymbol(),
                _ => ParseUnexpected(),
            };
        }

        private JsonNode? ParseUnexpected()
        {
            var token = parser._stream.Current;
            parser._diagnostics.Add(AvroDiagnostic.UnexpectedJsonValue(token));
            if (!parser._stream.IsAtEnd)
                _ = parser.Next();

            return null;
        }

        private JsonValue? ParseNull()
        {
            _ = parser.Match(SyntaxKind.NullKeyword);
            return null;
        }

        private JsonValue ParseTrue()
        {
            _ = parser.Match(SyntaxKind.TrueKeyword);
            return JsonValue.Create(true);
        }

        private JsonValue ParseFalse()
        {
            _ = parser.Match(SyntaxKind.FalseKeyword);
            return JsonValue.Create(false);
        }

        private JsonValue? ParseNumber()
        {
            var numberToken = parser._stream.Current.SyntaxKind is SyntaxKind.IntegerLiteralToken
                ? parser.Match(SyntaxKind.IntegerLiteralToken)
                : parser.Match(SyntaxKind.FloatLiteralToken);
            return numberToken.Value switch
            {
                int value => JsonValue.Create(value),
                long value => JsonValue.Create(value),
                double value => JsonValue.Create(value),
                _ => JsonValue.Create(numberToken.Value),
            };
        }

        private JsonValue? ParseString()
        {
            var stringToken = parser.Match(SyntaxKind.StringLiteralToken);
            return JsonValue.Create((string?)stringToken.Value);
        }

        private JsonArray ParseArray()
        {
            _ = parser.Match(SyntaxKind.BracketOpenToken);
            var array = new JsonArray();
            while (parser._stream is { IsAtEnd: false, Current.SyntaxKind: not SyntaxKind.BracketCloseToken })
            {
                parser._cancellationToken.ThrowIfCancellationRequested();
                array.Add(Parse());
                if (parser._stream.Current.SyntaxKind is not SyntaxKind.CommaToken)
                    break;

                _ = parser.Match(SyntaxKind.CommaToken);
            }

            _ = parser.Match(SyntaxKind.BracketCloseToken);

            return array;
        }

        private JsonObject ParseObject()
        {
            _ = parser.Match(SyntaxKind.BraceOpenToken);
            var @object = new JsonObject();
            while (parser._stream is { IsAtEnd: false, Current.SyntaxKind: not SyntaxKind.BraceCloseToken })
            {
                parser._cancellationToken.ThrowIfCancellationRequested();
                var propertyName = parser._stream.Current.SyntaxKind is SyntaxKind.StringLiteralToken
                    ? parser.Match(SyntaxKind.StringLiteralToken)
                    : parser.Match(SyntaxKind.IdentifierToken);
                _ = parser.Match(SyntaxKind.ColonToken);
                var propertyValue = Parse();

                @object[(string?)propertyName.Value ?? propertyName.ValueText] = propertyValue;
                if (parser._stream.Current.SyntaxKind is not SyntaxKind.CommaToken)
                    break;

                _ = parser.Match(SyntaxKind.CommaToken);
            }

            _ = parser.Match(SyntaxKind.BraceCloseToken);

            return @object;
        }

        private JsonValue ParseSymbol()
        {
            var symbolToken = parser.Match(SyntaxKind.IdentifierToken);
            return JsonValue.Create(symbolToken.ValueText);
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
