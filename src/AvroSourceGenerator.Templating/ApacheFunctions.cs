using AvroSourceGenerator.Compiler.Configuration;
using AvroSourceGenerator.Schemas;
using Scriban;
using Scriban.Runtime;

namespace AvroSourceGenerator.Templating;

internal sealed class ApacheFunctions : ScriptObject
{
    private const int FieldSwitchIndent = 3;
    private readonly RenderOptions _options;

    private bool UseNativeUnions => _options.LanguageFeatures.HasUnions;
    private bool UseNullableReferenceTypes => _options.LanguageFeatures.HasNullableReferenceTypes;

    public ApacheFunctions(RenderOptions options) : base(3, autoImportStaticsFromThisType: false)
    {
        _options = options;
        SetValue("write_get_field_value", DynamicCustomFunction.Create(WriteGetFieldValue), readOnly: true);
        SetValue("write_put_field_value", DynamicCustomFunction.Create(WritePutFieldValue), readOnly: true);
        SetValue("required_collection_converters", DynamicCustomFunction.Create(RequiredCollectionConverters), readOnly: true);
        IsReadOnly = true;
    }

    private void WriteGetFieldValue(TemplateContext context, Field field)
    {
        var memberName = field.Name.CSharpName;
        if (UseNativeUnions && ContainsNativeUnion(field.Type))
            WriteGetValue(context, field.Type, $"this.{memberName}", depth: 0, indent: FieldSwitchIndent);
        else
            context.WriteCode($"this.{memberName}");
    }

    private void WriteGetValue(TemplateContext context, AvroSchema schema, string expression, int depth, int indent, bool isBoxed = false)
    {
        switch (GetConversionShape(schema))
        {
            case ArraySchema array:
                WriteGetArray(context, array, expression, depth, indent);
                break;
            case MapSchema map:
                WriteGetMap(context, map, expression, depth, indent);
                break;
            case UnionTypeSchema union:
                WriteGetUnion(context, schema, union, expression, depth, indent, isBoxed);
                break;
        }
    }

    private void WriteGetArray(TemplateContext context, ArraySchema array, string expression, int depth, int indent)
    {
        var itemName = $"item{depth}";
        var objectType = _options.ObjectType;
        var nullable = UseNullableReferenceTypes ? "?" : "";
        context.WriteCode($"__ApacheConvertArray<global::System.Collections.Generic.List<{objectType}>{nullable}, {objectType}>({expression}, {itemName} => ");
        WriteGetValue(context, array.ItemSchema, itemName, depth + 1, indent, isBoxed: true);
        context.WriteCode(")");
    }

    private void WriteGetMap(TemplateContext context, MapSchema map, string expression, int depth, int indent)
    {
        var itemName = $"item{depth}";
        var objectType = _options.ObjectType;
        var nullable = UseNullableReferenceTypes ? "?" : "";
        context.WriteCode($"__ApacheConvertMap<global::System.Collections.Generic.Dictionary<string, {objectType}>{nullable}, {objectType}>({expression}, {itemName} => ");
        WriteGetValue(context, map.ValueSchema, itemName, depth + 1, indent, isBoxed: true);
        context.WriteCode(")");
    }

    private void WriteGetUnion(TemplateContext context, AvroSchema declaredSchema, UnionTypeSchema union, string expression, int depth, int indent, bool isBoxed)
    {
        if (isBoxed)
        {
            var nullForgiving = UseNullableReferenceTypes ? "!" : "";
            context.WriteCode($"(({declaredSchema}){expression}{nullForgiving})");
        }
        else
        {
            context.WriteCode(expression);
        }
        context.WriteCode(declaredSchema.CSharpName.HasNullableAnnotation ? "?.Value" : ".Value");

        // Only branches that contain more native unions need conversion after unwrapping Value.
        var armIndent = indent + 1;
        string? branchName = null;
        foreach (var member in union.MemberSchemas)
        {
            if (!ContainsNativeUnion(member))
                continue;

            if (branchName is null)
            {
                branchName = $"branch{depth}";
                context.WriteCode(" switch").WriteLine();
                context.WriteIndent(indent).WriteCode("{").WriteLine();
            }
            context.WriteIndent(armIndent).WriteCode($"{member} {branchName} => ");
            WriteGetValue(context, member, branchName, depth + 1, armIndent);
            context.WriteCode(",").WriteLine();
        }

        if (branchName is null)
            return;

        context.WriteIndent(armIndent).WriteCode($"var {branchName} => {branchName}").WriteLine();
        context.WriteIndent(indent).WriteCode("}");
    }

