using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal abstract record class AvroSchema(SchemaType Type, CSharpName CSharpName, SchemaName SchemaName)
{
    // ReSharper disable once UnusedMember.Global
    public bool RequiresNullability => Type is SchemaType.Record or SchemaType.Error;

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

    public static readonly PrimitiveSchema Object = new PrimitiveSchema(SchemaType.Null, new CSharpName("object"), new SchemaName("null"));
    public static readonly PrimitiveSchema Boolean = new PrimitiveSchema(SchemaType.Boolean, new CSharpName("bool"), new SchemaName("boolean"));
    public static readonly PrimitiveSchema Int = new PrimitiveSchema(SchemaType.Int, new CSharpName("int"), new SchemaName("int"));
    public static readonly PrimitiveSchema Long = new PrimitiveSchema(SchemaType.Long, new CSharpName("long"), new SchemaName("long"));
    public static readonly PrimitiveSchema Float = new PrimitiveSchema(SchemaType.Float, new CSharpName("float"), new SchemaName("float"));
    public static readonly PrimitiveSchema Double = new PrimitiveSchema(SchemaType.Double, new CSharpName("double"), new SchemaName("double"));
    public static readonly PrimitiveSchema Bytes = new PrimitiveSchema(SchemaType.Bytes, new CSharpName("byte[]"), new SchemaName("bytes"));
    public static readonly PrimitiveSchema String = new PrimitiveSchema(SchemaType.String, new CSharpName("string"), new SchemaName("string"));
}
