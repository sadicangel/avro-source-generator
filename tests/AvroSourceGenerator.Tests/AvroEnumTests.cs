namespace AvroSourceGenerator.Tests;

public class AvroEnumTests
{
    [Theory]
    [InlineData("\"EnumName\""), InlineData("\"enum_name\"")]
    [InlineData("\"public\""), InlineData("\"string\"")]
    [InlineData("null"), InlineData("\"\""), InlineData("[]")]
    public Task Verify_Name(string name) => TestHelper.Verify($$"""
        {
            "type": "enum",
            "name": {{name}},
            "namespace": "SchemaNamespace",
            "symbols": []
        }
        """)
        .UseParameters(name);

    [Theory]
    [InlineData("null"), InlineData("\"EnumSchemaNamespace\"")]
    [InlineData("\"enum\""), InlineData("\"name1.enum.name2\"")]
    [InlineData("\"\""), InlineData("[]")]
    public Task Verify_Namespace(string @namespace) => TestHelper.Verify($$"""
        {
            "type": "enum",
            "name": "TestEnum",
            "namespace": {{@namespace}},
            "symbols": []
        }
        """)
        .UseParameters(@namespace);

    [Theory]
    [InlineData("null"), InlineData("\"\""), InlineData("\"Single line comment\""), InlineData("\"Multi\\nline\\ncomment\"")]
    [InlineData("1"), InlineData("[]"), InlineData("{}")]
    public Task Verify_Documentation(string doc) => TestHelper.Verify($$"""
        {
            "type": "enum",
            "name": "TestEnum",
            "namespace": "SchemaNamespace",
            "doc": {{doc}},
            "symbols": []
        }
        """)
        .UseParameters(doc);

    [Theory]
    [InlineData("null"), InlineData("[]"), InlineData("[\"Alias1\"]"), InlineData("[\"Alias1\", \"Alias2\"]")]
    [InlineData("\"not an array\""), InlineData("{}")]
    public Task Verify_Aliases(string aliases) => TestHelper.Verify($$"""
        {
            "type": "enum",
            "name": "TestEnum",
            "namespace": "SchemaNamespace",
            "aliases": {{aliases}},
            "symbols": []
        }
        """)
        .UseParameters(aliases);

    [Theory]
    [InlineData("[]"), InlineData("[\"A\"]"), InlineData("[\"B\", \"C\"]")]
    [InlineData("null"), InlineData("\"not an array\""), InlineData("{}")]
    public Task Verify_Symbols(string symbols) => TestHelper.Verify($$"""
        {
            "type": "enum",
            "name": "TestEnum",
            "namespace": "SchemaNamespace",
            "symbols": {{symbols}}
        }
        """)
        .UseParameters(symbols);
}
