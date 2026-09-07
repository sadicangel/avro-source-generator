using System.Collections.Immutable;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Tests;

public sealed class ParserContextTests
{
    [Fact]
    public void References_use_the_latest_declaration_and_keep_declaration_order()
    {
        var context = new ParserContext(new AvroParseOptions(TargetProfile.Modern, true));
        var first = Record("Shared") with { CSharpName = new CSharpName("First") };
        var latest = Record("Shared") with { CSharpName = new CSharpName("Latest") };
        context.Declare(first);
        context.Declare(latest);

        var reference = context.Reference(new SchemaName("Shared"), "Example");

        Assert.Equal(latest.CSharpName, reference.CSharpName);
        var result = context.Complete(latest);
        Assert.Equal([first, latest], result.Declarations);
        Assert.Empty(result.References);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Variant_replacement_preserves_the_latest_duplicate(bool replaceLatest)
    {
        var context = new ParserContext(new AvroParseOptions(TargetProfile.Modern, true));
        var first = Record("Shared") with { CSharpName = new CSharpName("First") };
        var latest = Record("Shared") with { CSharpName = new CSharpName("Latest") };
        var other = Record("Other");
        context.Declare(first);
        context.Declare(latest);
        context.Declare(other);
        var replaced = replaceLatest ? latest : first;
        var union = UnionSchema.Create([replaced, other], true);

        context.ResolveFieldType(union, "Choice", new SchemaName("Container", "Example"), out _, out _);

        var result = context.Complete(union);
        var updated = Assert.IsType<RecordSchema>(result.Declarations[replaceLatest ? 1 : 0]);
        Assert.NotNull(updated.InheritsFrom);
        Assert.NotSame(replaced, updated);
        Assert.Equal(latest.CSharpName, context.Reference(new SchemaName("Shared"), "Example").CSharpName);
        Assert.Same(replaceLatest ? first : latest, result.Declarations[replaceLatest ? 0 : 1]);
    }

    [Fact]
    public void Recursive_references_record_dependencies_without_external_references()
    {
        var context = new ParserContext(new AvroParseOptions(TargetProfile.Modern, true));
        var record = Record("Node");
        using (context.EnterRecursionScope(record.SchemaName))
        {
            Assert.Equal(record.SchemaName, context.Reference(new SchemaName("Node"), "Example").SchemaName);
            context.Declare(record);
        }

        var result = context.Complete(record);
        Assert.Empty(result.References);
        Assert.Equal([record.SchemaName], result.Dependencies[record.SchemaName]);
    }

    private static RecordSchema Record(string name) =>
        new(new SchemaName(name, "Example"), null, [], [], ImmutableSortedDictionary<string, System.Text.Json.JsonElement>.Empty);
}
