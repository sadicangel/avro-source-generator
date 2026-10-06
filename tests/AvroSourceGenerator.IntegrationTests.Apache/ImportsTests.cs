using Avro;
using Avro.Specific;
using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests.Apache;

public sealed class ImportsTests
{
    [Fact]
    public void Imported_schema_uses_the_generated_nested_type()
    {
        var item = new NestedType { displayName = "Imported" };
        var envelope = new ImportedEnvelope { Item = item };
        var record = (ISpecificRecord)envelope;
        var schema = Assert.IsType<RecordSchema>(record.Schema);

        Assert.Same(item, envelope.Item);
        Assert.Equal(((ISpecificRecord)item).Schema, schema.Fields.Single().Schema);
        Assert.Same(item, record.Get(0));
    }
}
