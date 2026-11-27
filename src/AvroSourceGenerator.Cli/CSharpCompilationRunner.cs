using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using AvroSourceGenerator.Compiler.Configuration;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Serialization;
using AvroSourceGenerator.Templating;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AvroSourceGenerator.Cli;

internal static class CSharpCompilationRunner
{
    public static async Task<int> RunAsync(
        string sourcePath,
        string destination,
        string format,
        string? projectPath,
        string? framework,
        string? typeName,
        bool includeDocumentation,
        bool force,
        bool check,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        if (force && check)
        {
            await error.WriteLineAsync("--force and --check cannot be combined.");
            return 2;
        }

        try
        {
            destination = Path.GetFullPath(destination);
        }
        catch (ArgumentException exception)
        {
            await error.WriteLineAsync($"avro-sg: {exception.Message}");
            return 2;
        }

        CSharpInputCompilation input;
        try
        {
            input = await CSharpCompilationLoader.LoadAsync(sourcePath, projectPath, framework, cancellationToken);
        }
        catch (CSharpCompilationLoader.InputException exception)
        {
            await error.WriteLineAsync($"avro-sg: {exception.Message}");
            return 2;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            await error.WriteLineAsync($"avro-sg: {exception.Message}");
            return 1;
        }

        var failed = false;
        foreach (var diagnostic in input.Compilation.GetDiagnostics(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
                await error.WriteLineAsync(diagnostic.ToString());
            failed |= diagnostic.Severity == DiagnosticSeverity.Error;
        }
        if (failed)
            return 1;

        var root = (CompilationUnitSyntax)await input.SyntaxTree.GetRootAsync(cancellationToken);
        var model = input.Compilation.GetSemanticModel(input.SyntaxTree);
        var generator = new SchemaGenerator(model, includeDocumentation);
        var declarations = generator.GetDeclaredTypes(root, cancellationToken);
        if (typeName is not null)
        {
            declarations = [.. declarations.Where(symbol => symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) == typeName)];
            if (declarations.IsEmpty)
            {
                await error.WriteLineAsync($"avro-sg: Type '{typeName}' is not declared in '{sourcePath}'.");
                return 2;
            }
        }

        ImmutableArray<TopLevelSchema> schemas;
        try
        {
            schemas = generator.Generate(declarations, cancellationToken);
        }
        catch (SchemaGenerator.MappingException exception)
        {
            var span = exception.Symbol.Locations.FirstOrDefault(location => location.IsInSource)?.GetLineSpan();
            var location = span is { } source
                ? $"{source.Path}({source.StartLinePosition.Line + 1},{source.StartLinePosition.Character + 1}): "
                : string.Empty;
            await error.WriteLineAsync($"{location}avro-sg: {exception.Message}");
            return 1;
        }

        if (typeName is not null && schemas.IsEmpty)
        {
            await error.WriteLineAsync($"avro-sg: Type '{typeName}' cannot be generated as a named Avro schema.");
            return 2;
        }

        var schemaIndex = new Dictionary<SchemaName, TopLevelSchema>();
        foreach (var schema in generator.NamedSchemas)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!schemaIndex.TryAdd(schema.SchemaName, schema))
            {
                await error.WriteLineAsync($"avro-sg: Multiple C# types map to Avro name '{schema.SchemaName}'.");
                return 1;
            }
        }

        return await GeneratedFileWriter.WriteAsync(destination, Render(), force, check, failed: false, emitUtf8Bom: false, output, error, cancellationToken);

        IEnumerable<RenderedSchema> Render()
        {
            foreach (var schema in schemas)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourceText = format == "avsc"
                    ? schema.ToAvjs(schemaIndex, new JsonWriterOptions { Indented = true })
                    : schema.ToAvdl(schemaIndex, cancellationToken);
                yield return new RenderedSchema($"{schema.SchemaName.FullName}.{format}", sourceText);
            }
        }
    }
}

