using System.Collections.Immutable;
using AvroSourceGenerator.Avdl.Diagnostics;
using AvroSourceGenerator.Avdl.Syntax.Annotations;
using AvroSourceGenerator.Avdl.Syntax.Declarations;
using AvroSourceGenerator.Avdl.Syntax.Directives;
using AvroSourceGenerator.Avdl.Syntax.Types;
using AvroSourceGenerator.Avdl.Text;

namespace AvroSourceGenerator.Avdl.Syntax;

public sealed class Parser(SourceText sourceText)
{
    private readonly SyntaxTokenStream _stream = new(sourceText);
    private readonly List<IAnnotationSyntax> _annotations = [];
    private readonly List<DocumentationSyntax> _documentation = [];
    private readonly List<SyntaxDiagnostic> _diagnostics = [];

    public static SyntaxTree Parse(SourceText sourceText) => new Parser(sourceText).Parse();

    public SyntaxTree Parse()
    {
        var document = ParseDocument();

        return new SyntaxTree(sourceText, document, [.. _stream.Diagnostics.Concat(_diagnostics)]);
    }

    private DocumentSyntax ParseDocument()
    {
        var directives = ParseDirectives();
        var declarations = ParseDeclarations();
        // TODO: Check if protocol or schema IDL and validate directives.
        return new DocumentSyntax(directives, declarations);
    }

    private SyntaxList<IDirectiveSyntax> ParseDirectives()
    {
        var directives = ImmutableArray.CreateBuilder<IDirectiveSyntax>();
        while (_stream.Current.SyntaxKind is SyntaxKind.NamespaceKeyword or SyntaxKind.SchemaKeyword or SyntaxKind.ImportKeyword)
        {
            if (_stream.Current.SyntaxKind is SyntaxKind.NamespaceKeyword)
            {
                directives.Add(ParseNamespaceDirective());
            }
            else if (_stream.Current.SyntaxKind is SyntaxKind.SchemaKeyword)
            {
                directives.Add(ParseSchemaDirective());
            }
            else
            {
                directives.Add(ParseImportDirective());
            }
        }

        return new SyntaxList<IDirectiveSyntax>(directives.ToImmutable());
    }

    private NamespaceDirectiveSyntax ParseNamespaceDirective()
    {
        var namespaceKeyword = _stream.Match(SyntaxKind.NamespaceKeyword);
        var namespaceName = ParseName();
        var semicolonToken = _stream.Match(SyntaxKind.SemicolonToken);
        return new NamespaceDirectiveSyntax(namespaceKeyword, namespaceName, semicolonToken);
    }

    private SchemaDirectiveSyntax ParseSchemaDirective()
    {
        var schemaKeyword = _stream.Match(SyntaxKind.SchemaKeyword);
        var mainSchemaName = ParseSimpleName();
        var semicolonToken = _stream.Match(SyntaxKind.SemicolonToken);
        return new SchemaDirectiveSyntax(schemaKeyword, mainSchemaName, semicolonToken);
    }

    private ImportDirectiveSyntax ParseImportDirective()
    {
        var importKeyword = _stream.Match(SyntaxKind.ImportKeyword);
        var importTypeKeyword = _stream.Current.SyntaxKind switch
        {
            SyntaxKind.ProtocolKeyword => _stream.Match(SyntaxKind.ProtocolKeyword),
            SyntaxKind.SchemaKeyword => _stream.Match(SyntaxKind.SchemaKeyword),
            _ => _stream.Match(SyntaxKind.IdlKeyword),
        };
        var importPathLiteralToken = _stream.Match(SyntaxKind.StringLiteralToken);
        var semicolonToken = _stream.Match(SyntaxKind.SemicolonToken);
        return new ImportDirectiveSyntax(importKeyword, importTypeKeyword, importPathLiteralToken, semicolonToken);
    }

    private SyntaxList<IDeclarationSyntax> ParseDeclarations()
    {
        var declarations = ImmutableArray.CreateBuilder<IDeclarationSyntax>();
        while (!_stream.IsAtEnd)
        {
            EnqueueMetadata();
            if (_stream.IsAtEnd)
            {
                break;
            }

            declarations.Add(ParseDeclaration());
        }

        return new SyntaxList<IDeclarationSyntax>(declarations.ToImmutable());
    }

    private IDeclarationSyntax ParseDeclaration()
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

    private void ReportAndClearMisplacedMetadata(string target)
    {
        foreach (var documentation in _documentation)
            _diagnostics.Add(SyntaxDiagnostic.MisplacedDocumentation(documentation.DocumentationTrivia.SourceSpan, target));
        foreach (var annotation in _annotations)
            _diagnostics.Add(SyntaxDiagnostic.MisplacedAnnotation(GetAnnotationSpan(annotation), GetAnnotationName(annotation), target));

        _documentation.Clear();
        _annotations.Clear();
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
        var documentationTrivia = _stream.Match(SyntaxKind.DocumentationTrivia);
        return new DocumentationSyntax(documentationTrivia);
    }

