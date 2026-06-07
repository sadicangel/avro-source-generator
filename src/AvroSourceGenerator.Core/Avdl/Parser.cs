using System.Collections.Immutable;
using AvroSourceGenerator.Avdl.Syntax;
using AvroSourceGenerator.Avdl.Syntax.Annotations;
using AvroSourceGenerator.Avdl.Syntax.Declarations;
using AvroSourceGenerator.Avdl.Syntax.Directives;
using AvroSourceGenerator.Avdl.Syntax.Types;

namespace AvroSourceGenerator.Avdl;

public sealed class Parser(SourceText sourceText)
{
    private readonly SyntaxTokenStream _stream = new(sourceText);
    private readonly List<IAnnotationSyntax> _annotations = [];
    private readonly List<DocumentationSyntax> _documentation = [];

    public static CompilationUnitSyntax Parse(SourceText sourceText)
    {
        // TODO: Worth to use a single instance and reset it for each file?
        var parser = new Parser(sourceText);
        return parser.Parse();
    }

    private CompilationUnitSyntax Parse()
    {
        var directives = ParseDirectives();
        var declarations = ParseDeclarations();
        // TODO: Check if protocol or schema IDL and validate directives.
        return new CompilationUnitSyntax(directives, declarations);
    }

    private SyntaxList<IDirectiveSyntax> ParseDirectives()
    {
        var directives = ParseList<IDirectiveSyntax>(
            parseNode: () => _stream.Current.SyntaxKind switch
            {
                SyntaxKind.NamespaceKeyword => ParseNamespaceDirective(),
                SyntaxKind.SchemaKeyword => ParseSchemaDirective(),
                SyntaxKind.ImportKeyword => ParseImportDirective(),
                _ => throw new InvalidOperationException($"Unexpected syntax kind: {_stream.Current.SyntaxKind}"),
            },
            terminators: [SyntaxKind.AtSignToken, SyntaxKind.DocumentationTrivia, SyntaxKind.EnumKeyword, SyntaxKind.FixedKeyword, SyntaxKind.RecordKeyword, SyntaxKind.ErrorKeyword, SyntaxKind.ProtocolKeyword]);
        return directives;
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
        var declarations = ParseList(ParseDeclaration);
        return declarations;
    }

    private IDeclarationSyntax ParseDeclaration()
    {
        EnqueueMetadata();
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
            switch (_stream.Current.SyntaxKind)
            {
                case SyntaxKind.ImportKeyword:
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
