using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Registry;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Tests;

public sealed class SchemaRegistryFindTests
{
    [Theory]
    [InlineData("null")]
    [InlineData("boolean")]
    [InlineData("int")]
    [InlineData("long")]
    [InlineData("float")]
    [InlineData("double")]
    [InlineData("bytes")]
    [InlineData("string")]
    public void Find_WellKnownPrimitiveType_ReturnsSchema(string typeName)
    {
        var registry = new SchemaRegistry(SchemaRegistryOptions.Default);

        var result = registry.Find(new SchemaName(typeName));

        Assert.NotNull(result);
    }

    [Fact]
    public void Find_TypeRegisteredInPreviousRegisterScope_ReturnsNull()
    {
        var registry = new SchemaRegistry(SchemaRegistryOptions.Default);
        registry.Register(
            Parse(
                """
                {
                  "type": "record",
                  "name": "OrderCreated",
                  "namespace": "Demo",
                  "fields": []
                }
                """));

        var result = registry.Find(new SchemaName("OrderCreated", "Demo"));

        Assert.Null(result);
    }

    [Fact]
    public void Find_TypeRegisteredInCurrentRegisterScope_ReturnsSchema()
    {
        var registry = new SchemaRegistry(SchemaRegistryOptions.Default);
        using var _ = registry.EnterRegisterScope();
        Assert.True(registry.TryRegister(CreateRecord("OrderCreated", "Demo")));

        var result = registry.Find(new SchemaName("OrderCreated", "Demo"));

        Assert.NotNull(result);
    }

    [Fact]
    public void Find_TypeInCurrentRecursionScope_ReturnsSchema()
    {
        var registry = new SchemaRegistry(SchemaRegistryOptions.Default);
        using var _ = registry.EnterRecursionScope(new SchemaName("OrderCreated", "Demo"));

        var result = registry.Find(new SchemaName("OrderCreated", "Demo"));

        Assert.NotNull(result);
    }

    [Fact]
    public void Find_UnknownType_ReturnsNull()
    {
        var registry = new SchemaRegistry(SchemaRegistryOptions.Default);

        var result = registry.Find(new SchemaName("DefinitelyMissingType", "Demo"));

        Assert.Null(result);
    }

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static RecordSchema CreateRecord(string name, string? @namespace) =>
        new RecordSchema(
            Json: Parse(
                $$"""
                {
                  "type": "record",
                  "name": "{{name}}",
                  "namespace": "{{@namespace}}",
                  "fields": []
                }
                """),
            SchemaName: new SchemaName(name, @namespace),
            Documentation: null,
            Aliases: [],
            Fields: [],
            Properties: ImmutableSortedDictionary<string, JsonElement>.Empty);
}

