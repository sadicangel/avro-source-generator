using System.Diagnostics.CodeAnalysis;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler.Parsing;

internal static class NameValidator
{
    public static bool IsValidMemberName([NotNullWhen(true)] string? memberName) => IsValidName(memberName.AsSpan(), out var isQualified) && !isQualified;

    public static bool IsValidName([NotNullWhen(true)] string? name) => IsValidName(name.AsSpan());

    public static bool IsValidName(ReadOnlySpan<char> name) => IsValidName(name, out _);

    public static bool IsValidName(ReadOnlySpan<char> name, out bool isQualified)
    {
        isQualified = false;
        if (name.IsWhiteSpace())
            return false;

        var index = 0;
        var enumerator = new SplitEnumerable(name, '.').GetEnumerator();
        while (enumerator.MoveNext())
        {
            if (!IsValidPart(enumerator.Current))
                return false;
            ++index;
        }

        isQualified = index > 1;
        return true;
    }

    private static bool IsValidPart(ReadOnlySpan<char> part)
    {
        if (part.IsWhiteSpace() || !(char.IsAsciiLetter(part[0]) || part[0] is '_'))
            return false;

        for (var i = 0; i < part.Length; i++)
        {
            if (!(char.IsAsciiLetterOrDigit(part[i]) || part[i] is '_'))
                return false;
        }

        return true;
    }
}
