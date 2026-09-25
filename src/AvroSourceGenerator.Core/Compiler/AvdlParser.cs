using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using AvroSourceGenerator.Avdl.Annotations;
using AvroSourceGenerator.Avdl.Declarations;
using AvroSourceGenerator.Avdl.Directives;
using AvroSourceGenerator.Avdl.Syntax;
using AvroSourceGenerator.Avdl.Syntax.Declarations;
using AvroSourceGenerator.Avdl.Syntax.Directives;
using AvroSourceGenerator.Avdl.Syntax.Types;
using AvroSourceGenerator.Avdl.Types;
using AvroSourceGenerator.Avjs;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Avdl;

public sealed class AvdlParser(SourceText sourceText, AvroParseOptions options, CancellationToken cancellationToken)
    : AvxxParser(options)
{
    private readonly SyntaxTokenStream _stream = new(sourceText, cancellationToken);
    private readonly CancellationToken _cancellationToken = cancellationToken;
    private readonly List<IAnnotationSyntax> _annotations = [];
    private readonly List<DocumentationSyntax> _documentation = [];
    public AvdlParser(SourceText sourceText, CancellationToken cancellationToken) : this(sourceText, default, cancellationToken) { }

    public static SyntaxTree Parse(SourceText sourceText, CancellationToken cancellationToken) => new AvdlParser(sourceText, cancellationToken).Parse();

    public SyntaxTree Parse()
    {
        var document = ParseDocument();
        return new SyntaxTree(sourceText, document, [.. _stream.Diagnostics.Concat(Diagnostics)]);
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

        return new SyntaxList<IDirectiveSyntax>(directives.DrainToImmutable());
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
        var mainSchemaType = ParseType();
        var semicolonToken = _stream.Match(SyntaxKind.SemicolonToken);
        return new SchemaDirectiveSyntax(schemaKeyword, mainSchemaType, semicolonToken);
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

    private void ReportAndClearMisplacedMetadata(string target)
    {
        foreach (var documentation in _documentation)
            Report(AvroDiagnostic.MisplacedDocumentation(documentation.DocumentationTrivia.SourceSpan, target));
        foreach (var annotation in _annotations)
            Report(AvroDiagnostic.MisplacedAnnotation(GetAnnotationSpan(annotation), GetAnnotationName(annotation), target));

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
        var name = ParseAnnotationName();
        var parenthesisOpenToken = _stream.Match(SyntaxKind.ParenthesisOpenToken);
        var jsonValue = ParseJsonValue();
        var parenthesisCloseToken = _stream.Match(SyntaxKind.ParenthesisCloseToken);
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
            parseNode: () => _stream.Current.SyntaxKind is SyntaxKind.NamespaceKeyword ? _stream.Next() : _stream.Match(SyntaxKind.IdentifierToken),
            separator: SyntaxKind.DotToken,
            terminators: SyntaxKind.ParenthesisOpenToken);

        return new AnnotationNameSyntax(separatedIdentifiers);
    }

    private JsonValueSyntax ParseJsonValue()
    {
        var index = _stream.Position;
        var json = JsonParser.Parse(_stream, _cancellationToken);
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
        EnqueueDocumentation();
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

        var braceCloseToken = _stream.Match(SyntaxKind.BraceCloseToken);

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
        var parenthesisOpenToken = _stream.Match(SyntaxKind.ParenthesisOpenToken);
        var parameters = ParseSeparatedList(ParseParameterDeclaration, SyntaxKind.CommaToken, SyntaxKind.ParenthesisCloseToken);
        var parenthesisCloseToken = _stream.Match(SyntaxKind.ParenthesisCloseToken);
        var oneWayClause = ParseOneWayClause();
        var throwsErrorClause = ParseThrowsErrorClause();
        var semicolonToken = _stream.Match(SyntaxKind.SemicolonToken);
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
            nodes.Add(_stream.Next());
        }

        return new SeparatedSyntaxList<T>(nodes.DrainToImmutable());
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

    private ProgressTracker EnsureProgress(ReadOnlySpan<SyntaxKind> terminators = default) => new(_stream, terminators);

    private readonly ref struct ProgressTracker(SyntaxTokenStream stream, ReadOnlySpan<SyntaxKind> terminators)
    {
        private readonly int _startPosition = stream.Position;
        private readonly ReadOnlySpan<SyntaxKind> _terminators = terminators;

        public void Dispose()
        {
            if (stream.Position != _startPosition || stream.IsAtEnd || _terminators.Contains(stream.Current.SyntaxKind))
                return;

            _ = stream.Next();
        }
    }

    public static AvroFile ParseFile(SourceText source, AvroParseOptions options, CancellationToken cancellationToken) =>
        new AvdlParser(source, options, cancellationToken).ParseFile();

    public AvroFile ParseFile()
    {
        var syntaxTree = Parse();
        if (!syntaxTree.Diagnostics.IsEmpty)
            return AvroFile.Invalid(sourceText, syntaxTree.Diagnostics, Options);

        return ParseCore(syntaxTree);
    }

    private AvroFile ParseCore(SyntaxTree syntaxTree)
    {
        var source = syntaxTree.SourceText;

        var imports = syntaxTree.Document.ImportDirectives
            .Concat(
                syntaxTree.Document.Declarations
                    .OfType<ProtocolDeclarationSyntax>()
                    .SelectMany(static protocol => protocol.Imports))
            .WithCancellation(_cancellationToken)
            .Select(static import => new AvroImport(
                import.ImportTypeKeyword.SyntaxKind switch
                {
                    SyntaxKind.IdlKeyword => AvroImportKind.Idl,
                    SyntaxKind.ProtocolKeyword => AvroImportKind.Protocol,
                    SyntaxKind.SchemaKeyword => AvroImportKind.Schema,
                    _ => throw new InvalidOperationException("Unreachable: Unsupported Avro import kind."),
                },
                import.ImportPathLiteralToken.Value as string ?? string.Empty,
                import.ImportPathLiteralToken.SourceSpan))
            .ToImmutableArray();
        var rootSchema = Document(syntaxTree);
        if (rootSchema is null || Diagnostics.HasErrors) return AvroFile.Invalid(source, [.. Diagnostics], Options);
        if (imports.IsEmpty && !rootSchema.ContainsTopLevelSchema())
        {
            var sourceSpan = syntaxTree.Document.SchemaDirective?.MainSchemaType is { } mainSchemaType
                ? mainSchemaType.GetSourceSpan()
                : syntaxTree.Document.GetSourceSpan();
            Report(AvroDiagnostic.MissingIdlRoot(sourceSpan));
            return AvroFile.Invalid(source, [.. Diagnostics], Options);
        }

        return new AvroFile(
            source,
            rootSchema,
            [.. Declarations],
            [.. DeclarationSpans],
            GetReferences(),
            GetReferenceSpans(),
            GetDependencies(),
            imports,
            [.. Diagnostics],
            Options);
    }

    private AvroSchema? Document(SyntaxTree syntaxTree)
    {
        var document = syntaxTree.Document;
        var containingNamespace = document.NamespaceDirective?.NamespaceName.FullName;
        var mainSchema = document.SchemaDirective?.MainSchemaType;
        if (mainSchema is null)
        {
            if (document.Declarations is not [ProtocolDeclarationSyntax protocol])
                return Invalid<AvroSchema?>(null, AvroDiagnostic.InvalidIdlDocument(document.GetSourceSpan()));
            return Protocol(protocol, containingNamespace);
        }

        var isValid = true;
        foreach (var declaration in document.Declarations.WithCancellation(_cancellationToken))
        {
            if (declaration is not ISchemaDeclarationSyntax schemaDeclaration)
            {
                Report(AvroDiagnostic.InvalidIdlDeclaration(declaration.GetSourceSpan(), declaration.SyntaxKind));
                isValid = false;
                continue;
            }

            isValid &= Schema(schemaDeclaration, containingNamespace) is not null;
        }

        var root = Type(mainSchema, containingNamespace);
        return isValid ? root : null;
    }

    private AvroSchema? Type(
        ITypeSyntax syntax,
        string? containingNamespace,
        ImmutableSortedDictionary<string, JsonElement>? properties = null,
        JsonElement? defaultJson = null)
    {
        properties ??= ImmutableSortedDictionary<string, JsonElement>.Empty;

        return syntax switch
        {
            AnnotatedTypeSyntax type => Annotated(type, containingNamespace, defaultJson),
            ArrayTypeSyntax type => Array(type, containingNamespace, properties),
            ILogicalTypeSyntax type => Logical(type, containingNamespace),
            MapTypeSyntax type => Map(type, containingNamespace, properties),
            NamedTypeSyntax type => Named(type, containingNamespace),
            OptionalTypeSyntax type => Optional(type, containingNamespace, defaultJson),
            PrimitiveTypeSyntax type => Primitive(type, containingNamespace, properties),
            UnionTypeSyntax type => Union(type, containingNamespace),
            _ => Invalid<AvroSchema?>(null, AvroDiagnostic.InvalidIdlType(syntax.GetSourceSpan(), syntax.SyntaxKind)),
        };
    }

    private NamedSchema? Schema(ISchemaDeclarationSyntax declaration, string? containingNamespace)
    {
        return declaration switch
        {
            EnumDeclarationSyntax syntax => Enum(syntax, containingNamespace),
            ErrorDeclarationSyntax syntax => Error(syntax, containingNamespace),
            FixedDeclarationSyntax syntax => Fixed(syntax, containingNamespace),
            RecordDeclarationSyntax syntax => Record(syntax, containingNamespace),
            _ => Invalid<NamedSchema?>(null, AvroDiagnostic.InvalidIdlSchemaDeclaration(declaration.GetSourceSpan(), declaration.SyntaxKind))
        };
    }

    private AvroSchema? Annotated(AnnotatedTypeSyntax syntax, string? containingNamespace, JsonElement? defaultJson)
    {
        var tracker = TrackDiagnostics();
        var logicalTypeName = syntax.Annotations.OfType<LogicalTypeAnnotationSyntax>().LastOrDefault() is { } annotation
            ? GetString(annotation.JsonValue, "Logical type annotation value", required: true)
            : null;
        var properties = syntax.Annotations.GetProperties(ReservedSchemaProperties.IsReserved);
        var underlyingSchema = Type(syntax.Type, containingNamespace, properties, defaultJson);
        if (underlyingSchema is null || tracker.HasNewDiagnostics) return null;
        return logicalTypeName is not null
            ? LogicalSchema.Create(logicalTypeName, underlyingSchema, Options.GenerationTarget)
            : underlyingSchema;
    }

    private AvroSchema? Primitive(PrimitiveTypeSyntax syntax, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement> properties)
    {
        return syntax.SyntaxKind switch
        {
            SyntaxKind.VoidType => AvroSchema.Null,
            SyntaxKind.NullType => AvroSchema.Null,
            SyntaxKind.IntType => AvroSchema.Int,
            SyntaxKind.LongType => AvroSchema.Long,
            SyntaxKind.StringType => AvroSchema.String,
            SyntaxKind.BooleanType => AvroSchema.Boolean,
            SyntaxKind.FloatType => AvroSchema.Float,
            SyntaxKind.DoubleType => AvroSchema.Double,
            SyntaxKind.BytesType => AvroSchema.Bytes,

            _ => Invalid<AvroSchema?>(null, AvroDiagnostic.InvalidIdlPrimitive(syntax.TypeKeyword.SourceSpan, syntax.SyntaxKind))
        };
    }

    private AvroSchema? Array(ArrayTypeSyntax syntax, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement> properties)
    {
        var items = Type(syntax.ItemType, containingNamespace);
        return items is not null ? new ArraySchema(items, Documentation: null, properties) : null;
    }

    private AvroSchema? Map(MapTypeSyntax syntax, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement> properties)
    {
        var values = Type(syntax.ValueType, containingNamespace);
        return values is not null ? new MapSchema(values, Documentation: null, properties) : null;
    }

    private NamedSchema? Enum(EnumDeclarationSyntax syntax, string? containingNamespace)
    {
        var tracker = TrackDiagnostics();
        var schemaName = GetSchemaName(syntax, containingNamespace);
        if (tracker.HasNewDiagnostics) return null;
        if (IsInRecursionScope(schemaName))
            return Invalid<NamedSchema?>(null, AvroDiagnostic.RecursiveDefinition(syntax.Name.GetSourceSpan(), schemaName));

        using var scope = EnterRecursionScope(schemaName);
        var documentation = syntax.GetDocumentation();
        var aliases = GetAliases(syntax);
        var symbols = syntax.Symbols.WithCancellation(_cancellationToken)
            .Select(static symbol => symbol.FullName)
            .ToImmutableArray();
        var defaultValue = GetEnumDefault(syntax);
        var properties = syntax.GetSchemaProperties();
        if (aliases.IsDefault || tracker.HasNewDiagnostics)
            return null;

        var schema = new EnumSchema(schemaName, documentation, aliases, symbols, defaultValue, properties);
        Declare(schema, syntax.GetSourceSpan());
        return schema;
    }

    private NamedSchema? Fixed(FixedDeclarationSyntax syntax, string? containingNamespace)
    {
        var tracker = TrackDiagnostics();
        var schemaName = GetSchemaName(syntax, containingNamespace);
        if (tracker.HasNewDiagnostics) return null;
        if (IsInRecursionScope(schemaName))
            return Invalid<NamedSchema?>(null, AvroDiagnostic.RecursiveDefinition(syntax.Name.GetSourceSpan(), schemaName));

        using var scope = EnterRecursionScope(schemaName);
        var documentation = syntax.GetDocumentation();
        var aliases = GetAliases(syntax);
        var size = GetFixedSize(syntax);
        var properties = syntax.GetSchemaProperties();
        if (aliases.IsDefault || tracker.HasNewDiagnostics)
            return null;

        var schema = new FixedSchema(schemaName, documentation, aliases, size, properties)
        {
            // Only Apache.Avro needs a custom type for fixed, others use byte[].
            CSharpName = Options.GenerationTarget is GenerationTarget.Apache
                ? CSharpName.FromSchemaName(schemaName)
                : AvroSchema.Bytes.CSharpName
        };
        Declare(schema, syntax.GetSourceSpan());
        return schema;
    }

    private NamedSchema? Error(ErrorDeclarationSyntax syntax, string? containingNamespace)
    {
        var tracker = TrackDiagnostics();
        var schemaName = GetSchemaName(syntax, containingNamespace);
        if (tracker.HasNewDiagnostics) return null;
        if (IsInRecursionScope(schemaName))
            return Invalid<NamedSchema?>(null, AvroDiagnostic.RecursiveDefinition(syntax.Name.GetSourceSpan(), schemaName));

        using var scope = EnterRecursionScope(schemaName);
        var documentation = syntax.GetDocumentation();
        var aliases = GetAliases(syntax);
        var fields = Fields(syntax.Fields, schemaName);
        var properties = syntax.GetSchemaProperties();
        if (aliases.IsDefault || fields.IsDefault || tracker.HasNewDiagnostics)
            return null;

        var schema = new ErrorSchema(schemaName, documentation, aliases, fields, properties);
        Declare(schema, syntax.GetSourceSpan());
        return schema;
    }

    private NamedSchema? Record(RecordDeclarationSyntax syntax, string? containingNamespace)
    {
        var tracker = TrackDiagnostics();
        var schemaName = GetSchemaName(syntax, containingNamespace);
        if (tracker.HasNewDiagnostics) return null;
        if (IsInRecursionScope(schemaName))
            return Invalid<NamedSchema?>(null, AvroDiagnostic.RecursiveDefinition(syntax.Name.GetSourceSpan(), schemaName));

        using var scope = EnterRecursionScope(schemaName);
        var documentation = syntax.GetDocumentation();
        var aliases = GetAliases(syntax);
        var fields = Fields(syntax.Fields, schemaName);
        var properties = syntax.GetSchemaProperties();
        if (aliases.IsDefault || fields.IsDefault || tracker.HasNewDiagnostics)
            return null;

        var schema = new RecordSchema(schemaName, documentation, aliases, fields, properties);
        Declare(schema, syntax.GetSourceSpan());
        return schema;
    }

    private ImmutableArray<Field> Fields(SyntaxList<FieldDeclarationSyntax> syntaxList, SchemaName containingSchemaName)
    {
        var fields = ImmutableArray.CreateBuilder<Field>();
        var valid = true;
        foreach (var syntax in syntaxList.WithCancellation(_cancellationToken))
        {
            if (Field(syntax, containingSchemaName) is { } field)
                fields.Add(field);
            else
                valid = false;
        }
        return valid ? fields.DrainToImmutable() : default;
    }

    private Field? Field(FieldDeclarationSyntax syntax, SchemaName containingSchemaName)
    {
        var tracker = TrackDiagnostics();
        var name = new FieldName(syntax.Name.FullName);
        var defaultJson = syntax.DefaultValueClause?.JsonValue.ToOptionalJsonElement();
        var type = Type(syntax.Type, containingSchemaName.Namespace, defaultJson: defaultJson);
        var documentation = syntax.GetDocumentation();
        var aliases = GetAliases(syntax);
        var order = syntax.Annotations.OfType<OrderAnnotationSyntax>().LastOrDefault() is { } annotation
            ? GetString(annotation.JsonValue, "Order annotation value", required: true)
            : null;
        var properties = syntax.GetSchemaProperties();

        if (type is null || aliases.IsDefault || tracker.HasNewDiagnostics)
            return null;

        var fieldType = ResolveFieldType(type, name, containingSchemaName, out var underlyingType, out var remarks);
        return new Field(name, fieldType, underlyingType, documentation, aliases, defaultJson, fieldType.GetValue(defaultJson), order, properties, remarks);
    }

    private AvroSchema? Optional(OptionalTypeSyntax syntax, string? containingNamespace, JsonElement? defaultJson)
    {
        var underlyingSchema = Type(syntax.Type, containingNamespace);
        if (underlyingSchema is null) return null;
        var schemas = defaultJson is null or { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined }
            ? ImmutableArray.Create(AvroSchema.Null, underlyingSchema)
            : ImmutableArray.Create(underlyingSchema, AvroSchema.Null);
        return UnionSchema.Create(schemas, Options.UseNullableReferenceTypes);
    }

    private AvroSchema? Union(UnionTypeSyntax syntax, string? containingNamespace)
    {
        var schemas = ImmutableArray.CreateBuilder<AvroSchema>();
        var valid = true;
        foreach (var type in syntax.Types.WithCancellation(_cancellationToken))
        {
            var schema = Type(type, containingNamespace);
            if (schema is not null)
                schemas.Add(schema);
            else
                valid = false;
        }
        if (!valid) return null;
        return UnionSchema.Create(schemas.DrainToImmutable(), Options.UseNullableReferenceTypes);
    }

    private AvroSchema? Logical(ILogicalTypeSyntax syntax, string? containingNamespace)
    {
        if (syntax is DecimalLogicalTypeSyntax decimalSyntax)
        {
            var tracker = TrackDiagnostics();
            var precision = decimalSyntax.PrecisionLiteralToken.Value is int precisionValue
                ? precisionValue
                : Invalid(0, AvroDiagnostic.InvalidIdlDecimalPrecision(decimalSyntax.PrecisionLiteralToken.SourceSpan));
            var scale = decimalSyntax.ScaleLiteralToken.Value is int scaleValue
                ? scaleValue
                : Invalid(0, AvroDiagnostic.InvalidIdlDecimalScale(decimalSyntax.ScaleLiteralToken.SourceSpan));
            if (tracker.HasNewDiagnostics)
                return null;
            var properties = ImmutableSortedDictionary<string, JsonElement>.Empty
                .Add("precision", JsonSerializer.SerializeToElement(precision))
                .Add("scale", JsonSerializer.SerializeToElement(scale));
            var bytes = AvroSchema.Bytes with { Properties = properties };

            return LogicalSchema.Create(LogicalTypeNames.Decimal, bytes, Options.GenerationTarget);
        }

        if (syntax is not LogicalTypeSyntax logical)
        {
            return Invalid<AvroSchema?>(null, AvroDiagnostic.InvalidIdlLogicalType(syntax.GetSourceSpan(), syntax.SyntaxKind));
        }

        return logical.LogicalTypeNameKeyword.SyntaxKind switch
        {
            SyntaxKind.DateKeyword => LogicalSchema.Create(LogicalTypeNames.Date, AvroSchema.Int, Options.GenerationTarget),
            SyntaxKind.TimeMsKeyword => LogicalSchema.Create(LogicalTypeNames.TimeMillis, AvroSchema.Int, Options.GenerationTarget),
            SyntaxKind.TimestampMsKeyword => LogicalSchema.Create(LogicalTypeNames.TimestampMillis, AvroSchema.Long, Options.GenerationTarget),
            SyntaxKind.LocalTimestampMsKeyword => LogicalSchema.Create(LogicalTypeNames.LocalTimestampMillis, AvroSchema.Long, Options.GenerationTarget),
            SyntaxKind.UuidKeyword => LogicalSchema.Create(LogicalTypeNames.Uuid, AvroSchema.String, Options.GenerationTarget),
            _ => Invalid<AvroSchema?>(null, AvroDiagnostic.InvalidIdlLogicalType(logical.LogicalTypeNameKeyword.SourceSpan, syntax.SyntaxKind))
        };
    }

    private ProtocolSchema? Protocol(ProtocolDeclarationSyntax syntax, string? containingNamespace)
    {
        var tracker = TrackDiagnostics();
        var schemaName = GetSchemaName(syntax, containingNamespace);
        if (tracker.HasNewDiagnostics) return null;
        if (IsInRecursionScope(schemaName))
            return Invalid<ProtocolSchema?>(null, AvroDiagnostic.RecursiveDefinition(syntax.Name.GetSourceSpan(), schemaName));

        using var scope = EnterRecursionScope(schemaName);
        var documentation = syntax.GetDocumentation();
        var types = ProtocolTypes(syntax.Types, schemaName.Namespace);
        var messages = ProtocolMessages(syntax.Messages, schemaName.Namespace);
        var properties = syntax.GetProtocolProperties();
        if (types.IsDefault || messages.IsDefault || tracker.HasNewDiagnostics)
            return null;
        var protocol = new ProtocolSchema(schemaName, documentation, types, messages, properties);
        Declare(protocol, syntax.GetSourceSpan());
        return protocol;
    }

    private ImmutableArray<NamedSchema> ProtocolTypes(SyntaxList<ISchemaDeclarationSyntax> syntaxList, string? containingNamespace)
    {
        var schemas = ImmutableArray.CreateBuilder<NamedSchema>();
        var valid = true;
        foreach (var declaration in syntaxList.WithCancellation(_cancellationToken))
        {
            var schema = Schema(declaration, containingNamespace);
            if (schema is not null)
                schemas.Add(schema);
            else
                valid = false;
        }
        return valid ? schemas.DrainToImmutable() : default;
    }

    private ImmutableArray<ProtocolMessage> ProtocolMessages(SyntaxList<MessageDeclarationSyntax> syntaxList, string? containingNamespace)
    {
        var messages = ImmutableArray.CreateBuilder<ProtocolMessage>();
        var valid = true;
        foreach (var syntax in syntaxList.WithCancellation(_cancellationToken))
        {
            if (Message(syntax, containingNamespace) is { } message)
                messages.Add(message);
            else
                valid = false;
        }
        return valid ? messages.DrainToImmutable() : default;
    }

    private ProtocolMessage? Message(MessageDeclarationSyntax syntax, string? containingNamespace)
    {
        var tracker = TrackDiagnostics();
        var methodName = syntax.Name.FullName.ToValidName();
        var documentation = syntax.GetDocumentation();
        var requestParameters = ProtocolRequestParameters(syntax.Parameters, containingNamespace);
        var response = ProtocolResponse(syntax.Type, containingNamespace);
        var errors = ProtocolErrors(syntax.ThrowsErrorClause, containingNamespace);
        var oneWay = syntax.OneWayClause is not null ? true : default(bool?);
        if (oneWay is true && response is not null && !errors.IsDefault && (response.Type.Type is not SchemaType.Null || errors.Length > 0))
            Report(AvroDiagnostic.InvalidIdlOneWayMessage(syntax.OneWayClause!.OneWayKeyword.SourceSpan, syntax.Name.FullName));
        if (requestParameters.IsDefault || response is null || errors.IsDefault || tracker.HasNewDiagnostics)
            return null;
        return new ProtocolMessage(methodName, documentation, requestParameters, response, errors, oneWay);
    }

    private ImmutableArray<ProtocolRequestParameter> ProtocolRequestParameters(SeparatedSyntaxList<ParameterDeclarationSyntax> syntaxList, string? containingNamespace)
    {
        var parameters = ImmutableArray.CreateBuilder<ProtocolRequestParameter>();
        var valid = true;
        foreach (var syntax in syntaxList.WithCancellation(_cancellationToken))
        {
            if (ProtocolRequestParameter(syntax, containingNamespace) is { } parameter)
                parameters.Add(parameter);
            else
                valid = false;
        }
        return valid ? parameters.DrainToImmutable() : default;
    }

    private ProtocolRequestParameter? ProtocolRequestParameter(ParameterDeclarationSyntax syntax, string? containingNamespace)
    {
        var name = syntax.Name.FullName.ToValidName();
        var defaultJson = syntax.DefaultValueClause?.JsonValue.ToOptionalJsonElement();
        var type = Type(syntax.Type, containingNamespace, defaultJson: defaultJson);
        var documentation = syntax.GetDocumentation();
        if (type is null) return null;
        var underlyingType = type is UnionSchema union ? union.UnderlyingSchema : type;
        var @default = type.GetValue(defaultJson);
        return new ProtocolRequestParameter(name, type, underlyingType, documentation, defaultJson, @default);
    }

    private ProtocolResponse? ProtocolResponse(ITypeSyntax syntax, string? containingNamespace)
    {
        var type = Type(syntax, containingNamespace);
        if (type is null) return null;
        var underlyingType = type is UnionSchema union ? union.UnderlyingSchema : type;
        return new ProtocolResponse(type, underlyingType);
    }

    private ImmutableArray<AvroSchema> ProtocolErrors(ThrowsErrorClauseSyntax? syntax, string? containingNamespace)
    {
        if (syntax is null) return ImmutableArray<AvroSchema>.Empty;
        var parameters = ImmutableArray.CreateBuilder<AvroSchema>();
        var valid = true;
        foreach (var error in syntax.Errors.WithCancellation(_cancellationToken))
        {
            var schema = Type(error, containingNamespace);
            if (schema is not null)
                parameters.Add(schema);
            else
                valid = false;
        }
        return valid ? parameters.DrainToImmutable() : default;
    }

    private T Invalid<T>(T @default, AvroDiagnostic diagnostic)
    {
        Report(diagnostic);
        return @default;
    }

    private string? GetEnumDefault(EnumDeclarationSyntax syntax) =>
        syntax.DefaultValue is { } clause
            ? GetString(clause.JsonValue, "Enum default value", required: false)
            : null;

    private int GetFixedSize(FixedDeclarationSyntax syntax) =>
        syntax.SizeLiteralToken.Value is int value and > 0
            ? value
            : Invalid(0, AvroDiagnostic.InvalidIdlFixedSize(syntax.SizeLiteralToken.SourceSpan));

    private string? GetString(JsonValueSyntax syntax, string description, bool required)
    {
        if (syntax.JsonNode is null && !required)
            return null;
        if (syntax.JsonNode is JsonValue value && value.TryGetValue<string>(out var result) && (!required || result is not null))
            return result;
        return Invalid<string?>(null, AvroDiagnostic.InvalidIdlDeclaration(syntax.GetSourceSpan(), $"{description} must be a string."));
    }

    private ImmutableArray<string> GetAliases(IDeclarationSyntax syntax)
    {
        if (syntax.Annotations.OfType<AliasesAnnotationSyntax>().LastOrDefault() is not { } annotation)
            return ImmutableArray<string>.Empty;
        if (annotation.JsonValue.JsonNode is not JsonArray array)
            return Invalid(default(ImmutableArray<string>), AvroDiagnostic.InvalidIdlDeclaration(annotation.JsonValue.GetSourceSpan(), "Aliases annotation value must be an array of strings."));

        var builder = ImmutableArray.CreateBuilder<string>(array.Count);
        foreach (var node in array.WithCancellation(_cancellationToken))
        {
            if (node is not JsonValue value || !value.TryGetValue<string>(out var result))
                return Invalid(default(ImmutableArray<string>), AvroDiagnostic.InvalidIdlDeclaration(annotation.JsonValue.GetSourceSpan(), "Aliases annotation value must be an array of strings."));
            builder.Add(result);
        }
        return builder.MoveToImmutable();
    }

    private SchemaName GetSchemaName(IDeclarationSyntax syntax, string? containingNamespace)
    {
        var name = syntax.Name.FullName;
        if (name.TrySplitQualifiedName(out name, out var ns))
            return new SchemaName(name, ns);
        if (syntax.Annotations.OfType<NamespaceAnnotationSyntax>().LastOrDefault() is { } annotation)
        {
            var tracker = TrackDiagnostics();
            containingNamespace = GetString(annotation.JsonValue, "Namespace annotation value", required: true);
            if (tracker.HasNewDiagnostics) return default;
        }
        return new SchemaName(name, containingNamespace);
    }

    private AvroSchema? Named(NamedTypeSyntax syntax, string? containingNamespace)
    {
        syntax.Name.FullName.TrySplitQualifiedName(out var name, out var ns);
        if (string.IsNullOrWhiteSpace(name) || ns is "")
            return Invalid<AvroSchema?>(null, AvroDiagnostic.InvalidSchemaValue(sourceText.GetSourceSpan(), "Argument has an invalid name format: 'cannot start or end with a dot'"));
        return Reference(new SchemaName(name, ns), containingNamespace, syntax.Name.GetSourceSpan());
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
