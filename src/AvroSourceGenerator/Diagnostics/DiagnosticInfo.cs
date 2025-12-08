using AvroSourceGenerator.Parsing;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;

internal readonly record struct DiagnosticInfo(DiagnosticDescriptor Descriptor, LocationInfo Location, params object?[]? Arguments)
{
    private Diagnostic ToDiagnostic() => Diagnostic.Create(Descriptor, Location, Arguments);

    public static implicit operator Diagnostic(DiagnosticInfo diagnostic) => diagnostic.ToDiagnostic();

    public bool Equals(DiagnosticInfo other)
    {
        return Descriptor.Equals(other.Descriptor) && Location == other.Location && ArgumentsEquals(Arguments, other.Arguments);

        static bool ArgumentsEquals(object?[]? a, object?[]? b)
        {
            if (a is null) return b is null;
            if (b is null) return false;
            return a.SequenceEqual(b);
        }
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var argument in Arguments ?? [])
        {
            hash.Add(argument);
        }

        return hash.ToHashCode();
    }
}