    private IAnnotationSyntax ParseAnnotation()
    {
        var atSignToken = _stream.Match(SyntaxKind.AtSignToken);
        var name = _stream.Current.SyntaxKind is SyntaxKind.NamespaceKeyword ? _stream.Next() : _stream.Match(SyntaxKind.IdentifierToken);
        var parenthesisOpenToken = _stream.Match(SyntaxKind.ParenthesisOpenToken);
        var jsonValue = ParseJsonValue();
        var parenthesisCloseToken = _stream.Match(SyntaxKind.ParenthesisCloseToken);
        return name switch
        {
            { SyntaxKind: SyntaxKind.NamespaceKeyword } => new NamespaceAnnotationSyntax(atSignToken, name, parenthesisOpenToken, jsonValue, parenthesisCloseToken),
            { ValueText: "aliases" } => new AliasesAnnotationSyntax(atSignToken, name, parenthesisOpenToken, jsonValue, parenthesisCloseToken),
            { ValueText: "order" } => new OrderAnnotationSyntax(atSignToken, name, parenthesisOpenToken, jsonValue, parenthesisCloseToken),
            { ValueText: "logicalType" } => new LogicalTypeAnnotationSyntax(atSignToken, name, parenthesisOpenToken, jsonValue, parenthesisCloseToken),

            _ => new CustomAnnotationSyntax(atSignToken, name, parenthesisOpenToken, jsonValue, parenthesisCloseToken),
        };
    }

    private JsonValueSyntax ParseJsonValue()
    {
        var index = _stream.Position;
        var json = JsonParser.Parse(_stream);
        var count = _stream.Position - index;
        return new JsonValueSyntax(new SyntaxList<SyntaxToken>([.. _stream.GetTokens(index, count)]), json);
    }

    private EnumDeclarationSyntax ParseEnumDeclaration()
    {
        var enumKeyword = _stream.Match(SyntaxKind.EnumKeyword);
        var name = ParseSimpleName();
        var (documentation, annotations) = DequeueMetadata();
        var braceOpenToken = _stream.Match(SyntaxKind.BraceOpenToken);
        var symbols = ParseSeparatedList(
            parseNode: ParseSimpleName,
            separator: SyntaxKind.CommaToken,
            terminators: SyntaxKind.BraceCloseToken);
        var braceCloseToken = _stream.Match(SyntaxKind.BraceCloseToken);
        var defaultValue = ParseDefaultValueClause();
        var semicolonToken = defaultValue is not null ? _stream.Match(SyntaxKind.SemicolonToken) : null;
        return new EnumDeclarationSyntax(enumKeyword, name, documentation, annotations, braceOpenToken, symbols, braceCloseToken, defaultValue, semicolonToken);
    }

    private FixedDeclarationSyntax ParseFixedDeclaration()
    {
        var fixedKeyword = _stream.Match(SyntaxKind.FixedKeyword);
        var name = ParseSimpleName();
        var (documentation, annotations) = DequeueMetadata();
        var parenthesisOpenToken = _stream.Match(SyntaxKind.ParenthesisOpenToken);
        var sizeLiteralToken = _stream.Match(SyntaxKind.IntegerLiteralToken);
        var parenthesisCloseToken = _stream.Match(SyntaxKind.ParenthesisCloseToken);
        var semicolonToken = _stream.Match(SyntaxKind.SemicolonToken);
        return new FixedDeclarationSyntax(fixedKeyword, name, documentation, annotations, parenthesisOpenToken, sizeLiteralToken, parenthesisCloseToken, semicolonToken);
    }

    private RecordDeclarationSyntax ParseRecordDeclaration()
    {
        var recordKeyword = _stream.Match(SyntaxKind.RecordKeyword);
        var name = ParseSimpleName();
        var (documentation, annotations) = DequeueMetadata();
        var braceOpenToken = _stream.Match(SyntaxKind.BraceOpenToken);
        var fields = ParseList(
            parseNode: ParseFieldDeclaration,
            terminators: SyntaxKind.BraceCloseToken);
        var braceCloseToken = _stream.Match(SyntaxKind.BraceCloseToken);
        return new RecordDeclarationSyntax(recordKeyword, name, documentation, annotations, braceOpenToken, fields, braceCloseToken);
    }

