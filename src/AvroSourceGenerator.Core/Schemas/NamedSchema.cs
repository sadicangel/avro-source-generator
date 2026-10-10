using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public abstract record class NamedSchema(
    SchemaType SchemaType,
    SchemaName SchemaName,
    string? Documentation,
    ImmutableArray<string> Aliases,
    ImmutableSortedDictionary<string, JsonElement> Properties)
    : TopLevelSchema(SchemaType, SchemaName, CSharpName.FromSchemaName(SchemaName), Documentation, Properties)
{
    public CSharpName? InheritsFrom
    {
        get;
        init
        {
            if (SchemaType is not (SchemaType.Record or SchemaType.Error or SchemaType.Fixed))
                throw new InvalidOperationException($"InheritsFrom can only be set for Record, Error or Fixed schemas, but was {SchemaType}.");
            field = value;
        }
    }
}