    private void WritePutFieldValue(TemplateContext context, Field field)
    {
        var expression = _options.FieldValueExpression;
        var fieldType = field.Type;
        if (fieldType.SchemaType == SchemaType.Null)
        {
            context.WriteCode(expression);
            return;
        }

        switch (GetConversionShape(fieldType))
        {
            case ArraySchema array:
                WritePutArray(context, fieldType, array, expression, depth: 0, indent: FieldSwitchIndent, isCollectionItem: false);
                return;
            case MapSchema map:
                WritePutMap(context, fieldType, map, expression, depth: 0, indent: FieldSwitchIndent, isCollectionItem: false);
                return;
            case UnionTypeSchema union when UseNativeUnions:
                WritePutUnion(context, fieldType, union, expression, depth: 0, indent: FieldSwitchIndent, fieldName: field.Name.SchemaName);
                return;
        }

        if (field.AllowsNull)
            context.WriteCode($"{expression} is null ? ({fieldType})null : ");
        context.WriteCode($"({field.UnderlyingType}){expression}");
    }

    // Collections received as items can need materialization even when their contents only need casts.
    private void WritePutArray(TemplateContext context, AvroSchema declaredSchema, ArraySchema array, string expression, int depth, int indent, bool isCollectionItem)
    {
        var itemSchema = array.ItemSchema;
        var itemShape = GetConversionShape(itemSchema);
        var convertItem = IsConversionShape(itemShape);
        if (!isCollectionItem && !convertItem)
        {
            context.WriteCode($"({declaredSchema}){expression}");
            return;
        }

        var collectionExpression = expression.EndsWith("!", StringComparison.Ordinal) ? expression[..^1] : expression;
        context.WriteCode($"__ApacheConvertArray<{declaredSchema}, {itemSchema}>({collectionExpression}");
        if (convertItem)
            WritePutItemConverter(context, itemSchema, itemShape, collectionExpression, depth, indent);
        context.WriteCode(")");
    }

    private void WritePutMap(TemplateContext context, AvroSchema declaredSchema, MapSchema map, string expression, int depth, int indent, bool isCollectionItem)
    {
        var valueSchema = map.ValueSchema;
        var valueShape = GetConversionShape(valueSchema);
        var convertValue = IsConversionShape(valueShape);
        if (!isCollectionItem && !convertValue)
        {
            context.WriteCode($"({declaredSchema}){expression}");
            return;
        }

        var collectionExpression = expression.EndsWith("!", StringComparison.Ordinal) ? expression[..^1] : expression;
        context.WriteCode($"__ApacheConvertMap<{declaredSchema}, {valueSchema}>({collectionExpression}");
        if (convertValue)
            WritePutItemConverter(context, valueSchema, valueShape, collectionExpression, depth, indent);
        context.WriteCode(")");
    }

    private void WritePutItemConverter(TemplateContext context, AvroSchema itemSchema, AvroSchema itemShape, string collectionExpression, int depth, int indent)
    {
        // Number lambda parameters when conversion can recurse through native unions.
        // The C# 8 preset introduces nullable references and allows nested lambda parameter shadowing.
        string itemName;
        if (UseNativeUnions && ContainsNativeUnion(itemSchema))
            itemName = $"item{depth}";
        else
            itemName = UseNullableReferenceTypes ? "item" : $"item_{collectionExpression}";

        context.WriteCode($", {itemName} => ");
        var itemDepth = depth + 1;
        switch (itemShape)
        {
            case ArraySchema array:
                WritePutArray(context, itemSchema, array, itemName, itemDepth, indent, isCollectionItem: true);
                break;
            case MapSchema map:
                WritePutMap(context, itemSchema, map, itemName, itemDepth, indent, isCollectionItem: true);
                break;
            case UnionTypeSchema union:
                WritePutUnion(context, itemSchema, union, itemName, itemDepth, indent, fieldName: null);
                break;
        }
    }

