using AvroSourceGenerator.Extensions;

namespace AvroSourceGenerator.Schemas;

public readonly record struct CSharpName(string Name, string? Namespace)
{
    public string FullName { get; } = Namespace is null ? Name : $"global::{Namespace}.{Name}";

    public bool HasNullableAnnotation => Name.EndsWith("?", StringComparison.Ordinal);

    public CSharpName(string name) : this(name, null) { }

    public CSharpName WithNullableAnnotation() =>
        HasNullableAnnotation ? this : new CSharpName($"{Name}?", Namespace);

    public CSharpName WithoutNullableAnnotation() =>
        HasNullableAnnotation ? new CSharpName(Name[..^1], Namespace) : this;

    public override string ToString() => ToString(includeGlobalPrefix: true);

    public string ToString(bool includeGlobalPrefix) => includeGlobalPrefix || !FullName.StartsWith("global::") ? FullName : FullName[8..];

    public static CSharpName FromSchemaName(SchemaName schemaName) => new CSharpName(schemaName.Name.ToValidName(), schemaName.Namespace?.ToValidNamespace());

    public static CSharpName Null => new CSharpName("object");
    public static CSharpName Bool => new CSharpName("bool");
    public static CSharpName Int => new CSharpName("int");
    public static CSharpName Long => new CSharpName("long");
    public static CSharpName Float => new CSharpName("float");
    public static CSharpName Double => new CSharpName("double");
    public static CSharpName ByteArray => new CSharpName("byte[]");
    public static CSharpName String => new CSharpName("string");
    public static CSharpName Decimal => new CSharpName("decimal");
    public static CSharpName Guid => new CSharpName("Guid", "System");
    public static CSharpName DateTime => new CSharpName("DateTime", "System");
    public static CSharpName DateTimeOffset => new CSharpName("DateTimeOffset", "System");
    public static CSharpName DateOnly => new CSharpName("DateOnly", "System");
    public static CSharpName TimeSpan => new CSharpName("TimeSpan", "System");
    public static CSharpName TimeOnly => new CSharpName("TimeOnly", "System");
}