    private ErrorDeclarationSyntax ParseErrorDeclaration()
    {
        var errorKeyword = _stream.Match(SyntaxKind.ErrorKeyword);
        var name = ParseSimpleName();
        var (documentation, annotations) = DequeueMetadata();
        var braceOpenToken = _stream.Match(SyntaxKind.BraceOpenToken);
        var fields = ParseList(
            parseNode: ParseFieldDeclaration,
            terminators: SyntaxKind.BraceCloseToken);
        var braceCloseToken = _stream.Match(SyntaxKind.BraceCloseToken);
        return new ErrorDeclarationSyntax(errorKeyword, name, documentation, annotations, braceOpenToken, fields, braceCloseToken);
    }

    private FieldDeclarationSyntax ParseFieldDeclaration()
    {
        EnqueueMetadata();
        var type = ParseType();
        EnqueueMetadata();
        var name = ParseSimpleName();
        var (documentation, annotations) = DequeueMetadata();
        var defaultValue = ParseDefaultValueClause();
        var semicolonToken = _stream.Match(SyntaxKind.SemicolonToken);
        return new FieldDeclarationSyntax(type, name, documentation, annotations, defaultValue, semicolonToken);
    }

    private DefaultValueClauseSyntax? ParseDefaultValueClause()
    {
        if (_stream.Current.SyntaxKind != SyntaxKind.EqualsToken) return null;
        var equalsToken = _stream.Match(SyntaxKind.EqualsToken);
        var jsonValue = ParseJsonValue();
        return new DefaultValueClauseSyntax(equalsToken, jsonValue);
    }

