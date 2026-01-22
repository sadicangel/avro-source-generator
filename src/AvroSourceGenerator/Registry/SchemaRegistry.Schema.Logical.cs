using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Registry.Extensions;
using AvroSourceGenerator.Schemas;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.Registry;

internal readonly partial struct SchemaRegistry
{
    private AvroSchema Logical(JsonElement schema, AvroSchema underlyingSchema)
    {
        var logicalType = schema.GetRequiredString("logicalType");

        return avroLibrary switch
        {
            AvroLibrary.Apache =>
                LogicalSchema.ForApache(logicalType, underlyingSchema),

            _ when languageVersion < LanguageVersion.CSharp10 =>
                LogicalSchema.ForLegacy(logicalType, underlyingSchema),

            _ =>
                LogicalSchema.ForModern(logicalType, underlyingSchema),
        };
    }
}
