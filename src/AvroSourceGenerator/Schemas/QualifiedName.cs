using System.Text.Json;

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
    public static QualifiedName Duration(bool nullable) => new(nullable ? "int[]?" : "int[]", null);
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

internal static class NameHelper
{
    private static readonly Dictionary<string, string> s_reserved = new()
    {
        ["bool"] = "@bool",
        ["byte"] = "@byte",
        ["sbyte"] = "@sbyte",
        ["short"] = "@short",
        ["ushort"] = "@ushort",
        ["int"] = "@int",
        ["uint"] = "@uint",
        ["long"] = "@long",
        ["ulong"] = "@ulong",
        ["double"] = "@double",
        ["float"] = "@float",
        ["decimal"] = "@decimal",
        ["string"] = "@string",
        ["char"] = "@char",
        ["void"] = "@void",
        ["object"] = "@object",
        ["typeof"] = "@typeof",
        ["sizeof"] = "@sizeof",
        ["null"] = "@null",
        ["true"] = "@true",
        ["false"] = "@false",
        ["if"] = "@if",
        ["else"] = "@else",
        ["while"] = "@while",
        ["for"] = "@for",
        ["foreach"] = "@foreach",
        ["do"] = "@do",
        ["switch"] = "@switch",
        ["case"] = "@case",
        ["default"] = "@default",
        ["try"] = "@try",
        ["catch"] = "@catch",
        ["finally"] = "@finally",
        ["lock"] = "@lock",
        ["goto"] = "@goto",
        ["break"] = "@break",
        ["continue"] = "@continue",
        ["return"] = "@return",
        ["throw"] = "@throw",
        ["public"] = "@public",
        ["private"] = "@private",
        ["internal"] = "@internal",
        ["protected"] = "@protected",
        ["static"] = "@static",
        ["readonly"] = "@readonly",
        ["sealed"] = "@sealed",
        ["const"] = "@const",
        ["fixed"] = "@fixed",
        ["stackalloc"] = "@stackalloc",
        ["volatile"] = "@volatile",
        ["new"] = "@new",
        ["override"] = "@override",
        ["abstract"] = "@abstract",
        ["virtual"] = "@virtual",
        ["event"] = "@event",
        ["extern"] = "@extern",
        ["ref"] = "@ref",
        ["out"] = "@out",
        ["in"] = "@in",
        ["is"] = "@is",
        ["as"] = "@as",
        ["params"] = "@params",
        ["__arglist"] = "@__arglist",
        ["__makeref"] = "@__makeref",
        ["__reftype"] = "@__reftype",
        ["__refvalue"] = "@__refvalue",
        ["this"] = "@this",
        ["base"] = "@base",
        ["namespace"] = "@namespace",
        ["using"] = "@using",
        ["class"] = "@class",
        ["struct"] = "@struct",
        ["interface"] = "@interface",
        ["enum"] = "@enum",
        ["delegate"] = "@delegate",
        ["checked"] = "@checked",
        ["unchecked"] = "@unchecked",
        ["unsafe"] = "@unsafe",
        ["operator"] = "@operator",
        ["explicit"] = "@explicit",
        ["implicit"] = "@implicit",
    };

    public static string GetValid(string name) =>
        s_reserved.TryGetValue(name, out var reserved) ? reserved : name;

    public static string GetLocalName(JsonElement schema)
    {
        var name = schema.GetProperty("name").GetString();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("'name' cannot be null or empty");
        }
        return GetValid(name!);
    }

    public static string? GetNamespace(JsonElement schema)
    {
        if (!schema.TryGetProperty("namespace", out var json))
            return null;

        return GetValid(json.GetString()!);
    }
}
