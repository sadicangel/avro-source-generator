using System.Collections.Immutable;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Diagnostics;

public sealed class AvroDiagnostic(AvroDiagnosticCode code, SourceSpan sourceSpan, params ImmutableArray<object?> arguments)
    : IEquatable<AvroDiagnostic>
{
    public AvroDiagnosticCode Code { get; } = code;
    public SourceSpan SourceSpan { get; } = sourceSpan;
    public ImmutableArray<object?> Arguments { get; } = arguments;

    public AvroDiagnosticSeverity Severity => Code.Severity;

    public string GetMessage() => string.Format(Code.MessageTemplate, Arguments.ToArray());

    public override string ToString() => GetMessage();

    public bool Equals(AvroDiagnostic? other) => other is not null && Code == other.Code && SourceSpan.Equals(other.SourceSpan)
        && Arguments.SequenceEqual(other.Arguments);

    public override bool Equals(object? obj) => obj is AvroDiagnostic other && Equals(other);

    public static bool operator ==(AvroDiagnostic? left, AvroDiagnostic? right) => Equals(left, right);

    public static bool operator !=(AvroDiagnostic? left, AvroDiagnostic? right) => !Equals(left, right);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Code);
        hash.Add(SourceSpan);
        foreach (var argument in Arguments) hash.Add(argument);
        return hash.ToHashCode();
    }
}
