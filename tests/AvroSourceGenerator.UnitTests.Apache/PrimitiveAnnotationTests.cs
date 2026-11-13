using System.Reflection;
using System.Text.Json;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Compiler.Configuration;
using AvroSourceGenerator.Compiler.Linking;
using AvroSourceGenerator.Schemas;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SourceText = AvroSourceGenerator.Text.SourceText;

namespace AvroSourceGenerator.UnitTests.Apache;

public sealed class PrimitiveAnnotationTests
{
    private static readonly AvroParseOptions s_options = new AvroParseOptions(GenerationTarget.Apache, LanguageFeatures.CSharp12);

    [Fact]
    public void Issue_190_preserves_the_primitive_type_annotation()
    {
        var json = SerializeBound(Parse("schema.avdl", "schema R; record R { @custom(42) string Value; }"));
        using var serialized = JsonDocument.Parse(json);
        var field = serialized.RootElement.GetProperty("fields")[0];
        Assert.Equal("string", field.GetProperty("type").GetProperty("type").GetString());
        Assert.Equal(42, field.GetProperty("type").GetProperty("custom").GetInt32());
        Assert.False(field.TryGetProperty("custom", out _));
        AssertApachePreserves(json);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("boolean")]
    [InlineData("int")]
    [InlineData("long")]
    [InlineData("float")]
    [InlineData("double")]
    [InlineData("bytes")]
    [InlineData("string")]
    public void Primitive_type_properties_survive_lowering_binding_and_Apache_serialization(string primitive)
    {
        var idl = $"schema R; record R {{ @custom(42) {primitive} Value; {primitive} Plain; }}";
        var file = Parse("schema.avdl", idl);
        var record = Assert.IsType<RecordSchema>(Assert.Single(file.Declarations));
        var annotated = Assert.IsType<PrimitiveSchema>(record.Fields[0].Type);
        var plain = Assert.IsType<PrimitiveSchema>(record.Fields[1].Type);
        Assert.NotSame(plain, annotated);
        Assert.Empty(plain.Properties);
        Assert.Equal(42, annotated.Properties["custom"].GetInt32());
        Assert.Empty(record.Fields[0].Properties);
        var json = SerializeBound(file);
        using var serialized = JsonDocument.Parse(json);
        Assert.Equal(42, serialized.RootElement.GetProperty("fields")[0].GetProperty("type").GetProperty("custom").GetInt32());
        Assert.Equal(primitive, serialized.RootElement.GetProperty("fields")[1].GetProperty("type").GetString());
        AssertApachePreserves(json);

        // A later parse must still reuse the unannotated primitive without metadata.
        var later = Parse("later.avdl", $"schema S; record S {{ {primitive} Value; }}");
        Assert.Same(plain, Assert.Single(Assert.IsType<RecordSchema>(Assert.Single(later.Declarations)).Fields).Type);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("-12.5")]
    [InlineData("\"text\"")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("null")]
    [InlineData("[1,\"two\",null,false]")]
    [InlineData("{\"nested\":{\"value\":[42,true,null]}}")]
    public void Json_and_idl_preserve_the_same_values_on_fields_and_nested_collection_types(string value)
    {
        var idl = $$"""
            schema R;
            record R {
                @custom({{value}}) string Value;
                @outer(1) array<@custom({{value}}) string> Items;
                map<@custom({{value}}) string> Values;
                array<map<@custom({{value}}) string>> Nested;
            }
            """;
        var json = """
            {"type":"record","name":"R","fields":[
              {"name":"Value","type":{"type":"string","custom":VALUE}},
              {"name":"Items","type":{"type":"array","outer":1,"items":{"type":"string","custom":VALUE}}},
              {"name":"Values","type":{"type":"map","values":{"type":"string","custom":VALUE}}},
              {"name":"Nested","type":{"type":"array","items":{"type":"map","values":{"type":"string","custom":VALUE}}}}
            ]}
            """.Replace("VALUE", value, StringComparison.Ordinal);
        var actual = SerializeBound(Parse("schema.avdl", idl));
        var expected = SerializeBound(Parse("schema.avsc", json));
        using var serialized = JsonDocument.Parse(actual);
        using var equivalent = JsonDocument.Parse(expected);
        using var property = JsonDocument.Parse(value);
        Assert.True(JsonElement.DeepEquals(equivalent.RootElement, serialized.RootElement));
        var fields = serialized.RootElement.GetProperty("fields");
        Assert.True(JsonElement.DeepEquals(property.RootElement, fields[0].GetProperty("type").GetProperty("custom")));
        Assert.True(JsonElement.DeepEquals(property.RootElement, fields[1].GetProperty("type").GetProperty("items").GetProperty("custom")));
        Assert.True(JsonElement.DeepEquals(property.RootElement, fields[2].GetProperty("type").GetProperty("values").GetProperty("custom")));
        Assert.True(JsonElement.DeepEquals(property.RootElement, fields[3].GetProperty("type").GetProperty("items").GetProperty("values").GetProperty("custom")));
        AssertApachePreserves(actual);
    }

