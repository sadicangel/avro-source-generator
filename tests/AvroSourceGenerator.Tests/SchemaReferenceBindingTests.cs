using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Tests;

public sealed class SchemaReferenceBindingTests
{
    [Fact]
    public void Named_use_remains_a_reference_with_the_bound_csharp_name()
    {
        var compiled = SchemaCompilerTestHelpers.CompileProject(
            TargetProfile.Modern,
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            ("hash.avsc", """
            { "type": "fixed", "name": "Hash", "namespace": "Demo", "size": 16 }
            """),
            ("consumer.avsc", """
            {
              "type": "record",
              "name": "Consumer",
              "namespace": "Demo",
              "fields": [{ "name": "hash", "type": "Hash" }]
            }
            """));

        var consumer = Assert.IsType<RecordSchema>(Assert.Single(compiled.BoundFiles[1].Declarations));
        var reference = Assert.IsType<AvroSchemaReference>(Assert.Single(consumer.Fields).Type);

        Assert.Equal(new SchemaName("Hash", "Demo"), reference.SchemaName);
        Assert.Equal(AvroSchema.Bytes.CSharpName, reference.CSharpName);
    }

    [Fact]
    public void Inline_definition_remains_concrete()
    {
        var parsed = SchemaCompilerTestHelpers.ParseJson(
            """
            {
              "type": "record",
              "name": "Container",
              "namespace": "Demo",
              "fields": [{
                "name": "inline",
                "type": { "type": "record", "name": "Inline", "fields": [] }
              }]
            }
            """);

        var container = Assert.IsType<RecordSchema>(parsed.Declarations.Last());

        Assert.IsType<RecordSchema>(Assert.Single(container.Fields).Type);
    }

    [Fact]
    public void Parser_collects_dependencies_for_each_declaration()
    {
        var parsed = SchemaCompilerTestHelpers.ParseJson(
            """
            {
              "type": "record",
              "name": "Container",
              "namespace": "Demo",
              "fields": [{
                "name": "inline",
                "type": {
                  "type": "record",
                  "name": "Inline",
                  "fields": [{ "name": "external", "type": "External" }]
                }
              }]
            }
            """);

        Assert.Equal(
            [new SchemaName("Inline", "Demo")],
            parsed.Dependencies[new SchemaName("Container", "Demo")]);
        Assert.Equal(
            [new SchemaName("External", "Demo")],
            parsed.Dependencies[new SchemaName("Inline", "Demo")]);
    }
}