internal sealed class SchemaGenerator(SemanticModel model, bool includeDocumentation)
{
    private static readonly SymbolDisplayFormat s_namespaceFormat = new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
    private static readonly AvroSchema s_decimal = Logical("decimal", AvroSchema.Bytes with
    {
        Properties = AvroSchema.Bytes.Properties
            .Add("precision", JsonSerializer.SerializeToElement(57))
            .Add("scale", JsonSerializer.SerializeToElement(28)),
    });

    private readonly CollectionHelper _collectionHelper = new CollectionHelper(model);
    private readonly Dictionary<INamedTypeSymbol, NamedSchema> _schemas = [];
    private readonly List<SchemaName> _recursionStack = [];

    public IEnumerable<NamedSchema> NamedSchemas => _schemas.Values;

    public ImmutableArray<INamedTypeSymbol> GetDeclaredTypes(CompilationUnitSyntax syntax, CancellationToken cancellationToken)
    {
        var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var declarations = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
        foreach (var node in syntax.DescendantNodes(descendIntoChildren: static node =>
                     node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax or TypeDeclarationSyntax))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (node is BaseTypeDeclarationSyntax && model.GetDeclaredSymbol(node, cancellationToken) is INamedTypeSymbol type && seen.Add(type))
                declarations.Add(type);
        }
        return declarations.DrainToImmutable();
    }

    public ImmutableArray<TopLevelSchema> Generate(ImmutableArray<INamedTypeSymbol> declarations, CancellationToken cancellationToken)
    {
        var schemas = ImmutableArray.CreateBuilder<TopLevelSchema>();

        foreach (var symbol in declarations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Type(symbol, cancellationToken) is TopLevelSchema schema)
                schemas.Add(schema);
        }

        return schemas.DrainToImmutable();
    }

    private AvroSchema? Type(ITypeSymbol symbol, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (symbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
            return MakeNullable(Type(nullable.TypeArguments[0], cancellationToken));

        var type = symbol switch
        {
            { SpecialType: SpecialType.System_Boolean } => AvroSchema.Boolean,
            { SpecialType: SpecialType.System_Byte or SpecialType.System_Char or SpecialType.System_Int16 or SpecialType.System_Int32 or SpecialType.System_UInt16 or SpecialType.System_UInt32 } => AvroSchema.Int,
            { SpecialType: SpecialType.System_Int64 or SpecialType.System_UInt64 } => AvroSchema.Long,
            { SpecialType: SpecialType.System_Single } => AvroSchema.Float,
            { SpecialType: SpecialType.System_Double } => AvroSchema.Double,
            { SpecialType: SpecialType.System_String } => AvroSchema.String,
            { SpecialType: SpecialType.System_Decimal } => s_decimal,
            { SpecialType: SpecialType.System_DateTime } => Logical("timestamp-millis", AvroSchema.Long),
            { SpecialType: SpecialType.System_Object or SpecialType.System_Void } => null,
            { } when GetLogicalType(symbol) is { } logical => logical,
            IArrayTypeSymbol { IsSZArray: true, ElementType.SpecialType: SpecialType.System_Byte } => AvroSchema.Bytes,
            { } when IsMapLike(symbol, out var elementType) => elementType is null ? null : Map(elementType, cancellationToken),
            { } when IsArrayLike(symbol, out var elementType) => Array(elementType, cancellationToken),
            INamedTypeSymbol { TypeKind: TypeKind.Enum } @enum => Enum(@enum, cancellationToken),
            INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct, IsAbstract: false, IsStatic: false, IsUnion: false, IsRefLikeType: false } @class
                when !IsFrameworkType(@class) => Record(@class, cancellationToken),
            INamedTypeSymbol { TypeKind: TypeKind.Class, IsAbstract: true, IsStatic: false, IsClosed: true, IsUnion: false } @class => Union(ConcreteDescendants(@class, cancellationToken), cancellationToken),
            _ => null
        };

        return symbol?.NullableAnnotation is NullableAnnotation.Annotated ? MakeNullable(type) : type;
    }

    private static AvroSchema? MakeNullable(AvroSchema? type) => type switch
    {
        null => null,
        UnionSchema union when union.Schemas.Any(schema => schema.SchemaType == SchemaType.Null) => union,
        UnionSchema union => UnionSchema.Create([AvroSchema.Null, .. union.Schemas], useNullableReferenceTypes: true),
        _ => UnionSchema.Create([AvroSchema.Null, type], useNullableReferenceTypes: true),
    };

    private static AvroSchema? GetLogicalType(ITypeSymbol symbol) => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) switch
    {
        "global::System.Guid" => Logical("uuid", AvroSchema.String),
        "global::System.DateOnly" => Logical("date", AvroSchema.Int),
        "global::System.TimeOnly" => Logical("time-millis", AvroSchema.Int),
        "global::System.DateTimeOffset" => Logical("timestamp-millis", AvroSchema.Long),
        _ => null,
    };

    private static AvroSchema Logical(string name, AvroSchema storage) =>
        LogicalSchema.Create(name, storage, GenerationTarget.Modern);

    private static bool IsFrameworkType(INamedTypeSymbol symbol)
    {
        var assembly = symbol.ContainingAssembly.Identity;
        return assembly.IsStrongName && !symbol.Locations.Any(location => location.IsInSource) &&
            (assembly.Name is "mscorlib" or "System" || assembly.Name.StartsWith("System.", StringComparison.Ordinal));
    }

    private bool IsArrayLike(ITypeSymbol symbol, [MaybeNullWhen(false)] out ITypeSymbol elementType)
    {
        if (symbol is IArrayTypeSymbol { IsSZArray: true } array)
        {
            elementType = array.ElementType;
            return true;
        }

        var candidates = symbol is INamedTypeSymbol named ? symbol.AllInterfaces.Prepend(named) : symbol.AllInterfaces;
        var interfaces = candidates.Where(_collectionHelper.IsArrayLike).ToArray();
        elementType = interfaces.Length == 1 ? interfaces[0].TypeArguments[0] : null;
        return elementType != null;
    }

    private AvroSchema? Array(ITypeSymbol symbol, CancellationToken cancellationToken) => Type(symbol, cancellationToken) is { } item
        ? new ArraySchema(item, Documentation: null, Properties: ImmutableSortedDictionary<string, JsonElement>.Empty)
        : null;

    private bool IsMapLike(ITypeSymbol symbol, out ITypeSymbol? elementType)
    {
        var candidates = symbol is INamedTypeSymbol named ? symbol.AllInterfaces.Prepend(named) : symbol.AllInterfaces;
        var interfaces = candidates.Where(_collectionHelper.IsMapLike).ToArray();
        elementType = null;
        if (interfaces is not [var first, ..])
            return false;

        if (first.TypeArguments[0].SpecialType == SpecialType.System_String && interfaces.All(candidate =>
                candidate.TypeArguments[0].SpecialType == SpecialType.System_String &&
                SymbolEqualityComparer.IncludeNullability.Equals(candidate.TypeArguments[1], first.TypeArguments[1])))
            elementType = first.TypeArguments[1];
        return true;
    }

    private AvroSchema? Map(ITypeSymbol symbol, CancellationToken cancellationToken) => Type(symbol, cancellationToken) is { } value
        ? new MapSchema(value, Documentation: null, Properties: ImmutableSortedDictionary<string, JsonElement>.Empty)
        : null;

    private AvroSchema? Union(IEnumerable<INamedTypeSymbol> symbols, CancellationToken cancellationToken)
    {
        var schemas = ImmutableArray.CreateBuilder<AvroSchema>();
        foreach (var symbol in symbols)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Type(symbol, cancellationToken) is not { } schema)
                return null;
            schemas.Add(schema);
        }
        return UnionSchema.Create(schemas.DrainToImmutable(), useNullableReferenceTypes: true);
    }

    private static IEnumerable<INamedTypeSymbol> ConcreteDescendants(INamedTypeSymbol symbol, CancellationToken cancellationToken)
    {
        var info = symbol.GetClosedDerivedTypeInfo(cancellationToken);
        if (!info.IsComplete)
            throw new MappingException(symbol, $"Cannot generate an Avro union for the incomplete hierarchy '{symbol}'.");

        foreach (var derived in info.ClosedDerivedTypes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (derived.IsClosed)
            {
                foreach (var descendant in ConcreteDescendants(derived, cancellationToken))
                    yield return descendant;
            }
            else if (!derived.IsAbstract)
            {
                yield return derived;
            }
        }
    }

    private AvroSchema Record(INamedTypeSymbol symbol, CancellationToken cancellationToken)
    {
        if (_schemas.TryGetValue(symbol, out var schema)) return schema;
        var schemaName = GetSchemaName(symbol);
        if (_recursionStack.Contains(schemaName)) return new AvroSchemaReference(schemaName);
        using var scope = EnterDeclarationScope(schemaName);
        var fields = ImmutableArray.CreateBuilder<Field>();
        foreach (var property in symbol.GetMembers().OfType<IPropertySymbol>().Where(IsEligibleProperty))
        {
            fields.Add(Field(property, cancellationToken));
        }

        return _schemas[symbol] = new RecordSchema(
            SchemaName: schemaName,
            Documentation: Documentation(symbol, cancellationToken),
            Aliases: [],
            Fields: fields.DrainToImmutable(),
            Properties: ImmutableSortedDictionary<string, JsonElement>.Empty);
    }

    private static bool IsEligibleProperty(IPropertySymbol property) =>
        property is { IsStatic: false, IsAbstract: false, IsIndexer: false, DeclaredAccessibility: Accessibility.Public };

    private Field Field(IPropertySymbol symbol, CancellationToken cancellationToken)
    {
        var fieldName = new MemberName(GetUnreserved(symbol.Name), symbol.Name);
        var fieldType = Type(symbol.Type, cancellationToken) ?? throw new MappingException(symbol,
            $"Cannot map property '{symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)}' of type '{symbol.Type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)}' to Avro.");
        return new Field(
            Name: fieldName,
            Type: fieldType,
            UnderlyingType: fieldType.GetUnderlyingOrSelf(),
            Documentation: Documentation(symbol, cancellationToken),
            Aliases: [],
            Default: null,
            Order: null,
            Properties: ImmutableSortedDictionary<string, JsonElement>.Empty,
            Remarks: null);
    }

    private AvroSchema Enum(INamedTypeSymbol symbol, CancellationToken cancellationToken)
    {
        if (_schemas.TryGetValue(symbol, out var schema)) return schema;
        var schemaName = GetSchemaName(symbol);
        if (_recursionStack.Contains(schemaName)) return new AvroSchemaReference(schemaName);
        using var scope = EnterDeclarationScope(schemaName);
        var symbols = ImmutableArray.CreateBuilder<MemberName>();
        foreach (var member in symbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (member is IFieldSymbol { HasConstantValue: true } field)
                symbols.Add(new MemberName(GetUnreserved(field.Name), field.Name));
        }

        return _schemas[symbol] = new EnumSchema(
            SchemaName: schemaName,
            Documentation: Documentation(symbol, cancellationToken),
            Aliases: [],
            Symbols: symbols.DrainToImmutable(),
            Default: null,
            Properties: ImmutableSortedDictionary<string, JsonElement>.Empty);
    }

    private static SchemaName GetSchemaName(ISymbol symbol)
    {
        var name = GetUnreserved(symbol.Name);
        var @namespace = symbol.ContainingNamespace is { IsGlobalNamespace: false } ns ? ns.ToDisplayString(s_namespaceFormat) : null;
        return new SchemaName(name, @namespace);

    }

    private static string GetUnreserved(string name) => name.StartsWith('@') ? name[1..] : name;

    private string? Documentation(ISymbol symbol, CancellationToken cancellationToken)
    {
        if (!includeDocumentation)
            return null;

        var element = GetDocumentationXml(symbol, cancellationToken)?.Element("summary");
        if (element is null && symbol is IPropertySymbol { ContainingType.IsRecord: true } property &&
            property.ContainingType.DeclaringSyntaxReferences.Any(reference =>
                reference.GetSyntax(cancellationToken) is RecordDeclarationSyntax { ParameterList: { } parameters } &&
                parameters.Parameters.Any(parameter => parameter.Identifier.ValueText == property.Name)))
        {
            element = GetDocumentationXml(property.ContainingType, cancellationToken)?.Elements("param")
                .FirstOrDefault(parameter => (string?)parameter.Attribute("name") == property.Name);
        }

        if (element is null)
            return null;

        foreach (var reference in element.Descendants().Where(node => node.IsEmpty).ToArray())
        {
            var text = (string?)reference.Attribute("name") ?? (string?)reference.Attribute("langword") ?? (string?)reference.Attribute("cref");
            if (text is not null)
                reference.ReplaceWith(text.Length > 2 && text[1] == ':' ? text[2..] : text);
        }
        var result = string.Join(' ', element.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return result.Length == 0 ? null : result;
    }

    private static XElement? GetDocumentationXml(ISymbol symbol, CancellationToken cancellationToken)
    {
        var xml = symbol.GetDocumentationCommentXml(cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(xml))
            return null;

        try
        {
            return XDocument.Parse(xml).Root;
        }
        catch (XmlException)
        {
            return null;
        }
    }

    private RecursionScope EnterDeclarationScope(SchemaName schemaName) => new RecursionScope(this, schemaName);

    internal sealed class MappingException(ISymbol symbol, string message) : Exception(message)
    {
        public ISymbol Symbol { get; } = symbol;
    }

    private readonly ref struct RecursionScope : IDisposable
    {
        private readonly List<SchemaName> _recursionStack;
        private readonly SchemaName _schemaName;

        public RecursionScope(SchemaGenerator generator, SchemaName schemaName)
        {
            _recursionStack = generator._recursionStack;
            _schemaName = schemaName;
            _recursionStack.Add(_schemaName);
        }
        public void Dispose()
        {
            if (_recursionStack is not [.., var popped] || popped != _schemaName)
                throw new InvalidOperationException("Recursion stack corrupted.");

            _recursionStack.RemoveAt(_recursionStack.Count - 1);
        }
    }
}

