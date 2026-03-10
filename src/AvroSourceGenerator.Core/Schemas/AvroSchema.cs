using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public abstract record class AvroSchema(
    SchemaType Type,
    CSharpName CSharpName,
    SchemaName SchemaName,
    string? Documentation,
    ImmutableSortedDictionary<string, JsonElement> Properties)
{
    // ReSharper disable once UnusedMember.Global
    public bool RequiresNullability => Type is SchemaType.Record or SchemaType.Error;

    public sealed override string ToString() => CSharpName.FullName;

    public string ToJsonString(IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, JsonWriterOptions options = default)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, options);
        WriteTo(writer, registeredSchemas, [], SchemaName.Namespace);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public abstract void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace);

    public static readonly PrimitiveSchema Object = new(SchemaType.Null, new CSharpName("object"), new SchemaName(AvroTypeNames.Null));
    public static readonly PrimitiveSchema Boolean = new(SchemaType.Boolean, new CSharpName("bool"), new SchemaName(AvroTypeNames.Boolean));
    public static readonly PrimitiveSchema Int = new(SchemaType.Int, new CSharpName("int"), new SchemaName(AvroTypeNames.Int));
    public static readonly PrimitiveSchema Long = new(SchemaType.Long, new CSharpName("long"), new SchemaName(AvroTypeNames.Long));
    public static readonly PrimitiveSchema Float = new(SchemaType.Float, new CSharpName("float"), new SchemaName(AvroTypeNames.Float));
    public static readonly PrimitiveSchema Double = new(SchemaType.Double, new CSharpName("double"), new SchemaName(AvroTypeNames.Double));
    public static readonly PrimitiveSchema Bytes = new(SchemaType.Bytes, new CSharpName("byte[]"), new SchemaName(AvroTypeNames.Bytes));
    public static readonly PrimitiveSchema String = new(SchemaType.String, new CSharpName("string"), new SchemaName(AvroTypeNames.String));
}
