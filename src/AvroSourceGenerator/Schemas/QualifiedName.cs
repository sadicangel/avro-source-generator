namespace AvroSourceGenerator.Schemas;

internal readonly record struct QualifiedName(string Name, string? Namespace)
{
    public override string ToString() => Namespace is null ? Name : $"{Namespace}.{Name}";

    public bool IsNullable => Name[^1] is '?';

    public QualifiedName ToNullable() => IsNullable ? this : this with { Name = Name + "?" };

    public static QualifiedName Object(bool nullable) => new(nullable ? "object?" : "object", null);
    public static QualifiedName Boolean(bool nullable) => new(nullable ? "bool?" : "bool", null);
    public static QualifiedName Int(bool nullable) => new(nullable ? "int?" : "int", null);
    public static QualifiedName Long(bool nullable) => new(nullable ? "long?" : "long", null);
    public static QualifiedName Float(bool nullable) => new(nullable ? "float?" : "float", null);
    public static QualifiedName Double(bool nullable) => new(nullable ? "double?" : "double", null);
    public static QualifiedName Bytes(bool nullable) => new(nullable ? "byte[]?" : "byte[]", null);
    public static QualifiedName String(bool nullable) => new(nullable ? "string?" : "string", null);

    public static QualifiedName Date(bool nullable) => new(nullable ? "DateTime?" : "DateTime", "global::System");
    public static QualifiedName Decimal(bool nullable) => new(nullable ? "AvroDecimal?" : "AvroDecimal", "global::Avro");
    public static QualifiedName TimeMillis(bool nullable) => new(nullable ? "TimeSpan?" : "TimeSpan", "global::System");
    public static QualifiedName TimeMicros(bool nullable) => new(nullable ? "TimeSpan?" : "TimeSpan", "global::System");
    public static QualifiedName TimestampMillis(bool nullable) => new(nullable ? "DateTime?" : "DateTime", "global::System");
    public static QualifiedName TimestampMicros(bool nullable) => new(nullable ? "DateTime?" : "DateTime", "global::System");
    public static QualifiedName LocalTimestampMillis(bool nullable) => new(nullable ? "DateTime?" : "DateTime", "global::System");
    public static QualifiedName LocalTimestampMicros(bool nullable) => new(nullable ? "DateTime?" : "DateTime", "global::System");
    public static QualifiedName Uuid(bool nullable) => new(nullable ? "Guid?" : "Guid", "global::System");


    public static QualifiedName Array(QualifiedName elementType, bool nullable) => new($"IList<{elementType}>{(nullable ? "?" : "")}", "global::System.Collections.Generic");
    public static QualifiedName Map(QualifiedName valueType, bool nullable) => new($"IDictionary<string, {valueType}>{(nullable ? "?" : "")}", "global::System.Collections.Generic");
}
