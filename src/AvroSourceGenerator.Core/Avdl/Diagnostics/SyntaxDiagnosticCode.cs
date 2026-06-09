namespace AvroSourceGenerator.Avdl.Diagnostics;

public enum SyntaxDiagnosticCode
{
    InvalidCharacter,
    InvalidEscapeSequence,
    InvalidNumber,
    UnterminatedDocumentation,
    UnterminatedComment,
    UnterminatedString,
    UnterminatedVerbatimIdentifier,
    UnexpectedToken,
    UnexpectedJsonValue,
    MisplacedAnnotation,
    MisplacedDocumentation,
}