internal sealed class CollectionHelper(SemanticModel model)
{
    private const string EnumerableName = "System.Collections.Generic.IEnumerable`1";
    private const string DictionaryName = "System.Collections.Generic.IDictionary`2";
    private const string ReadOnlyDictionaryName = "System.Collections.Generic.IReadOnlyDictionary`2";

    private INamedTypeSymbol EnumerableType { get; } = model.Compilation.GetTypeByMetadataName(EnumerableName) ?? throw new InvalidOperationException($"{EnumerableName} undefined");
    private INamedTypeSymbol DictionaryType { get; } = model.Compilation.GetTypeByMetadataName(DictionaryName) ?? throw new InvalidOperationException($"{DictionaryName} undefined");
    private INamedTypeSymbol ReadOnlyDictionaryType { get; } = model.Compilation.GetTypeByMetadataName(ReadOnlyDictionaryName) ?? throw new InvalidOperationException($"{ReadOnlyDictionaryName} undefined");

    public bool IsArrayLike(INamedTypeSymbol type) => SymbolEqualityComparer.Default.Equals(EnumerableType, type.OriginalDefinition);
    public bool IsMapLike(INamedTypeSymbol type) => SymbolEqualityComparer.Default.Equals(DictionaryType, type.OriginalDefinition) || SymbolEqualityComparer.Default.Equals(ReadOnlyDictionaryType, type.OriginalDefinition);
}