    [Fact]
    public void Type_and_field_annotations_remain_distinct_with_reserved_names_and_duplicates()
    {
        const string Idl = """
            schema R;
            record R {
                @type("long") @name("Wrong") @items(1) @values(2) @doc("ignored")
                @custom(1) @custom({"last":true}) string @custom("field") Value;
                @type("long") string Plain;
            }
            """;
        var file = Parse("schema.avdl", Idl);
        var record = Assert.IsType<RecordSchema>(Assert.Single(file.Declarations));
        Assert.Equal(["custom"], record.Fields[0].Type.Properties.Keys);
        Assert.Equal(["custom"], record.Fields[0].Properties.Keys);
        Assert.Same(AvroSchema.String, record.Fields[1].Type);
        using var serialized = JsonDocument.Parse(SerializeBound(file));
        var field = serialized.RootElement.GetProperty("fields")[0];
        Assert.Equal("field", field.GetProperty("custom").GetString());
        Assert.Equal("string", field.GetProperty("type").GetProperty("type").GetString());
        Assert.True(field.GetProperty("type").GetProperty("custom").GetProperty("last").GetBoolean());
        AssertApachePreserves(serialized.RootElement.GetRawText());
    }

    [Theory]
    [InlineData("@logicalType(\"uuid\") @custom(42) string", "{\"type\":\"string\",\"logicalType\":\"uuid\",\"custom\":42}")]
    [InlineData("@logicalType(\"future-type\") @custom(42) string", "{\"type\":\"string\",\"logicalType\":\"future-type\",\"custom\":42}")]
    [InlineData("@logicalType(\"future-type\") @logicalType(\"uuid\") @custom(42) string", "{\"type\":\"string\",\"logicalType\":\"uuid\",\"custom\":42}")]
    [InlineData("@logicalType(\"uuid\") @custom(42) bytes", "{\"type\":\"bytes\",\"logicalType\":\"uuid\",\"custom\":42}")]
    [InlineData("union { null, @logicalType(\"uuid\") @custom(42) string }", "[\"null\",{\"type\":\"string\",\"logicalType\":\"uuid\",\"custom\":42}]")]
    [InlineData("union { null, @custom(42) string }", "[\"null\",{\"type\":\"string\",\"custom\":42}]")]
    [InlineData("@custom(42) string?", "[\"null\",{\"type\":\"string\",\"custom\":42}]")]
    public void Logical_and_nullable_primitives_keep_custom_properties_and_existing_logical_policy(string type, string jsonType)
    {
        var actual = SerializeBound(Parse("schema.avdl", $"schema R; record R {{ {type} Value; }}"));
        var expected = SerializeBound(Parse("schema.avsc", $$"""{"type":"record","name":"R","fields":[{"name":"Value","type":{{jsonType}}}]}"""));
        using var idl = JsonDocument.Parse(actual);
        using var json = JsonDocument.Parse(expected);
        Assert.True(JsonElement.DeepEquals(json.RootElement, idl.RootElement));
        AssertApachePreserves(actual);
    }

