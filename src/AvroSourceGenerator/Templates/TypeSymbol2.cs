using System.Text.Json;
using AvroSourceGenerator.Output;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Templates;

internal readonly record struct TypeSymbol2(string Name, string? Namespace, SchemaTypeTag Kind)
{
    public string Name { get; init; } = Name;

    public bool IsNullable { get => Name[^1] == '?'; }

    public override string ToString() => string.IsNullOrWhiteSpace(Namespace) ? Name : $"{Namespace}.{Name}";

    public static string Escape(string name) => name switch
    {
        "bool" => "@bool",
        "byte" => "@byte",
        "sbyte" => "@sbyte",
        "short" => "@short",
        "ushort" => "@ushort",
        "int" => "@int",
        "uint" => "@uint",
        "long" => "@long",
        "ulong" => "@ulong",
        "double" => "@double",
        "float" => "@float",
        "decimal" => "@decimal",
        "string" => "@string",
        "char" => "@char",
        "void" => "@void",
        "object" => "@object",
        "typeof" => "@typeof",
        "sizeof" => "@sizeof",
        "null" => "@null",
        "true" => "@true",
        "false" => "@false",
        "if" => "@if",
        "else" => "@else",
        "while" => "@while",
        "for" => "@for",
        "foreach" => "@foreach",
        "do" => "@do",
        "switch" => "@switch",
        "case" => "@case",
        "default" => "@default",
        "try" => "@try",
        "catch" => "@catch",
        "finally" => "@finally",
        "lock" => "@lock",
        "goto" => "@goto",
        "break" => "@break",
        "continue" => "@continue",
        "return" => "@return",
        "throw" => "@throw",
        "public" => "@public",
        "private" => "@private",
        "internal" => "@internal",
        "protected" => "@protected",
        "static" => "@static",
        "readonly" => "@readonly",
        "sealed" => "@sealed",
        "const" => "@const",
        "fixed" => "@fixed",
        "stackalloc" => "@stackalloc",
        "volatile" => "@volatile",
        "new" => "@new",
        "override" => "@override",
        "abstract" => "@abstract",
        "virtual" => "@virtual",
        "event" => "@event",
        "extern" => "@extern",
        "ref" => "@ref",
        "out" => "@out",
        "in" => "@in",
        "is" => "@is",
        "as" => "@as",
        "params" => "@params",
        "__arglist" => "@__arglist",
        "__makeref" => "@__makeref",
        "__reftype" => "@__reftype",
        "__refvalue" => "@__refvalue",
        "this" => "@this",
        "base" => "@base",
        "namespace" => "@namespace",
        "using" => "@using",
        "class" => "@class",
        "struct" => "@struct",
        "interface" => "@interface",
        "enum" => "@enum",
        "delegate" => "@delegate",
        "checked" => "@checked",
        "unchecked" => "@unchecked",
        "unsafe" => "@unsafe",
        "operator" => "@operator",
        "explicit" => "@explicit",
        "implicit" => "@implicit",
        _ => name,
    };

    public string? GetValue(JsonElement? @default)
    {
        return Kind switch
        {
            SchemaTypeTag.Null => @default?.GetRawText(),
            SchemaTypeTag.Boolean => @default?.GetRawText(),
            SchemaTypeTag.Int => @default?.GetRawText(),
            SchemaTypeTag.Long => @default?.GetRawText(),
            SchemaTypeTag.Float => @default?.GetRawText(),
            SchemaTypeTag.Double => @default?.GetRawText(),
            SchemaTypeTag.Bytes => @default?.GetRawText() is string bytes ? $"System.Text.Encoding.GetBytes({bytes})" : null,
            SchemaTypeTag.String => @default?.GetRawText(),
            SchemaTypeTag.Enum => @default is not null ? $"{Name}.{Identifier.GetValid(@default.Value)}" : null,
            SchemaTypeTag.Fixed => @default?.GetRawText() is string bytes ? $"new {Name} {{ Value = System.Text.Encoding.GetBytes({bytes}) }}" : null,
            //SchemaTypeTag.Union => throw new NotImplementedException(),
            //SchemaTypeTag.Record => throw new NotImplementedException(),
            //SchemaTypeTag.Array => throw new NotImplementedException(),
            //SchemaTypeTag.Map => throw new NotImplementedException(),
            //SchemaTypeTag.Error => throw new NotImplementedException(),
            //SchemaTypeTag.Logical => throw new NotImplementedException(),
            _ => null,
        };
    }

    public static TypeSymbol2 Null(bool nullable) => nullable ? Types.NullNullable : Types.Null;
    public static TypeSymbol2 Boolean(bool nullable) => nullable ? Types.BooleanNullable : Types.Boolean;
    public static TypeSymbol2 Int(bool nullable) => nullable ? Types.IntNullable : Types.Int;
    public static TypeSymbol2 Long(bool nullable) => nullable ? Types.LongNullable : Types.Long;
    public static TypeSymbol2 Float(bool nullable) => nullable ? Types.FloatNullable : Types.Float;
    public static TypeSymbol2 Double(bool nullable) => nullable ? Types.DoubleNullable : Types.Double;
    public static TypeSymbol2 Bytes(bool nullable) => nullable ? Types.BytesNullable : Types.Bytes;
    public static TypeSymbol2 String(bool nullable) => nullable ? Types.StringNullable : Types.String;
    public static TypeSymbol2 Enum(JsonElement name, bool nullable, string? rootNamespace) => new($"{name}{(nullable ? "?" : "")}", rootNamespace, SchemaTypeTag.Enum);
    public static TypeSymbol2 Fixed(JsonElement name, bool nullable, string? rootNamespace) => new($"{name}{(nullable ? "?" : "")}", rootNamespace, SchemaTypeTag.Fixed);
    public static TypeSymbol2 Record(JsonElement name, bool nullable, string? rootNamespace) => new($"{name}{(nullable ? "?" : "")}", rootNamespace, SchemaTypeTag.Record);
    public static TypeSymbol2 Error(JsonElement name, bool nullable, string? rootNamespace) => new($"{name}{(nullable ? "?" : "")}", rootNamespace, SchemaTypeTag.Error);
    public static TypeSymbol2 Array(TypeSymbol2 typeArg, bool nullable) => new($"IList<{typeArg.Name}>{(nullable ? "?" : "")}", "global::System.Collections.Generic", SchemaTypeTag.Array);
    public static TypeSymbol2 Map(TypeSymbol2 typeArg, bool nullable) => new($"IDictionary<string, {typeArg.Name}>{(nullable ? "?" : "")}", "global::System.Collections.Generic", SchemaTypeTag.Map);
    public static TypeSymbol2 Union(bool nullable) => nullable ? Types.UnionNullable : Types.Union;
    public static TypeSymbol2 LogicalDecimal(bool nullable) => nullable ? Types.LogicalDecimalNullable : Types.LogicalDecimal;
    public static TypeSymbol2 LogicalUuid(bool nullable) => nullable ? Types.LogicalUuidNullable : Types.LogicalUuid;
    public static TypeSymbol2 LogicalTimestampMillis(bool nullable) => nullable ? Types.LogicalTimestampMillisNullable : Types.LogicalTimestampMillis;
    public static TypeSymbol2 LogicalTimestampMicros(bool nullable) => nullable ? Types.LogicalTimestampMicrosNullable : Types.LogicalTimestampMicros;
    public static TypeSymbol2 LogicalLocalTimestampMillis(bool nullable) => nullable ? Types.LogicalLocalTimestampMillisNullable : Types.LogicalLocalTimestampMillis;
    public static TypeSymbol2 LogicalLocalTimestampMicros(bool nullable) => nullable ? Types.LogicalLocalTimestampMicrosNullable : Types.LogicalLocalTimestampMicros;
    public static TypeSymbol2 LogicalDate(bool nullable) => nullable ? Types.LogicalDateNullable : Types.LogicalDate;
    public static TypeSymbol2 LogicalTimeMillis(bool nullable) => nullable ? Types.LogicalTimeMillisNullable : Types.LogicalTimeMillis;
    public static TypeSymbol2 LogicalTimeMicros(bool nullable) => nullable ? Types.LogicalTimeMicrosNullable : Types.LogicalTimeMicros;
    public static TypeSymbol2 LogicalDuration(bool nullable) => nullable ? Types.LogicalDurationNullable : Types.LogicalDuration;
}

