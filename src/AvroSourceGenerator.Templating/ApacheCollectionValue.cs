using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Templating;

internal static class ApacheCollectionValue
{
    public static string? Render(Field field, RenderOptions options)
    {
        if (!NeedsConversion(field.Type))
        {
            return null;
        }

        return Convert(field.Type, options.FieldValueExpression, field.Name.SchemaName, 0, options);
    }

    private static bool NeedsConversion(AvroSchema schema) => schema switch
    {
        ArraySchema array => IsCollectionOrUnion(array.ItemSchema),
        MapSchema map => IsCollectionOrUnion(map.ValueSchema),
        UnionSchema union => union.UnderlyingSchema.Type != SchemaType.Null && union.Schemas.Any(NeedsConversion),
        _ => false,
    };

    private static bool IsCollectionOrUnion(AvroSchema schema) =>
        schema is ArraySchema or MapSchema or UnionSchema;

    private static string Convert(AvroSchema schema, string value, string prefix, int depth, RenderOptions options)
    {
        var variable = $"collection_{prefix}_{depth}";
        var item = $"item_{prefix}_{depth}";
        var nonNullValue = options.LanguageFeatures.HasNullableReferenceTypes ? $"{value.TrimEnd('!')}!" : value;
        switch (schema)
        {
            case ArraySchema array:
                // Apache constructs interface-typed inner collections. Preserve the public concrete
                // collection types, reusing compatible instances and converting only mismatches.
                var arrayItem = Convert(array.ItemSchema, item, prefix, depth + 1, options);
                return $"({value} is {array} {variable} ? {variable} : global::System.Linq.Enumerable.ToList(global::System.Linq.Enumerable.Select(global::System.Linq.Enumerable.Cast<{options.ObjectType}>((global::System.Collections.IEnumerable){nonNullValue}), {item} => {arrayItem})))";

            case MapSchema map:
                var mapValue = Convert(map.ValueSchema, $"((global::System.Collections.IDictionary){nonNullValue})[{item}]", prefix, depth + 1, options);
                return $"({value} is {map} {variable} ? {variable} : global::System.Linq.Enumerable.ToDictionary(global::System.Linq.Enumerable.Cast<string>(((global::System.Collections.IDictionary){nonNullValue}).Keys), {item} => {item}, {item} => {mapValue}))";

            case UnionSchema union:
                // Object unions already accept the runtime value without a collection cast.
                if (union.UnderlyingSchema.Type == SchemaType.Null)
                {
                    return $"({union}){nonNullValue}";
                }

                var members = union.Schemas.Where(static member => member.Type != SchemaType.Null).ToArray();
                var allowsNull = members.Length != union.Schemas.Length;
                if (members.Length == 1)
                {
                    var converted = Convert(members[0], value, prefix, depth, options);
                    return allowsNull ? $"({value} is null ? ({union})null : {converted})" : converted;
                }

                var arms = new List<string>();
                if (allowsNull)
                {
                    arms.Add($"null => ({union})null");
                }
                for (var index = 0; index < members.Length; index++)
                {
                    var member = members[index];
                    var name = $"variant_{prefix}_{depth}_{index}";
                    var pattern = member switch
                    {
                        ArraySchema => "global::System.Collections.IList",
                        MapSchema => "global::System.Collections.IDictionary",
                        _ => member.ToString(),
                    };
                    var converted = Convert(member, name, $"{prefix}_{index}", depth + 1, options);
                    arms.Add($"{pattern} {name} => ({union}){converted}");
                }
                arms.Add("_ => throw new global::Avro.AvroRuntimeException(\"Bad collection union value in Put()\")");
                return $"({value} switch {{ {string.Join(", ", arms)} }})";

            default:
                return $"({schema}){nonNullValue}";
        }
    }
}
