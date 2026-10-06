using AvroSourceGenerator.IntegrationTests.Schemas;
using Chr.Avro.Abstract;

namespace AvroSourceGenerator.IntegrationTests.Chr;

public sealed class GeneratedModelsTests
{
    [Theory]
    [InlineData(typeof(Primitives))]
    [InlineData(typeof(Collections))]
    [InlineData(typeof(NestedType))]
    [InlineData(typeof(EmailContent))]
    [InlineData(typeof(CreateUserRequest))]
    public void Generated_models_produce_record_schemas(Type type)
    {
        var schema = Assert.IsType<RecordSchema>(new SchemaBuilder().BuildSchema(type));

        Assert.Equal(type.Name, schema.Name);
        Assert.NotEmpty(schema.Fields);
    }

    [Fact]
    public void Imported_schema_uses_the_generated_nested_type()
    {
        var item = new NestedType { displayName = "Imported" };
        var envelope = new ImportedEnvelope { Item = item };
        var schema = Assert.IsType<RecordSchema>(new SchemaBuilder().BuildSchema<ImportedEnvelope>());
        var nestedSchema = Assert.IsType<RecordSchema>(schema.Fields.Single().Type);

        Assert.Same(item, envelope.Item);
        Assert.Equal(nameof(NestedType), nestedSchema.Name);
        Assert.Equal("displayName", nestedSchema.Fields.Single().Name);
    }

    [Fact]
    public void Union_variant_can_be_assigned_to_its_generated_interface()
    {
        var content = new EmailContent { subject = "Subject", body = "Body", recipientEmail = "test@example.com" };
        var notification = new Notification { content = content };

        Assert.IsAssignableFrom<INotificationContentVariant>(content);
        Assert.Same(content, notification.content);
    }
}
