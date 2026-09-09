using System.Text;

namespace AvroSourceGenerator.Compiler;

internal static class ImportPathResolver
{
    public static string Resolve(string importerPath, string importPath)
    {
        var separator = GetSeparator(importerPath, importPath);
        if (Path.IsPathRooted(importPath))
        {
            var importRoot = Path.GetPathRoot(importPath) ?? string.Empty;
            return ResolveSegments(default, importPath.AsSpan(importRoot.Length), importRoot, separator);
        }

        var directory = Path.GetDirectoryName(importerPath) ?? string.Empty;
        var root = Path.GetPathRoot(directory) ?? string.Empty;
        return ResolveSegments(directory.AsSpan(root.Length), importPath.AsSpan(), root, separator);
    }

    private static string ResolveSegments(ReadOnlySpan<char> directory, ReadOnlySpan<char> importPath, string root, char separator)
    {
        // Keep ranges into each source; existing directory segments are deliberately not normalized.
        var segments = new List<(bool FromDirectory, int Start, int Length)>();
        var position = 0;
        while (TryReadSegment(directory, ref position, out var start, out var length))
            segments.Add((true, start, length));

        position = 0;
        while (TryReadSegment(importPath, ref position, out var start, out var length))
        {
            var segment = importPath.Slice(start, length);
            if (segment is ".")
                continue;

            if (segment is "..")
            {
                var last = segments.Count > 0 ? segments[^1] : default;
                var lastSegment = (last.FromDirectory ? directory : importPath).Slice(last.Start, last.Length);
                if (segments.Count > 0 && lastSegment is not "..")
                    segments.RemoveAt(segments.Count - 1);
                else if (root.Length == 0)
                    segments.Add((false, start, length));
                continue;
            }

            segments.Add((false, start, length));
        }

        var capacity = root.Length + Math.Max(0, segments.Count - 1);
        foreach (var segment in segments)
            capacity += segment.Length;
        var builder = new StringBuilder(capacity);
        foreach (var character in root)
            builder.Append(IsSeparator(character) ? separator : character);
        for (var index = 0; index < segments.Count; index++)
        {
            if (index > 0) builder.Append(separator);
            var segment = segments[index];
            builder.Append((segment.FromDirectory ? directory : importPath).Slice(segment.Start, segment.Length));
        }
        return builder.ToString();
    }

    private static bool TryReadSegment(ReadOnlySpan<char> path, ref int position, out int start, out int length)
    {
        while (position < path.Length && IsSeparator(path[position])) position++;
        start = position;
        while (position < path.Length && !IsSeparator(path[position])) position++;
        length = position - start;
        return length > 0;
    }

    private static bool IsSeparator(char character) =>
        character == Path.DirectorySeparatorChar || character == Path.AltDirectorySeparatorChar;

    private static char GetSeparator(string path, string fallback)
    {
        if (Path.DirectorySeparatorChar == Path.AltDirectorySeparatorChar)
            return Path.DirectorySeparatorChar;

        foreach (var character in path)
        {
            if (character == Path.DirectorySeparatorChar || character == Path.AltDirectorySeparatorChar)
                return character;
        }

        foreach (var character in fallback)
        {
            if (character == Path.DirectorySeparatorChar || character == Path.AltDirectorySeparatorChar)
                return character;
        }

        return Path.DirectorySeparatorChar;
    }
}
