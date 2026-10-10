using AvroSourceGenerator.Schemas;
using Scriban.Runtime;

namespace AvroSourceGenerator.Templating;

internal sealed class ApacheFunctions : ScriptObject
{
    public ApacheFunctions() : base(2, autoImportStaticsFromThisType: false)
    {
        SetValue("requires_collection_conversion", DynamicCustomFunction.Create(RequiresCollectionConversion), readOnly: true);
        SetValue("required_collection_converters", DynamicCustomFunction.Create(RequiredCollectionConverters), readOnly: true);
        IsReadOnly = true;
    }

    // A nullable collection is typed even without a generated union declaration. Object
    // unions, scalars, and named references are leaves in the property's representation.
    public static bool RequiresCollectionConversion(AvroSchema schema) => schema switch
    {
        ArraySchema or MapSchema => true,
        UnionSchema union => union.UnderlyingSchema.SchemaType != SchemaType.Null && union.Schemas.Any(RequiresCollectionConversion),
        _ => false,
    };

    public static string[] RequiredCollectionConverters(AvroSchema schema)
    {
        var fields = schema switch
        {
            RecordSchema record => record.Fields,
            ErrorSchema error => error.Fields,
            _ => [],
        };
        var needsArray = false;
        var needsMap = false;
        foreach (var field in fields)
        {
            CollectRequiredConverters(field.Type, false, ref needsArray, ref needsMap);
            if (needsArray && needsMap)
                break;
        }

        return (needsArray, needsMap) switch
        {
            (true, true) => ["array", "map"],
            (true, false) => ["array"],
            (false, true) => ["map"],
            _ => [],
        };

        static void CollectRequiredConverters(AvroSchema schema, bool nested, ref bool needsArray, ref bool needsMap)
        {
            while (true)
            {
                switch (schema)
                {
                    case ArraySchema array:
                        if (!nested && !RequiresCollectionConversion(array.ItemSchema)) return;
                        needsArray = true;
                        if (needsMap) return;
                        nested = true;
                        schema = array.ItemSchema;
                        continue;
                    case MapSchema map:
                        if (!nested && !RequiresCollectionConversion(map.ValueSchema)) return;
                        needsMap = true;
                        if (needsArray) return;
                        nested = true;
                        schema = map.ValueSchema;
                        continue;
                    case UnionSchema { UnderlyingSchema.SchemaType: SchemaType.UnionType } union:
                        foreach (var member in union.Schemas)
                        {
                            CollectRequiredConverters(member, nested, ref needsArray, ref needsMap);
                            if (needsArray && needsMap) return;
                        }
                        return;
                    case UnionSchema union when union.UnderlyingSchema.SchemaType != SchemaType.Null:
                        schema = union.UnderlyingSchema;
                        continue;
                    default:
                        return;
                }
            }
        }
    }
}
