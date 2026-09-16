using System.Text;

namespace AvroSourceGenerator.Text;

internal static class SourcePathNormalizer
{
    public static string Normalize(string path)
    {
        var pathInfo = GetPathInfo(path);
        var segments = new List<Segment>();
        var position = pathInfo.SegmentStart;
        while (TryReadSegment(path, ref position, out var segmentStart, out var segmentLength))
        {
            var segment = path.AsSpan(segmentStart, segmentLength);
            switch (segment)
            {
                case ".":
                    continue;

                case "..":
                    if (segments.Count > 0 && !segments[^1].IsParent(path))
                        segments.RemoveAt(segments.Count - 1);
                    else if (!pathInfo.IsRooted)
                        segments.Add(new Segment(segmentStart, segmentLength));
                    continue;

                default:
                    segments.Add(new Segment(segmentStart, segmentLength)); break;
            }
        }

        var capacity = pathInfo.GetPrefixLength() + Math.Max(0, segments.Count - 1) + segments.Sum(segment => segment.Length);

        var builder = new StringBuilder(capacity);
        pathInfo.AppendPrefix(builder, path);
        for (var index = 0; index < segments.Count; index++)
        {
            if (index > 0 || pathInfo is { HasPrefix: true, PrefixEndsWithSeparator: false })
                builder.Append('/');
            var segment = segments[index];
            builder.Append(path, segment.Start, segment.Length);
        }

        return builder.ToString();
    }

    private static PathInfo GetPathInfo(string path)
    {
        if (path.Length >= 2 && IsSeparator(path[0]) && IsSeparator(path[1]))
        {
            var position = 2;
            TryReadSegment(path, ref position, out var serverStart, out var serverLength);
            TryReadSegment(path, ref position, out var shareStart, out var shareLength);
            return new PathInfo(PathRoot.Unc, position, serverStart, serverLength, shareStart, shareLength);
        }

        if (IsWindowsDriveRooted(path))
            return new PathInfo(PathRoot.WindowsDrive, 3, driveLetter: path[0]);

        if (path.Length > 0 && IsSeparator(path[0]))
            return new PathInfo(PathRoot.Unix, 1);

        return new PathInfo(PathRoot.Relative, 0);
    }

    public static bool IsRootedPath(string? path)
    {
        var nonNullPath = path ?? string.Empty;
        if (nonNullPath.Length == 0)
            return false;

        return nonNullPath[0] is '/' || IsWindowsDriveRooted(nonNullPath);
    }

    public static bool IsWindowsDriveRooted(string path) =>
        path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] is ':' && IsSeparator(path[2]);

    private static bool TryReadSegment(string path, ref int position, out int start, out int length)
    {
        while (position < path.Length && IsSeparator(path[position]))
            position++;
        start = position;
        while (position < path.Length && !IsSeparator(path[position]))
            position++;
        length = position - start;
        return length > 0;
    }

    private static bool IsSeparator(char character) => character is '/' or '\\';

    private enum PathRoot
    {
        Relative,
        Unix,
        WindowsDrive,
        Unc,
    }

    private readonly struct Segment(int start, int length)
    {
        public int Start { get; } = start;
        public int Length { get; } = length;

        public bool IsParent(string path) => Length == 2 && path.AsSpan(Start, Length) is "..";
    }

    private readonly struct PathInfo(
        PathRoot root,
        int segmentStart,
        int serverStart = 0,
        int serverLength = 0,
        int shareStart = 0,
        int shareLength = 0,
        char driveLetter = '\0')
    {
        public int SegmentStart { get; } = segmentStart;

        public bool IsRooted => root is not PathRoot.Relative;

        public bool HasPrefix => root is not PathRoot.Relative;

        public bool PrefixEndsWithSeparator => root is PathRoot.Unix or PathRoot.WindowsDrive;

        public int GetPrefixLength() => root switch
        {
            PathRoot.Relative => 0,
            PathRoot.Unix => 1,
            PathRoot.WindowsDrive => 3,
            PathRoot.Unc => 2 + serverLength + (shareLength == 0 ? 0 : shareLength + 1),
            _ => throw new InvalidOperationException("Unreachable: Unsupported path root."),
        };

        public void AppendPrefix(StringBuilder builder, string path)
        {
            switch (root)
            {
                case PathRoot.Relative:
                    return;
                case PathRoot.Unix:
                    builder.Append('/');
                    return;
                case PathRoot.WindowsDrive:
                    builder.Append(char.ToUpperInvariant(driveLetter));
                    builder.Append(":/");
                    return;
                case PathRoot.Unc:
                    builder.Append("//");
                    builder.Append(path, serverStart, serverLength);
                    if (shareLength == 0)
                        return;
                    builder.Append('/');
                    builder.Append(path, shareStart, shareLength);
                    return;
                default:
                    throw new InvalidOperationException("Unreachable: Unsupported path root.");
            }
        }
    }
}