    private ProtocolDeclarationSyntax ParseProtocolDeclaration()
    {
        var protocolKeyword = _stream.Match(SyntaxKind.ProtocolKeyword);
        var name = ParseSimpleName();
        var (documentation, annotations) = DequeueMetadata();
        var braceOpenToken = _stream.Match(SyntaxKind.BraceOpenToken);
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
                    ReportAndClearMisplacedMetadata("message declaration");
                    messages.Add(ParseMessageDeclaration());
                    break;
            }
        }

        var braceCloseToken = _stream.Match(SyntaxKind.BraceCloseToken);

        return new ProtocolDeclarationSyntax(
            protocolKeyword,
            name,
            documentation,
            annotations,
            braceOpenToken,
            new SyntaxList<ImportDirectiveSyntax>(imports.ToImmutable()),
            new SyntaxList<ISchemaDeclarationSyntax>(types.ToImmutable()),
            new SyntaxList<MessageDeclarationSyntax>(messages.ToImmutable()),
            braceCloseToken);
    }

    private MessageDeclarationSyntax ParseMessageDeclaration()
    {
        var type = ParseType();
        var name = ParseSimpleName();
        var parenthesisOpenToken = _stream.Match(SyntaxKind.ParenthesisOpenToken);
        var parameters = ParseSeparatedList(ParseParameterDeclaration, SyntaxKind.CommaToken, SyntaxKind.ParenthesisCloseToken);
        var parenthesisCloseToken = _stream.Match(SyntaxKind.ParenthesisCloseToken);
        var oneWayClause = ParseOneWayClause();
        var throwsErrorClause = ParseThrowsErrorClause();
        var semicolonToken = _stream.Match(SyntaxKind.SemicolonToken);
        return new MessageDeclarationSyntax(
            type,
            name,
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
        var defaultValue = ParseDefaultValueClause();
        return new ParameterDeclarationSyntax(type, name, defaultValue);
    }

    private OneWayClauseSyntax? ParseOneWayClause()
    {
        if (_stream.Current.SyntaxKind is not SyntaxKind.OneWayKeyword) return null;
        var oneWayKeyword = _stream.Next();
        return new OneWayClauseSyntax(oneWayKeyword);
    }

    private ThrowsErrorClauseSyntax? ParseThrowsErrorClause()
    {
        if (_stream.Current.SyntaxKind is not SyntaxKind.ThrowsKeyword) return null;
        var throwsKeyword = _stream.Next();
        var errors = ParseSeparatedList(ParseNamedType, SyntaxKind.CommaToken, SyntaxKind.SemicolonToken);
        return new ThrowsErrorClauseSyntax(throwsKeyword, errors);
    }

    private INameSyntax ParseName() => _stream.Peek(1).SyntaxKind == SyntaxKind.DotToken
        ? ParseQualifiedName()
        : ParseSimpleName();

    private SimpleNameSyntax ParseSimpleName()
    {
        var identifierToken = _stream.Match(SyntaxKind.IdentifierToken);
        return new SimpleNameSyntax(identifierToken);
    }

    private QualifiedNameSyntax ParseQualifiedName()
    {
        var separatedIdentifiers = ParseSeparatedList(
            parseNode: () => _stream.Match(SyntaxKind.IdentifierToken),
            separator: SyntaxKind.DotToken,
            terminators: SyntaxKind.SemicolonToken);

        return new QualifiedNameSyntax(separatedIdentifiers);
    }

    private ITypeSyntax ParseType()
    {
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
                or SyntaxKind.BytesKeyword => new PrimitiveTypeSyntax(_stream.Next()),

            SyntaxKind.ArrayKeyword => ParseArrayType(),
            SyntaxKind.MapKeyword => ParseMapType(),
            SyntaxKind.UnionKeyword => ParseUnionType(),

            SyntaxKind.DecimalKeyword => ParseDecimalLogicalType(),
            SyntaxKind.DateKeyword
                or SyntaxKind.TimeMsKeyword
                or SyntaxKind.TimestampMsKeyword
                or SyntaxKind.LocalTimestampMsKeyword
                or SyntaxKind.UuidKeyword => new LogicalTypeSyntax(_stream.Next()),

            _ => ParseNamedType(),
        };

        if (_stream.Current.SyntaxKind is SyntaxKind.QuestionMarkToken)
        {
            type = new OptionalTypeSyntax(type, _stream.Next());
        }

        return type;
    }

    private ArrayTypeSyntax ParseArrayType()
    {
        var arrayKeyword = _stream.Match(SyntaxKind.ArrayKeyword);
        var lessThanToken = _stream.Match(SyntaxKind.LessThanToken);
        var elementType = ParseType();
        var greaterThanToken = _stream.Match(SyntaxKind.GreaterThanToken);
        return new ArrayTypeSyntax(arrayKeyword, lessThanToken, elementType, greaterThanToken);
    }

    private MapTypeSyntax ParseMapType()
    {
        var mapKeyword = _stream.Match(SyntaxKind.MapKeyword);
        var lessThanToken = _stream.Match(SyntaxKind.LessThanToken);
        var valueType = ParseType();
        var greaterThanToken = _stream.Match(SyntaxKind.GreaterThanToken);
        return new MapTypeSyntax(mapKeyword, lessThanToken, valueType, greaterThanToken);
    }

    private UnionTypeSyntax ParseUnionType()
    {
        var unionKeyword = _stream.Match(SyntaxKind.UnionKeyword);
        var braceOpenToken = _stream.Match(SyntaxKind.BraceOpenToken);
        var types = ParseSeparatedList(ParseType, SyntaxKind.CommaToken, SyntaxKind.BraceCloseToken);
        var braceCloseToken = _stream.Match(SyntaxKind.BraceCloseToken);
        return new UnionTypeSyntax(unionKeyword, braceOpenToken, types, braceCloseToken);
    }

    private DecimalLogicalTypeSyntax ParseDecimalLogicalType()
    {
        var decimalKeyword = _stream.Match(SyntaxKind.DecimalKeyword);
        var parenthesisOpenToken = _stream.Match(SyntaxKind.ParenthesisOpenToken);
        var precisionLiteralToken = _stream.Match(SyntaxKind.IntegerLiteralToken);
        var commaToken = _stream.Match(SyntaxKind.CommaToken);
        var scaleLiteralToken = _stream.Match(SyntaxKind.IntegerLiteralToken);
        var parenthesisCloseToken = _stream.Match(SyntaxKind.ParenthesisCloseToken);
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

    private SyntaxList<T> ParseList<T>(Func<T> parseNode, params ReadOnlySpan<SyntaxKind> terminators) where T : ISyntaxNode
    {
        var nodes = ImmutableArray.CreateBuilder<T>();
        while (!_stream.IsAtEnd && !terminators.Contains(_stream.Current.SyntaxKind)) nodes.Add(parseNode());
        return new SyntaxList<T>(nodes.ToImmutable());
    }

    private SeparatedSyntaxList<T> ParseSeparatedList<T>(Func<T> parseNode, SyntaxKind separator, params ReadOnlySpan<SyntaxKind> terminators) where T : ISyntaxNode
    {
        var nodes = ImmutableArray.CreateBuilder<ISyntaxNode>();

        while (!_stream.IsAtEnd && !terminators.Contains(_stream.Current.SyntaxKind))
        {
            nodes.Add(parseNode());
            if (_stream.Current.SyntaxKind != separator) break;
            nodes.Add(_stream.Next());
        }

        return new SeparatedSyntaxList<T>(nodes.ToImmutable());
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
        annotation switch
        {
            NamespaceAnnotationSyntax => "namespace",
            AliasesAnnotationSyntax aliasesAnnotation => aliasesAnnotation.AliasesIdentifier.ValueText,
            OrderAnnotationSyntax orderAnnotation => orderAnnotation.OrderIdentifier.ValueText,
            LogicalTypeAnnotationSyntax logicalTypeAnnotation => logicalTypeAnnotation.LogicalTypeIdentifier.ValueText,
            CustomAnnotationSyntax customAnnotation => customAnnotation.NameIdentifier.ValueText,
            _ => "unknown",
        };
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
