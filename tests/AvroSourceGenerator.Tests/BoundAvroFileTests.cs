using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Output;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Tests;

public sealed class BoundAvroFileTests
{
    [Fact]
    public void Matching_reference_name_structurally_shares_the_parsed_graph()
    {
        var compiled = Compile(
            ("target.avsc", Record("Target")),
            ("consumer.avsc", Record("Consumer", Field("target", "Target"))));
        var parsed = Assert.IsType<RecordSchema>(Assert.Single(compiled.Files[1].Declarations));
        var bound = Assert.IsType<RecordSchema>(Assert.Single(compiled.BoundFiles[1].Declarations));

        Assert.Same(parsed, bound);
        Assert.Same(compiled.Files[1].RootSchema, compiled.BoundFiles[1].RootSchema);
        Assert.Same(Assert.Single(parsed.Fields).Type, Assert.Single(bound.Fields).Type);
    }

    [Fact]
    public void Changed_reference_name_rebuilds_only_its_ancestor_path()
    {
        var compiled = Compile(
            ("hash.avsc", """
            { "type": "fixed", "name": "Hash", "namespace": "Demo", "size": 16 }
            """),
            ("consumer.avsc", Record(
                "Consumer",
                Field("hash", "Hash"),
                Field("unchanged", "string"))));
        var parsed = Assert.IsType<RecordSchema>(Assert.Single(compiled.Files[1].Declarations));
        var boundFile = compiled.BoundFiles[1];
        var bound = Assert.IsType<RecordSchema>(Assert.Single(boundFile.Declarations));

        Assert.NotSame(parsed, bound);
        Assert.Same(bound, boundFile.RootSchema);
        Assert.NotSame(parsed.Fields[0], bound.Fields[0]);
        Assert.Equal(AvroSchema.Bytes.CSharpName, bound.Fields[0].Type.CSharpName);
        Assert.Same(parsed.Fields[1], bound.Fields[1]);
    }

    [Fact]
    public void Arrays_and_maps_recalculate_their_csharp_names()
    {
        var compiled = Compile(
            ("hash.avsc", """
            { "type": "fixed", "name": "Hash", "namespace": "Demo", "size": 16 }
            """),
            ("consumer.avsc", """
            {
              "type": "record",
              "name": "Consumer",
              "namespace": "Demo",
              "fields": [
                { "name": "hashes", "type": { "type": "array", "items": "Hash" } },
                { "name": "hashByName", "type": { "type": "map", "values": "Hash" } }
              ]
            }
            """));
        var consumer = Assert.IsType<RecordSchema>(Assert.Single(compiled.BoundFiles[1].Declarations));

        Assert.Equal("global::System.Collections.Generic.List<byte[]>", consumer.Fields[0].Type.CSharpName.FullName);
        Assert.Equal(
            "global::System.Collections.Generic.Dictionary<string, byte[]>",
            consumer.Fields[1].Type.CSharpName.FullName);
    }

    [Fact]
    public void Missing_reference_keeps_the_parsed_graph()
    {
        var compiled = Compile(("consumer.avsc", Record("Consumer", Field("missing", "Missing"))));

        Assert.Same(compiled.Files[0].RootSchema, compiled.BoundFiles[0].RootSchema);
        Assert.Same(compiled.Files[0].Declarations[0], compiled.BoundFiles[0].Declarations[0]);
        Assert.Null(compiled.BoundFiles[0].References[Name("Missing")]);
    }

    [Fact]
    public void Rebuilt_variant_uses_bound_clones_without_mutating_parsed_schemas()
    {
        var compiled = Compile(
            ("hash.avsc", """
            { "type": "fixed", "name": "Hash", "namespace": "Demo", "size": 16 }
            """),
            ("envelope.avsc", """
            {
              "type": "record",
              "name": "Envelope",
              "namespace": "Demo",
              "fields": [{
                "name": "choice",
                "type": [
                  {
                    "type": "record",
                    "name": "First",
                    "fields": [{ "name": "hash", "type": "Hash" }]
                  },
                  { "type": "record", "name": "Second", "fields": [] }
                ]
              }]
            }
            """));
        var parsed = compiled.Files[1].Declarations;
        var parsedFirst = Assert.IsType<RecordSchema>(parsed.Single(schema => schema.SchemaName.Name == "First"));
        var parsedSecond = Assert.IsType<RecordSchema>(parsed.Single(schema => schema.SchemaName.Name == "Second"));
        var parsedVariant = Assert.IsType<VariantSchema>(parsed.Single(schema => schema.Type is SchemaType.Variant));
        var bound = compiled.BoundFiles[1].Declarations;
        var boundFirst = Assert.IsType<RecordSchema>(bound.Single(schema => schema.SchemaName.Name == "First"));
        var boundSecond = Assert.IsType<RecordSchema>(bound.Single(schema => schema.SchemaName.Name == "Second"));
        var boundVariant = Assert.IsType<VariantSchema>(bound.Single(schema => schema.Type is SchemaType.Variant));
        var envelope = Assert.IsType<RecordSchema>(bound.Single(schema => schema.SchemaName.Name == "Envelope"));
        var union = Assert.IsType<UnionSchema>(Assert.Single(envelope.Fields).Type);

        Assert.NotSame(parsedFirst, boundFirst);
        Assert.Same(parsedSecond, boundSecond);
        Assert.NotSame(parsedVariant, boundVariant);
        Assert.Equal(parsedVariant.CSharpName, parsedFirst.InheritsFrom);
        Assert.Equal(parsedVariant.CSharpName, parsedSecond.InheritsFrom);
        Assert.Equal(boundVariant.CSharpName, boundFirst.InheritsFrom);
        Assert.Equal(boundVariant.CSharpName, boundSecond.InheritsFrom);
        Assert.Same(boundVariant, union.UnderlyingSchema);
        Assert.Contains(boundFirst, boundVariant.DerivedSchemas);
        Assert.Contains(boundSecond, boundVariant.DerivedSchemas);
        Assert.Equal(AvroSchema.Bytes.CSharpName, Assert.Single(boundFirst.Fields).Type.CSharpName);
    }

    [Fact]
    public void Forwards_parsed_dependencies_for_every_declaration()
    {
        var compiled = Compile(
            ("target.avsc", Record("Target")),
            ("consumer.avsc", Record("Consumer", Field("target", "Target"), Field("missing", "Missing"))));

        Assert.Equal(
            [Name("Missing"), Name("Target")],
            compiled.Files[1].Dependencies[Name("Consumer")]);
        Assert.Same(compiled.Files[1].Dependencies, compiled.BoundFiles[1].Dependencies);
    }

    private static CompiledAvroProject Compile(params (string Path, string Text)[] sources) =>
        SchemaCompilerTestHelpers.CompileProject(
            TargetProfile.Modern,
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            sources);

    private static string Record(string name, params string[] fields) => $$"""
        {
          "type": "record",
          "name": "{{name}}",
          "namespace": "Demo",
          "fields": [{{string.Join(",", fields)}}]
        }
        """;

    private static string Field(string name, string type) =>
        $$"""{ "name": "{{name}}", "type": "{{type}}" }""";

    private static SchemaName Name(string name) => new(name, "Demo");
}
