using AvroSourceGenerator.Text;
using Microsoft.Extensions.FileSystemGlobbing;

namespace AvroSourceGenerator.Cli;

internal static class InputDiscovery
{
    public static string[] Discover(IEnumerable<string> inputs, CancellationToken cancellationToken)
    {
        var selected = new HashSet<SourcePath>();
        var paths = new List<string>();
        foreach (var input in inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IEnumerable<string> matches;
            if (File.Exists(input))
            {
                if (!IsSupported(input))
                    throw new ArgumentException($"Unsupported input file '{input}'. Expected .avsc, .avpr, or .avdl.");
                matches = [Path.GetFullPath(input)];
            }
            else if (Directory.Exists(input))
            {
                matches = Enumerate(new DirectoryInfo(Path.GetFullPath(input)), cancellationToken);
            }
            else if (input.Contains('*'))
            {
                var absolute = Path.GetFullPath(input).Replace('\\', '/');
                var wildcard = absolute.IndexOf('*');
                var separator = absolute.LastIndexOf('/', wildcard);
                var root = absolute[..(separator + 1)];
                var matcher = new Matcher(OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                matcher.AddInclude(absolute[(separator + 1)..]);
                // Match incrementally so cancellation is observed while traversing large directory trees.
                matches = Enumerate(new DirectoryInfo(root), cancellationToken)
                    .Where(path => matcher.Match(Path.GetRelativePath(root, path).Replace('\\', '/')).HasMatches);
            }
            else
            {
                throw new ArgumentException($"Input '{input}' does not exist.");
            }

            var ordered = matches.Select(path => new SourcePath(path)).Order().ToArray();
            if (ordered.Length == 0)
                throw new ArgumentException($"Input '{input}' matched no supported Avro files.");
            foreach (var path in ordered)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (selected.Add(path))
                    paths.Add(path.OriginalPath);
            }
        }
        return paths.ToArray();
    }

    private static IEnumerable<string> Enumerate(DirectoryInfo directory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!directory.Exists)
            yield break;
        foreach (var entry in directory.EnumerateFileSystemInfos())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry is DirectoryInfo child)
            {
                // Do not follow directory links or junctions into cycles or unrelated trees.
                if ((child.Attributes & FileAttributes.ReparsePoint) == 0)
                    foreach (var path in Enumerate(child, cancellationToken))
                        yield return path;
            }
            else if (IsSupported(entry.FullName))
            {
                yield return entry.FullName;
            }
        }
    }

    private static bool IsSupported(string path) =>
        path.EndsWith(".avsc", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".avpr", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".avdl", StringComparison.OrdinalIgnoreCase);
}
