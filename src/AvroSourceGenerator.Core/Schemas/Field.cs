using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Schemas.Defaults;

namespace AvroSourceGenerator.Schemas;

public sealed record class Field(
    MemberName Name,
    AvroSchema Type,
    AvroSchema UnderlyingType,
    string? Documentation,
    ImmutableArray<string> Aliases,
    DefaultValue? Default,
    string? Order,
    ImmutableSortedDictionary<string, JsonElement> Properties,
    string? Remarks)
{
    public IEnumerable<AvroSchema> PossibleTypes
    {
        get
        {
            if (Type is not UnionSchema union)
            {
                yield return Type;
                yield break;
            }

            foreach (var schema in union.Schemas.Canonicalize(includeNull: false))
                yield return schema;
        }
    }

    public bool AllowsNull => Type is UnionSchema union
        ? union.Schemas.Any(static schema => schema.SchemaType is SchemaType.Null)
        : Type.SchemaType is SchemaType.Null;
}
