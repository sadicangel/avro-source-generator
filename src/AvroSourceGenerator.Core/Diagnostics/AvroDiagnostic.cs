using AvroSourceGenerator.Avdl.Syntax;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Diagnostics;

public readonly record struct AvroDiagnostic(AvroDiagnosticCode Code, SourceSpan SourceSpan, params object?[]? Arguments) : IEquatable<AvroDiagnostic>
{
    public AvroDiagnosticSeverity Severity => Code.Severity;

    public string GetMessage() => string.Format(Code.MessageTemplate, Arguments ?? []);

    public override string ToString() => GetMessage();

    public bool Equals(AvroDiagnostic other) => Code == other.Code && SourceSpan.Equals(other.SourceSpan)
        && (Arguments ?? []).SequenceEqual(other.Arguments ?? []);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Code);
        hash.Add(SourceSpan);
        foreach (var argument in Arguments ?? []) hash.Add(argument);
        return hash.ToHashCode();
    }
}

internal static class AvroDiagnosticFactoryExtensions
{
    private static string? GetDisplayText(SyntaxKind syntaxKind) => SyntaxFacts.GetDisplayText(syntaxKind) ?? syntaxKind.ToString();

    extension(AvroDiagnostic)
    {
        public static AvroDiagnostic InvalidCharacter(SourceSpan sourceSpan) => new AvroDiagnostic(AvroDiagnosticCode.InvalidCharacter, sourceSpan, sourceSpan.ToString());

        public static AvroDiagnostic InvalidEscapeSequence(SourceSpan sourceSpan) => new AvroDiagnostic(AvroDiagnosticCode.InvalidEscapeSequence, sourceSpan, sourceSpan.ToString());

        public static AvroDiagnostic InvalidNumber(SourceSpan sourceSpan) => new AvroDiagnostic(AvroDiagnosticCode.InvalidNumber, sourceSpan, sourceSpan.ToString());

        public static AvroDiagnostic UnterminatedDocumentation(SourceSpan sourceSpan) => new AvroDiagnostic(AvroDiagnosticCode.UnterminatedDocumentation, sourceSpan);

        public static AvroDiagnostic UnterminatedComment(SourceSpan sourceSpan) => new AvroDiagnostic(AvroDiagnosticCode.UnterminatedComment, sourceSpan);

        public static AvroDiagnostic UnterminatedString(SourceSpan sourceSpan) => new AvroDiagnostic(AvroDiagnosticCode.UnterminatedString, sourceSpan);

        public static AvroDiagnostic UnterminatedVerbatimIdentifier(SourceSpan sourceSpan) => new AvroDiagnostic(AvroDiagnosticCode.UnterminatedVerbatimIdentifier, sourceSpan);

        public static AvroDiagnostic UnexpectedToken(SyntaxKind expected, SyntaxToken actual) => new AvroDiagnostic(AvroDiagnosticCode.UnexpectedToken, actual.SourceSpan, SyntaxFacts.GetDisplayText(actual.SyntaxKind) ?? actual.ValueText, GetDisplayText(expected));

        public static AvroDiagnostic UnexpectedJsonValue(SyntaxToken actual) => new AvroDiagnostic(AvroDiagnosticCode.UnexpectedJsonValue, actual.SourceSpan, actual.ValueText);

        public static AvroDiagnostic MisplacedAnnotation(SourceSpan sourceSpan, string annotationName, string target) => new AvroDiagnostic(AvroDiagnosticCode.MisplacedAnnotation, sourceSpan, annotationName, target);

        public static AvroDiagnostic MisplacedDocumentation(SourceSpan sourceSpan, string target) => new AvroDiagnostic(AvroDiagnosticCode.MisplacedDocumentation, sourceSpan, target);

        public static AvroDiagnostic InvalidSource(SourceSpan sourceSpan, string message) => new AvroDiagnostic(AvroDiagnosticCode.InvalidSource, sourceSpan, message);

        public static AvroDiagnostic InvalidSchema(SourceSpan sourceSpan, string message) => new AvroDiagnostic(AvroDiagnosticCode.InvalidSchema, sourceSpan, message);

        public static AvroDiagnostic InvalidJson(SourceSpan sourceSpan, string message) => new AvroDiagnostic(AvroDiagnosticCode.InvalidJson, sourceSpan, message);

        public static AvroDiagnostic UnknownError(SourceSpan sourceSpan, string message) => new AvroDiagnostic(AvroDiagnosticCode.UnknownError, sourceSpan, message);

        public static AvroDiagnostic DuplicateSchema(SourceSpan sourceSpan, string typeName) => new AvroDiagnostic(AvroDiagnosticCode.DuplicateSchema, sourceSpan, typeName);

        public static AvroDiagnostic MissingReferences(SourceSpan sourceSpan, IReadOnlyCollection<SchemaName> references) => new AvroDiagnostic(AvroDiagnosticCode.MissingReferences, sourceSpan, string.Join(", ", references.Select(static name => name.FullName)));

        public static AvroDiagnostic InvalidImport(SourceSpan sourceSpan, string message) => new AvroDiagnostic(AvroDiagnosticCode.InvalidImport, sourceSpan, message);
    }
}