file static class Types
{
    public static readonly TypeSymbol2 Null = new("object", null, SchemaTypeTag.Null);
    public static readonly TypeSymbol2 NullNullable = new("object?", null, SchemaTypeTag.Null);
    public static readonly TypeSymbol2 Boolean = new("bool", null, SchemaTypeTag.Null);
    public static readonly TypeSymbol2 BooleanNullable = new("bool?", null, SchemaTypeTag.Null);
    public static readonly TypeSymbol2 Int = new("int", null, SchemaTypeTag.Null);
    public static readonly TypeSymbol2 IntNullable = new("int?", null, SchemaTypeTag.Null);
    public static readonly TypeSymbol2 Long = new("long", null, SchemaTypeTag.Null);
    public static readonly TypeSymbol2 LongNullable = new("long?", null, SchemaTypeTag.Null);
    public static readonly TypeSymbol2 Float = new("float", null, SchemaTypeTag.Null);
    public static readonly TypeSymbol2 FloatNullable = new("float?", null, SchemaTypeTag.Null);
    public static readonly TypeSymbol2 Double = new("double", null, SchemaTypeTag.Null);
    public static readonly TypeSymbol2 DoubleNullable = new("double?", null, SchemaTypeTag.Null);
    public static readonly TypeSymbol2 Bytes = new("byte[]", null, SchemaTypeTag.Null);
    public static readonly TypeSymbol2 BytesNullable = new("byte[]?", null, SchemaTypeTag.Null);
    public static readonly TypeSymbol2 String = new("string", null, SchemaTypeTag.Null);
    public static readonly TypeSymbol2 StringNullable = new("string?", null, SchemaTypeTag.Null);
    public static readonly TypeSymbol2 Union = new("object", null, SchemaTypeTag.Union);
    public static readonly TypeSymbol2 UnionNullable = new("object?", null, SchemaTypeTag.Union);
    public static readonly TypeSymbol2 LogicalDecimal = new("AvroDecimal", "global::Avro", SchemaTypeTag.Logical);
    public static readonly TypeSymbol2 LogicalDecimalNullable = new("AvroDecimal?", "global::Avro", SchemaTypeTag.Logical);
    public static readonly TypeSymbol2 LogicalUuid = new("Guid", "global::System", SchemaTypeTag.Logical);
    public static readonly TypeSymbol2 LogicalUuidNullable = new("Guid?", "global::System", SchemaTypeTag.Logical);
    public static readonly TypeSymbol2 LogicalTimestampMillis = new("DateTime", "global::System", SchemaTypeTag.Logical);
    public static readonly TypeSymbol2 LogicalTimestampMillisNullable = new("DateTime?", "global::System", SchemaTypeTag.Logical);
    public static readonly TypeSymbol2 LogicalTimestampMicros = new("DateTime", "global::System", SchemaTypeTag.Logical);
    public static readonly TypeSymbol2 LogicalTimestampMicrosNullable = new("DateTime?", "global::System", SchemaTypeTag.Logical);
    public static readonly TypeSymbol2 LogicalLocalTimestampMillis = new("DateTime", "global::System", SchemaTypeTag.Logical);
    public static readonly TypeSymbol2 LogicalLocalTimestampMillisNullable = new("DateTime?", "global::System", SchemaTypeTag.Logical);
    public static readonly TypeSymbol2 LogicalLocalTimestampMicros = new("DateTime", "global::System", SchemaTypeTag.Logical);
    public static readonly TypeSymbol2 LogicalLocalTimestampMicrosNullable = new("DateTime?", "global::System", SchemaTypeTag.Logical);
    public static readonly TypeSymbol2 LogicalDate = new("DateTime", "global::System", SchemaTypeTag.Logical);
    public static readonly TypeSymbol2 LogicalDateNullable = new("DateTime?", "global::System", SchemaTypeTag.Logical);
    public static readonly TypeSymbol2 LogicalTimeMillis = new("TimeSpan", "global::System", SchemaTypeTag.Logical);
    public static readonly TypeSymbol2 LogicalTimeMillisNullable = new("TimeSpan?", "global::System", SchemaTypeTag.Logical);
    public static readonly TypeSymbol2 LogicalTimeMicros = new("TimeSpan", "global::System", SchemaTypeTag.Logical);
    public static readonly TypeSymbol2 LogicalTimeMicrosNullable = new("TimeSpan?", "global::System", SchemaTypeTag.Logical);
    public static readonly TypeSymbol2 LogicalDuration = new("int[]", null, SchemaTypeTag.Logical);
    public static readonly TypeSymbol2 LogicalDurationNullable = new("int[]?", null, SchemaTypeTag.Logical);
}
