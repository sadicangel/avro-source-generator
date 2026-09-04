using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Tests;

public sealed class BoundReferenceTypeTests
{
    [Fact]
    public void Non_apache_fixed_reference_maps_to_byte_array()
    {
        var compiled = CompileFixedReference(TargetProfile.Modern);
        var reference = GetConsumerReference(compiled);

        Assert.Equal(AvroSchema.Bytes.CSharpName, reference.CSharpName);
        Assert.Empty(compiled.RenderableFiles[0].EmittedSchemas);
    }

    [Fact]
    public void Apache_fixed_reference_maps_to_declared_type()
    {
        var compiled = CompileFixedReference(TargetProfile.Apache);
        var reference = GetConsumerReference(compiled);

        Assert.Equal(CSharpName.FromSchemaName(Name("Hash")), reference.CSharpName);
        Assert.Single(compiled.RenderableFiles[0].EmittedSchemas);
    }

    [Fact]
    public void Nullable_union_uses_bound_reference_csharp_name()
    {
        var compiled = SchemaCompilerTestHelpers.CompileProject(
            TargetProfile.Modern,
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            ("target.avsc", Record("Target")),
            ("consumer.avsc", """
                {
                  "type": "record",
                  "name": "Consumer",
                  "namespace": "Demo",
                  "fields": [{ "name": "target", "type": ["null", "Target"] }]
                }
                """));
        var consumer = Assert.IsType<RecordSchema>(compiled.BoundFiles[1].Declarations.Single());
        var union = Assert.IsType<UnionSchema>(Assert.Single(consumer.Fields).Type);
        var reference = Assert.IsType<AvroSchemaReference>(union.Schemas[1]);

        Assert.Equal(CSharpName.FromSchemaName(Name("Target")), reference.CSharpName);
        Assert.Equal("global::Demo.Target?", union.CSharpName.FullName);
    }

    [Fact]
    public void Protocol_parameters_and_responses_are_bound_references()
    {
        var compiled = SchemaCompilerTestHelpers.CompileProject(
            TargetProfile.Modern,
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("service.avpr", """
                {
                  "protocol": "Service",
                  "namespace": "Demo",
                  "types": [
                    { "type": "record", "name": "Request", "fields": [] },
                    { "type": "record", "name": "Response", "fields": [] }
                  ],
                  "messages": {
                    "Send": {
                      "request": [{ "name": "request", "type": "Request" }],
                      "response": "Response"
                    }
                  }
                }
                """));
        var protocol = Assert.IsType<ProtocolSchema>(compiled.BoundFiles[0].Declarations.Last());
        var message = Assert.Single(protocol.Messages);
        var parameter = Assert.IsType<AvroSchemaReference>(Assert.Single(message.RequestParameters).Type);
        var response = Assert.IsType<AvroSchemaReference>(message.Response.Type);

        Assert.Equal(CSharpName.FromSchemaName(Name("Request")), parameter.CSharpName);
        Assert.Equal(CSharpName.FromSchemaName(Name("Response")), response.CSharpName);
    }

    private static CompiledAvroProject CompileFixedReference(TargetProfile targetProfile) =>
        SchemaCompilerTestHelpers.CompileProject(
            targetProfile,
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

    private static AvroSchemaReference GetConsumerReference(CompiledAvroProject compiled)
    {
        var consumer = Assert.IsType<RecordSchema>(compiled.BoundFiles[1].Declarations.Single());
        return Assert.IsType<AvroSchemaReference>(Assert.Single(consumer.Fields).Type);
    }

    private static string Record(string name) => $$"""
        { "type": "record", "name": "{{name}}", "namespace": "Demo", "fields": [] }
        """;

    private static SchemaName Name(string name) => new(name, "Demo");
}
