using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public abstract record class NamedSchema(
    SchemaType Type,
    SchemaName SchemaName,
    string? Documentation,
    ImmutableArray<string> Aliases,
    ImmutableSortedDictionary<string, JsonElement> Properties)
    : TopLevelSchema(Type, SchemaName, CSharpName.FromSchemaName(SchemaName), Documentation, Properties)
{
    public CSharpName? InheritsFrom
    {
        get;
        init
        {
            if (Type is not (SchemaType.Record or SchemaType.Error or SchemaType.Fixed))
                throw new InvalidOperationException($"InheritsFrom can only be set for Record, Error or Fixed schemas, but was {Type}.");
            field = value;
        }
    }
}
