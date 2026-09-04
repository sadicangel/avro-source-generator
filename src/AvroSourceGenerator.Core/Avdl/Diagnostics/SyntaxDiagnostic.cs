using AvroSourceGenerator.Avdl.Syntax;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Avdl.Diagnostics;

public readonly record struct SyntaxDiagnostic(SyntaxDiagnosticCode Code, SourceSpan SourceSpan, string Message);

internal static class SyntaxDiagnosticExtensions
{
    private static string? GetDisplayText(SyntaxKind syntaxKind) => SyntaxFacts.GetDisplayText(syntaxKind) ?? syntaxKind.ToString();

    extension(SyntaxDiagnostic)
    {
        public static SyntaxDiagnostic InvalidCharacter(SourceSpan sourceSpan) =>
            new(SyntaxDiagnosticCode.InvalidCharacter, sourceSpan, $"Invalid character input: '{sourceSpan.ToString()}'");

        public static SyntaxDiagnostic InvalidEscapeSequence(SourceSpan sourceSpan) =>
            new(SyntaxDiagnosticCode.InvalidEscapeSequence, sourceSpan, $"Invalid escape sequence: '{sourceSpan.ToString()}'");

        public static SyntaxDiagnostic InvalidNumber(SourceSpan sourceSpan) =>
            new(SyntaxDiagnosticCode.InvalidNumber, sourceSpan, $"Invalid number literal: '{sourceSpan.ToString()}'");

        public static SyntaxDiagnostic UnterminatedDocumentation(SourceSpan sourceSpan) =>
            new(SyntaxDiagnosticCode.UnterminatedDocumentation, sourceSpan, "Unterminated documentation comment");

        public static SyntaxDiagnostic UnterminatedComment(SourceSpan sourceSpan) =>
            new(SyntaxDiagnosticCode.UnterminatedComment, sourceSpan, "Unterminated comment");

        public static SyntaxDiagnostic UnterminatedString(SourceSpan sourceSpan) =>
            new(SyntaxDiagnosticCode.UnterminatedString, sourceSpan, "Unterminated string literal");

        public static SyntaxDiagnostic UnterminatedVerbatimIdentifier(SourceSpan sourceSpan) =>
            new(SyntaxDiagnosticCode.UnterminatedVerbatimIdentifier, sourceSpan, "Unterminated verbatim identifier");

        public static SyntaxDiagnostic UnexpectedToken(SyntaxKind expected, SyntaxToken actual)
        {
            var actualValueText = actual.ValueText;
            var actualDisplayText = SyntaxFacts.GetDisplayText(actual.SyntaxKind);
            return actualDisplayText is not null && actualDisplayText != actualValueText
                ? new SyntaxDiagnostic(SyntaxDiagnosticCode.UnexpectedToken, actual.SourceSpan, $"Unexpected token '{actualValueText}' ({actualDisplayText}). Expected '{GetDisplayText(expected)}'")
                : new SyntaxDiagnostic(SyntaxDiagnosticCode.UnexpectedToken, actual.SourceSpan, $"Unexpected token '{actualValueText}'. Expected '{GetDisplayText(expected)}'");
        }

        public static SyntaxDiagnostic UnexpectedJsonValue(SyntaxToken actual) =>
            new(SyntaxDiagnosticCode.UnexpectedJsonValue, actual.SourceSpan, $"Unexpected JSON value token '{actual.ValueText}'");

        public static SyntaxDiagnostic MisplacedAnnotation(SourceSpan sourceSpan, string annotationName, string target) =>
            new(SyntaxDiagnosticCode.MisplacedAnnotation, sourceSpan, $"Annotation '@{annotationName}' is not valid on {target}");

        public static SyntaxDiagnostic MisplacedDocumentation(SourceSpan sourceSpan, string target) =>
            new(SyntaxDiagnosticCode.MisplacedDocumentation, sourceSpan, $"Documentation comment is not valid on {target}");
    }
}
