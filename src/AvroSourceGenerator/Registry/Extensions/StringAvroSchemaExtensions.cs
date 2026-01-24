using System.Diagnostics.CodeAnalysis;
using System.Text;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry.Extensions;

internal static class StringAvroSchemaExtensions
{
    private static bool TryGetReservedName(ReadOnlySpan<char> name, [MaybeNullWhen(false)] out string replacement)
    {
        replacement = name switch
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
            _ => null
        };

        return replacement is not null;
    }

    extension(ReadOnlySpan<char> name)
    {
        public ReadOnlySpan<char> ToValidName() =>
            TryGetReservedName(name, out var replacement) ? replacement.AsSpan() : name;
    }

    extension(string name)
    {
        public string ToValidName() =>
            TryGetReservedName(name.AsSpan(), out var replacement) ? replacement : name;

        public SchemaName ToSchemaName(string? containingNamespace = null)
        {
            _ = name.TrySplitQualifiedName(out name, out var @namespace);

            if (string.IsNullOrWhiteSpace(name) || @namespace is "")
                throw new InvalidSchemaException("Argument has an invalid name format: 'cannot start or end with a dot'");

            return new SchemaName(name, @namespace ?? containingNamespace);
        }
    }

    extension(ReadOnlySpan<char> @namespace)
    {
        public string GetValidNamespace()
        {
            // TODO: Might be a good idea to pool this builder.
            var builder = new StringBuilder();

            var first = true;
            foreach (var part in new SplitEnumerable(@namespace, '.'))
            {
                var name = part.ToValidName();
                if (first) first = false;
                else builder.Append('.');
                builder.EnsureCapacity(builder.Length + name.Length);
                foreach (var @char in name)
                    builder.Append(@char);
            }

            return builder.ToString();
        }
    }

    extension(string @namespace)
    {
        public string GetValidNamespace() =>
            @namespace.Contains('.') ? @namespace.AsSpan().GetValidNamespace() : @namespace.ToValidName();
    }

    extension(string qualifiedName)
    {
        public bool TrySplitQualifiedName(out string name, out string? @namespace)
        {
            var indexOfLast = qualifiedName.LastIndexOf('.');
            if (indexOfLast < 0)
            {
                name = qualifiedName;
                @namespace = null;
                return false;
            }

            name = qualifiedName[(indexOfLast + 1)..];
            @namespace = qualifiedName.AsSpan(0, indexOfLast).GetValidNamespace();

            return true;
        }
    }
}
