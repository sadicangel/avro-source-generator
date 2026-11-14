using AvroSourceGenerator.Extensions;

namespace AvroSourceGenerator.Compiler.Parsing;

internal static class NameValidator
{
    public static bool IsValidName(string? name) => IsValidName(name.AsSpan());

    public static bool IsValidName(ReadOnlySpan<char> name)
    {
        if (name.IsWhiteSpace())
            return false;

        foreach (var part in new SplitEnumerable(name, '.'))
        {
            if (!IsValidPart(part))
                return false;
        }

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
