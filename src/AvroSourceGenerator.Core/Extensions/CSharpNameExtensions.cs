using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace AvroSourceGenerator.Extensions;

internal static class CSharpNameExtensions
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

    extension(string name)
    {
        public string ToValidName() => TryGetReservedName(name.AsSpan(), out var replacement) ? replacement : name;
    }

    extension(string @namespace)
    {
        public string ToValidNamespace()
        {
            if (!@namespace.Contains('.')) return @namespace.ToValidName();

            // TODO: Might be a good idea to pool this builder.
            var builder = new StringBuilder();

            var first = true;
            foreach (var part in new SplitEnumerable(@namespace, '.'))
            {
                if (first) first = false;
                else builder.Append('.');

                var name = TryGetReservedName(part, out var replacement) ? replacement.AsSpan() : part;
                builder.EnsureCapacity(builder.Length + name.Length);
                foreach (var @char in name)
                    builder.Append(@char);
            }

            return builder.ToString();
        }
    }
}
