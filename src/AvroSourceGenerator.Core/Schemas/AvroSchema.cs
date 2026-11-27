using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public closed record class AvroSchema(
    SchemaType SchemaType,
    SchemaName SchemaName,
    CSharpName CSharpName,
    string? Documentation,
    ImmutableSortedDictionary<string, JsonElement> Properties)
{
    public static readonly PrimitiveSchema Null = new PrimitiveSchema(SchemaType.Null, CSharpName.Null, new SchemaName(AvroTypeNames.Null));
    public static readonly PrimitiveSchema Boolean = new PrimitiveSchema(SchemaType.Boolean, CSharpName.Bool, new SchemaName(AvroTypeNames.Boolean));
    public static readonly PrimitiveSchema Int = new PrimitiveSchema(SchemaType.Int, CSharpName.Int, new SchemaName(AvroTypeNames.Int));
    public static readonly PrimitiveSchema Long = new PrimitiveSchema(SchemaType.Long, CSharpName.Long, new SchemaName(AvroTypeNames.Long));
    public static readonly PrimitiveSchema Float = new PrimitiveSchema(SchemaType.Float, CSharpName.Float, new SchemaName(AvroTypeNames.Float));
    public static readonly PrimitiveSchema Double = new PrimitiveSchema(SchemaType.Double, CSharpName.Double, new SchemaName(AvroTypeNames.Double));
    public static readonly PrimitiveSchema Bytes = new PrimitiveSchema(SchemaType.Bytes, CSharpName.ByteArray, new SchemaName(AvroTypeNames.Bytes));
    public static readonly PrimitiveSchema String = new PrimitiveSchema(SchemaType.String, CSharpName.String, new SchemaName(AvroTypeNames.String));

    // ReSharper disable once UnusedMember.Global
    public bool RequiresNullability => SchemaType is SchemaType.Record or SchemaType.Error or SchemaType.Protocol;

    public sealed override string ToString() => CSharpName.FullName;
}
