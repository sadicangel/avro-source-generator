using System.Text;

namespace AvroSourceGenerator.Text;

// Identity is lexical and filesystem-independent: separators become '/', dot segments are
// collapsed, and equality follows the host platform's path case behavior. OriginalPath
// remains the display spelling supplied by the caller.
public readonly struct SourcePath : IComparable<SourcePath>, IEquatable<SourcePath>
{
    private static readonly StringComparer s_comparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private readonly string? _originalPath;
    private readonly string? _canonicalPath;

    public SourcePath(string originalPath)
    {
        ArgumentNullException.ThrowIfNull(originalPath);

        _originalPath = originalPath;
        _canonicalPath = SourcePathNormalizer.Normalize(originalPath);
    }

    public string OriginalPath => _originalPath ?? string.Empty;

    public string CanonicalPath => _canonicalPath ?? string.Empty;

    public bool IsEmpty => string.IsNullOrWhiteSpace(_originalPath);

    public bool IsRooted => SourcePathNormalizer.IsRootedPath(_canonicalPath);

    internal bool TryGetSourceType(out SourceType sourceType)
    {
        if (string.IsNullOrWhiteSpace(_originalPath))
        {
            sourceType = default;
            return false;
        }

        var path = _originalPath!;
        if (path.EndsWith(".avsc", StringComparison.OrdinalIgnoreCase))
        {
            sourceType = SourceType.Avsc;
            return true;
        }

        if (path.EndsWith(".avpr", StringComparison.OrdinalIgnoreCase))
        {
            sourceType = SourceType.Avpr;
            return true;
        }

        if (path.EndsWith(".avdl", StringComparison.OrdinalIgnoreCase))
        {
            sourceType = SourceType.Avdl;
            return true;
        }

        sourceType = default;
        return false;
    }

    internal SourcePath Resolve(string importPath)
    {
        var imported = new SourcePath(importPath);
        if (imported.IsRooted)
            return imported;

        var canonicalPath = CanonicalPath;
        var separatorIndex = canonicalPath.LastIndexOf('/');
        if (separatorIndex < 0)
            return imported;

        var directoryLength = separatorIndex;
        if (directoryLength == 0 && canonicalPath.StartsWith('/', StringComparison.Ordinal))
            directoryLength = 1;
        else if (directoryLength == 2 && SourcePathNormalizer.IsWindowsDriveRooted(canonicalPath))
            directoryLength = 3;

        var builder = new StringBuilder(directoryLength + importPath.Length + 1);
        builder.Append(canonicalPath, 0, directoryLength);
        if (directoryLength > 0 && canonicalPath[directoryLength - 1] is not '/')
            builder.Append('/');
        builder.Append(importPath);
        return new SourcePath(builder.ToString());
    }

    public bool Equals(SourcePath other) => Equals(other, s_comparer);

    internal bool Equals(SourcePath other, StringComparer comparer)
    {
        if (_originalPath is null || other._originalPath is null)
            return _originalPath is null && other._originalPath is null;

        return comparer.Equals(_canonicalPath, other._canonicalPath);
    }

    public override bool Equals(object? obj) => obj is SourcePath other && Equals(other);

    public int CompareTo(SourcePath other)
    {
        if (_originalPath is null)
            return other._originalPath is null ? 0 : -1;
        if (other._originalPath is null)
            return 1;

        return s_comparer.Compare(_canonicalPath, other._canonicalPath);
    }

    public override int GetHashCode() => GetHashCode(s_comparer);

    internal int GetHashCode(StringComparer comparer) =>
        _originalPath is null ? 0 : comparer.GetHashCode(_canonicalPath!);

    public static bool operator ==(SourcePath left, SourcePath right) => left.Equals(right);

    public static bool operator !=(SourcePath left, SourcePath right) => !left.Equals(right);

    public static implicit operator string(SourcePath path) => path.OriginalPath;

    public override string ToString() => OriginalPath;
}
