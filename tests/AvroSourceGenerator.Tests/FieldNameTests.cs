using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Tests;

public sealed class FieldNameTests
{
    [Theory]
    [InlineData("ordinary", "ordinary")]
    [InlineData("int", "@int")]
    [InlineData("class", "@class")]
    public void Preserves_schema_name_and_exposes_valid_csharp_name(string schemaName, string csharpName)
    {
        FieldName name = schemaName;

        Assert.Equal(schemaName, name.SchemaName);
        Assert.Equal(csharpName, name.CSharpName);
        Assert.Equal(csharpName, name.ToString());
    }

    [Theory]
    [InlineData("""
        {"type":"record","name":"Container","fields":[{"name":"int","type":"string"}]}
        """, "int", "@int")]
    [InlineData("""
        schema Container;
        record Container { string class; }
        """, "class", "@class")]
    public void Parsers_preserve_keyword_field_names(string source, string schemaName, string csharpName)
    {
        var parsed = source.StartsWith('{')
            ? SchemaCompilerTestHelpers.ParseJson(source)
            : SchemaCompilerTestHelpers.ParseSource(source);
        var record = Assert.Single(parsed.Declarations.OfType<RecordSchema>());
        var field = Assert.Single(record.Fields);

        Assert.Equal(schemaName, field.Name.SchemaName);
        Assert.Equal(csharpName, field.Name.CSharpName);
    }
}
