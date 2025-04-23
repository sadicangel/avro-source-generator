using System.Text;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal abstract record class AvroSchema(SchemaType Type, CSharpName CSharpName, SchemaName SchemaName)
{
    public sealed override string ToString() => CSharpName.FullName;

    public string ToJsonString(JsonWriterOptions options = default)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, options);
        WriteTo(writer, [], SchemaName.Namespace);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public abstract void WriteTo(Utf8JsonWriter writer, HashSet<SchemaName> writtenSchemas, string? containingNamespace);

    public static readonly AvroSchema Object = new PrimitiveSchema(SchemaType.Null, new CSharpName("object"), new SchemaName("null"));
    public static readonly AvroSchema Boolean = new PrimitiveSchema(SchemaType.Boolean, new CSharpName("bool"), new SchemaName("boolean"));
    public static readonly AvroSchema Int = new PrimitiveSchema(SchemaType.Int, new CSharpName("int"), new SchemaName("int"));
    public static readonly AvroSchema Long = new PrimitiveSchema(SchemaType.Long, new CSharpName("long"), new SchemaName("long"));
    public static readonly AvroSchema Float = new PrimitiveSchema(SchemaType.Float, new CSharpName("float"), new SchemaName("float"));
    public static readonly AvroSchema Double = new PrimitiveSchema(SchemaType.Double, new CSharpName("double"), new SchemaName("double"));
    public static readonly AvroSchema Bytes = new PrimitiveSchema(SchemaType.Bytes, new CSharpName("byte[]"), new SchemaName("bytes"));
    public static readonly AvroSchema String = new PrimitiveSchema(SchemaType.String, new CSharpName("string"), new SchemaName("string"));
}
