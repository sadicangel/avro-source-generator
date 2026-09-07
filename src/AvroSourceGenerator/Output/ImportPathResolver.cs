namespace AvroSourceGenerator.Output;

// TODO: We can probably avoid some allocations here using the split span enumerable.
internal static class ImportPathResolver
{
    public static string Resolve(string importerPath, string importPath)
    {
        var separator = GetSeparator(importerPath, importPath);
        if (Path.IsPathRooted(importPath))
            return Normalize(importPath, separator);

        var directory = Path.GetDirectoryName(importerPath) ?? string.Empty;
        return ResolveRelative(directory, importPath, separator);
    }

    private static string ResolveRelative(string directory, string importPath, char separator)
    {
        var root = Path.GetPathRoot(directory) ?? string.Empty;
        var segments = directory[root.Length..].Split(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        foreach (var segment in importPath.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
                continue;

            if (segment == "..")
            {
                if (segments.Count > 0 && segments[^1] != "..")
                {
                    segments.RemoveAt(segments.Count - 1);
                }
                else if (root.Length == 0)
                {
                    segments.Add(segment);
                }

                continue;
            }

            segments.Add(segment);
        }

        if (Path.DirectorySeparatorChar != Path.AltDirectorySeparatorChar)
        {
            var alternateSeparator = separator == Path.DirectorySeparatorChar
                ? Path.AltDirectorySeparatorChar
                : Path.DirectorySeparatorChar;
            root = root.Replace(alternateSeparator, separator);
        }

        return root + string.Join(separator.ToString(), segments);
    }

    private static string Normalize(string path, char separator)
    {
        var root = Path.GetPathRoot(path) ?? string.Empty;
        return ResolveRelative(root, path[root.Length..], separator);
    }

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
