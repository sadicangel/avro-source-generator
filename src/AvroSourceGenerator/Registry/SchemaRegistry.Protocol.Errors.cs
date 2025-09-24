using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

internal readonly partial struct SchemaRegistry
{
    private ImmutableArray<AvroSchema> ProtocolErrors(JsonElement.ArrayEnumerator? errors, string? containingNamespace)
    {
        var builder = ImmutableArray.CreateBuilder<AvroSchema>();
        builder.Add(AvroSchema.String);
        if (errors is null)
        {
            return builder.ToImmutable();
        }

        foreach (var error in errors.Value)
        {
            builder.Add(Schema(error, containingNamespace));
        }

        return builder.ToImmutable();
    }
}