    private void WritePutUnion(TemplateContext context, AvroSchema declaredSchema, UnionTypeSchema union, string expression, int depth, int indent, string? fieldName)
    {
        var isCollectionItem = fieldName is null;
        var branchName = isCollectionItem ? $"branch{depth}" : "x";
        var armIndent = indent + 1;

        context.WriteCode($"{expression} switch").WriteLine();
        context.WriteIndent(indent).WriteCode("{").WriteLine();

        if (declaredSchema.CSharpName.HasNullableAnnotation)
            context.WriteIndent(armIndent).WriteCode("null => null,").WriteLine();

        // Collection items can already have the generated union type.
        if (isCollectionItem)
            context.WriteIndent(armIndent).WriteCode($"{union} {branchName} => {branchName},").WriteLine();

        // A collection arm inherits the union's position in the field or collection.
        foreach (var member in union.MemberSchemas)
            WritePutUnionArm(context, member, branchName, depth + 1, armIndent, isCollectionItem);

        var invalidValueExpression = isCollectionItem ? expression : "fieldValue";
        var targetPrefix = isCollectionItem ? "" : "field ";
        var targetName = fieldName ?? union.CSharpName.Name;
        context.WriteIndent(armIndent).WriteCode($"_ => throw new global::Avro.AvroRuntimeException($\"Bad value {{{invalidValueExpression}}} for {targetPrefix}{targetName} in Put()\")").WriteLine();
        context.WriteIndent(indent).WriteCode("}");
    }

    private void WritePutUnionArm(TemplateContext context, AvroSchema member, string branchName, int depth, int indent, bool isCollectionItem)
    {
        context.WriteIndent(indent);
        switch (GetConversionShape(member))
        {
            case ArraySchema array:
                // Avro bytes also implement IList, so they need their own union arm.
                context.WriteCode($"global::System.Collections.IList {branchName} when {branchName} is not byte[] => ");
                WritePutArray(context, member, array, branchName, depth, indent, isCollectionItem);
                break;
            case MapSchema map:
                context.WriteCode($"global::System.Collections.IDictionary {branchName} => ");
                WritePutMap(context, member, map, branchName, depth, indent, isCollectionItem);
                break;
            default:
                context.WriteCode($"{member} {branchName} => {branchName}");
                break;
        }
        context.WriteCode(",").WriteLine();
    }

    // The shape selects the converter. Casts and generic arguments still use the original schema's C# type.
    private static AvroSchema GetConversionShape(AvroSchema schema)
    {
        if (schema is UnionSchema union)
            schema = union.UnderlyingSchema;
        return schema is LogicalSchema logical && ContainsNativeUnion(logical) ? logical.UnderlyingSchema : schema;
    }

    private static bool RequiresItemConversion(AvroSchema schema) => IsConversionShape(GetConversionShape(schema));

    private static bool IsConversionShape(AvroSchema schema) => schema is ArraySchema or MapSchema or UnionTypeSchema;

    private static bool ContainsNativeUnion(AvroSchema schema) => schema switch
    {
        ArraySchema array => ContainsNativeUnion(array.ItemSchema),
        MapSchema map => ContainsNativeUnion(map.ValueSchema),
        UnionSchema { UnderlyingSchema: UnionTypeSchema } => true,
        UnionSchema union => ContainsNativeUnion(union.UnderlyingSchema),
        LogicalSchema { UnderlyingSchema: ArraySchema or MapSchema } logical when logical.CSharpName == logical.UnderlyingSchema.CSharpName =>
            ContainsNativeUnion(logical.UnderlyingSchema),
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
            CollectRequiredConverters(field.Type, isCollectionItem: false, ref needsArray, ref needsMap);
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

        static void CollectRequiredConverters(AvroSchema schema, bool isCollectionItem, ref bool needsArray, ref bool needsMap)
        {
            while (!needsArray || !needsMap)
            {
                switch (schema)
                {
                    case ArraySchema array when isCollectionItem || RequiresItemConversion(array.ItemSchema):
                        needsArray = true;
                        isCollectionItem = true;
                        schema = array.ItemSchema;
                        break;
                    case MapSchema map when isCollectionItem || RequiresItemConversion(map.ValueSchema):
                        needsMap = true;
                        isCollectionItem = true;
                        schema = map.ValueSchema;
                        break;
                    case UnionSchema { UnderlyingSchema.SchemaType: SchemaType.UnionType } union:
                        foreach (var member in union.Schemas)
                        {
                            CollectRequiredConverters(member, isCollectionItem, ref needsArray, ref needsMap);
                            if (needsArray && needsMap)
                                return;
                        }
                        return;
                    case UnionSchema union when union.UnderlyingSchema.SchemaType != SchemaType.Null:
                        schema = union.UnderlyingSchema;
                        break;
                    case LogicalSchema logical when ContainsNativeUnion(logical):
                        schema = logical.UnderlyingSchema;
                        break;
                    default:
                        return;
                }
            }
        }
    }
}
