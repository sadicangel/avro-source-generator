using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.UnitTests;

public sealed class UnionMemberSchemaTests
{
    [Fact]
    public void Prepared_members_are_ordered_and_distinct_by_csharp_name_without_null()
    {
        var first = new RecordSchema(new SchemaName("First"), null, [], [], ImmutableSortedDictionary<string, JsonElement>.Empty);
        var second = first with
        {
            SchemaName = new SchemaName("Second"),
            CSharpName = new CSharpName("Second")
        };
        AvroSchema[] schemas = [second, AvroSchema.Null, first, second with { Documentation = "Duplicate CLR name" }];
        var members = schemas.Canonicalize(includeNull: false).ToImmutableArray();
        var name = new SchemaName("Choice");
        var csharpName = new CSharpName("Choice");
        var normalized = new UnionObjectSchema(name, csharpName, members).MemberSchemas;

        Assert.Equal([first, second], normalized);
    }

    [Theory]
    [InlineData(LanguageFeatures.CSharp14)]
    public void Binding_preserves_original_branch_order_and_nullable_field_documentation(LanguageFeatures features)
    {
        var file = AvroFile.Parse(
            new SourceText(
                "envelope.avsc",
                """
                {
                  "type": "record", "name": "Envelope",
                  "fields": [{ "name": "choice", "type": [
                    { "type": "record", "name": "Second", "fields": [{ "name": "hash", "type": "Hash" }] },
                    "null",
                    { "type": "record", "name": "First", "fields": [] }
                  ] }]
                }
                """),
            new AvroParseOptions(GenerationTarget.Modern, features),
            TestContext.Current.CancellationToken);
        var dependency = AvroFile.Parse(
            new SourceText(
                "hash.avsc",
                """{"type":"fixed","name":"Hash","size":16}"""),
            file.ParseOptions,
            TestContext.Current.CancellationToken);
        var symbols = SymbolTable.FromFiles([file, dependency], TestContext.Current.CancellationToken);
        var bound = BoundAvroFile.Bind(LinkedAvroFile.Link(file, symbols, TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);
        var field = Assert.Single(Assert.IsType<RecordSchema>(bound.Declarations.Single(schema => schema.SchemaName.Name == "Envelope")).Fields);
        var union = Assert.IsType<UnionSchema>(field.Type);
        var normalized = Assert.IsType<UnionObjectSchema>(union.UnderlyingSchema).MemberSchemas;

        Assert.Equal(["Second", "null", "First"], union.Schemas.Select(member => member.SchemaName.Name));
        Assert.Equal(["First", "Second"], normalized.Select(member => member.SchemaName.Name));
        Assert.True(field.AllowsNull);
        Assert.True(field.Type.CSharpName.HasNullableAnnotation);
        Assert.Contains("<see langword=\"null\"/>", field.Remarks);
        Assert.Contains("cref=\"First\"", field.Remarks);
        Assert.Contains("cref=\"Second\"", field.Remarks);
        var second = Assert.IsType<RecordSchema>(union.Schemas[0]);
        Assert.Equal(CSharpName.ByteArray, Assert.Single(second.Fields).Type.CSharpName);
        Assert.Same(second, normalized[1]);
        Assert.Same(union.Schemas[2], normalized[0]);
    }
}
