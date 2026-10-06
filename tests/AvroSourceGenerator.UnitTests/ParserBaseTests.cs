using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.UnitTests;

public sealed class ParserBaseTests
{
    [Fact]
    public void References_use_the_latest_declaration_and_keep_declaration_order()
    {
        var context = new TestParser(new AvroParseOptions(GenerationTarget.Modern, true));
        var first = Record("Shared") with { CSharpName = new CSharpName("First") };
        var latest = Record("Shared") with { CSharpName = new CSharpName("Latest") };
        context.Declare(first, SourceSpan.None);
        context.Declare(latest, SourceSpan.None);

        var reference = context.Reference(new SchemaName("Shared"), "Example", SourceSpan.None);

        Assert.Equal(latest.CSharpName, reference.CSharpName);
        var result = context.Complete(Source, latest, [], []);
        Assert.Equal([first, latest], result.Declarations);
        Assert.Empty(result.References);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Variant_replacement_preserves_the_latest_duplicate(bool replaceLatest)
    {
        var context = new TestParser(new AvroParseOptions(GenerationTarget.Modern, true));
        var first = Record("Shared") with { CSharpName = new CSharpName("First") };
        var latest = Record("Shared") with { CSharpName = new CSharpName("Latest") };
        var other = Record("Other");
        context.Declare(first, SourceSpan.None);
        context.Declare(latest, SourceSpan.None);
        context.Declare(other, SourceSpan.None);
        var replaced = replaceLatest ? latest : first;
        var union = UnionSchema.Create([replaced, other], true);

        context.ApplyFieldType(union, "Choice", new SchemaName("Container", "Example"));

        var result = context.Complete(Source, union, [], []);
        var updated = Assert.IsType<RecordSchema>(result.Declarations[replaceLatest ? 1 : 0]);
        Assert.NotNull(updated.InheritsFrom);
        Assert.NotSame(replaced, updated);
        Assert.Equal(latest.CSharpName, context.Reference(new SchemaName("Shared"), "Example", SourceSpan.None).CSharpName);
        Assert.Same(replaceLatest ? first : latest, result.Declarations[replaceLatest ? 0 : 1]);
    }

    [Fact]
    public void Replacement_rejects_an_equal_but_unregistered_instance()
    {
        var context = new TestParser(new AvroParseOptions(GenerationTarget.Modern, true));
        var original = Record("Shared");
        context.Declare(original, SourceSpan.None);
        var clone = original with { };

        Assert.Equal(original, clone);
        Assert.Throws<InvalidOperationException>(() => context.Replace(clone, clone with { InheritsFrom = new CSharpName("IVariant") }));
        Assert.Same(original, Assert.Single(context.Complete(Source, original, [], []).Declarations));
    }

    [Fact]
    public void Recursive_references_record_dependencies_without_external_references()
    {
        var context = new TestParser(new AvroParseOptions(GenerationTarget.Modern, true));
        var record = Record("Node");
        using (context.EnterRecursionScope(record.SchemaName))
        {
            Assert.Equal(record.SchemaName, context.Reference(new SchemaName("Node"), "Example", SourceSpan.None).SchemaName);
            context.Declare(record, SourceSpan.None);
        }

        var result = context.Complete(Source, record, [], []);
        Assert.Empty(result.References);
        Assert.Equal([record.SchemaName], result.Dependencies[record.SchemaName]);
    }

    private static RecordSchema Record(string name) => new RecordSchema(new SchemaName(name, "Example"), null, [], [], ImmutableSortedDictionary<string, JsonElement>.Empty);

    private static SourceText Source { get; } = new SourceText("test.avsc", "{}");

    private sealed class TestParser(AvroParseOptions options) : AvxxParser(options)
    {
        public new void Declare(TopLevelSchema schema, SourceSpan span) => base.Declare(schema, span);
        public new void Replace(TopLevelSchema original, TopLevelSchema replacement) => base.Replace(original, replacement);
        public new AvroSchema Reference(SchemaName name, string? containingNamespace, SourceSpan span) => base.Reference(name, containingNamespace, span);

        public void ApplyFieldType(AvroSchema type, FieldName name, SchemaName containingSchema) =>
            _ = base.ResolveFieldType(type, name, containingSchema, out _, out _);

        private RecursionScope EnterScope(SchemaName name) => base.EnterRecursionScope(name);
        public new TestScope EnterRecursionScope(SchemaName name) => new TestScope(this, name);

        public readonly ref struct TestScope(TestParser parser, SchemaName name)
        {
            private readonly RecursionScope _scope = parser.EnterScope(name);
            public void Dispose() => _scope.Dispose();
        }

        public AvroFile Complete(SourceText source, AvroSchema root, ImmutableArray<AvroImport> imports, ImmutableArray<AvroDiagnostic> diagnostics) => new AvroFile(source, root, [.. Declarations], [.. DeclarationSpans], GetReferences(), GetReferenceSpans(), GetDependencies(), imports, diagnostics, Options);
    }
}