    [Theory]
    [InlineData("null", 1)]
    [InlineData("\"default\"", 0)]
    public void Optional_primitive_annotations_preserve_default_branch_order(string defaultValue, int primitiveIndex)
    {
        var json = SerializeBound(Parse("schema.avdl", $"schema R; record R {{ @custom(42) string? Value = {defaultValue}; }}"));
        using var serialized = JsonDocument.Parse(json);
        var field = serialized.RootElement.GetProperty("fields")[0];
        var branches = field.GetProperty("type");
        Assert.Equal(42, branches[primitiveIndex].GetProperty("custom").GetInt32());
        Assert.Equal("null", branches[1 - primitiveIndex].GetString());
        using var expectedDefault = JsonDocument.Parse(defaultValue);
        Assert.True(JsonElement.DeepEquals(expectedDefault.RootElement, field.GetProperty("default")));
        AssertApachePreserves(json);
    }

    [Fact]
    public void Generated_record_compiles_and_its_Apache_schema_retains_nested_metadata()
    {
        const string Idl = """
            schema R;
            record R {
                @custom(42) string Value;
                array<@custom({"item":true}) int> Items;
                map<@custom([1,null]) string> Values;
                @logicalType("uuid") @custom("identifier") string Id;
                @custom("optional") string? Optional;
                string Plain;
            }
            """;
        var input = GeneratorInput.Create([ProjectFile.Source(Idl)],
            [MetadataReference.CreateFromFile(typeof(Avro.Schema).Assembly.Location)],
            new ProjectConfig(LanguageVersion.CSharp12) { AvroLibrary = "Apache" });
        var token = TestContext.Current.CancellationToken;
        var driver = input.GeneratorDriver.RunGeneratorsAndUpdateCompilation(input.Compilation, out var compilation, out var diagnostics, token);
        Assert.Empty(diagnostics);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        using var stream = new MemoryStream();
        var emitted = compilation.Emit(stream, cancellationToken: token);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        var assembly = Assembly.Load(stream.ToArray());
        var schema = Assert.IsAssignableFrom<Avro.Schema>(assembly.GetType("R", throwOnError: true)!.GetField("_SCHEMA")!.GetValue(null));
        using var actual = JsonDocument.Parse(schema.ToString());
        using var expected = JsonDocument.Parse(SerializeBound(Parse("schema.avdl", Idl)));
        Assert.True(JsonElement.DeepEquals(expected.RootElement, actual.RootElement));
    }

    private static AvroFile Parse(string path, string text)
    {
        var file = AvroFile.Parse(new SourceText(path, text), s_options, TestContext.Current.CancellationToken);
        Assert.True(file.IsValid, string.Join(Environment.NewLine, file.GetDiagnostics()));
        Assert.Empty(file.GetDiagnostics());
        return file;
    }

    private static string SerializeBound(AvroFile file)
    {
        var token = TestContext.Current.CancellationToken;
        var symbols = SymbolTable.FromFiles([file], token);
        var bound = BoundAvroFile.Bind(LinkedAvroFile.Link(file, symbols, token), token);
        Assert.True(bound.IsValid);
        Assert.Empty(bound.GetDiagnostics());
        var schemas = bound.Declarations.ToDictionary(schema => schema.SchemaName);
        var lowered = file.RootSchema.ToJsonString(file.Declarations.ToDictionary(schema => schema.SchemaName));
        var serialized = bound.RootSchema!.ToJsonString(schemas);
        Assert.Equal(lowered, serialized);
        return serialized;
    }

    private static void AssertApachePreserves(string json)
    {
        using var expected = JsonDocument.Parse(json);
        using var actual = JsonDocument.Parse(Avro.Schema.Parse(json).ToString());
        Assert.True(JsonElement.DeepEquals(expected.RootElement, actual.RootElement));
    }
}
