using System.Text.Json;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Tests;

public sealed class JsonElementAvroExtensionsTests
{
    [Fact]
    public void GetOptionalAvroName_DefaultOrder_PrefersNameOverProtocol()
    {
        var schema = Parse(
            """
            {
              "name": "FromName",
              "protocol": "FromProtocol",
              "namespace": "SchemaNamespace"
            }
            """);

        var result = schema.GetOptionalAvroName();

        Assert.Equal(new SchemaName("FromName", "SchemaNamespace"), result);
    }

    [Fact]
    public void GetOptionalAvroName_CanUseExplicitProtocolProperty()
    {
        var schema = Parse(
            """
            {
              "protocol": "FromProtocol",
              "namespace": "SchemaNamespace"
            }
            """);

        var result = schema.GetOptionalAvroName();

        Assert.Equal(new SchemaName("FromProtocol", "SchemaNamespace"), result);
    }

    [Fact]
    public void GetOptionalAvroName_UsesContainingNamespaceFallback()
    {
        var schema = Parse(
            """
            {
              "name": "SimpleName"
            }
            """);

        var result = schema.GetOptionalAvroName();

        Assert.Equal(new SchemaName("SimpleName", null), result);
    }

    [Fact]
    public void GetOptionalAvroName_ReportsSourcePropertyNameInError()
    {
        var schema = Parse(
            """
            {
              "protocol": "Invalid..Name"
            }
            """);

        var exception = Assert.Throws<InvalidSchemaException>(() => schema.GetOptionalAvroName());

        Assert.Contains("Property 'protocol' has an invalid format", exception.Message);
    }

    [Fact]
    public void GetRequiredSchemaName_ThrowsWhenOptionalCanonicalPathReturnsDefault()
    {
        var schema = Parse(
            """
            {
              "type": "record",
              "fields": []
            }
            """);

        var exception = Assert.Throws<InvalidSchemaException>(() => schema.GetRequiredSchemaName(null));

        Assert.Contains("'name' property is required in schema:", exception.Message);
    }

    [Fact]
    public void GetRequiredSchemaName_ReturnsResolvedSchemaName()
    {
        var schema = Parse(
            """
            {
              "name": "SimpleName"
            }
            """);

        var result = schema.GetRequiredSchemaName("Containing.Namespace");

        Assert.Equal(new SchemaName("SimpleName", "Containing.Namespace"), result);
    }

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
