namespace AvroSourceGenerator.Diagnostics;

public enum AvroDiagnosticCode
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
    InvalidSource,
    InvalidSchema,
    InvalidJson,
    UnknownError,
    DuplicateSchema,
    InvalidImport,
    MissingReferences,
    NoAvroLibraryDetected,
    MultipleAvroLibrariesDetected,
}

public static class AvroDiagnosticCodeExtensions
{
    extension(AvroDiagnosticCode code)
    {
        public AvroDiagnosticSeverity Severity => code is AvroDiagnosticCode.NoAvroLibraryDetected or AvroDiagnosticCode.MultipleAvroLibrariesDetected
            ? AvroDiagnosticSeverity.Warning
            : AvroDiagnosticSeverity.Error;

        public bool IsInvalidSyntax => code is AvroDiagnosticCode.InvalidCharacter
            or AvroDiagnosticCode.InvalidEscapeSequence
            or AvroDiagnosticCode.InvalidNumber
            or AvroDiagnosticCode.UnterminatedDocumentation
            or AvroDiagnosticCode.UnterminatedComment
            or AvroDiagnosticCode.UnterminatedString
            or AvroDiagnosticCode.UnterminatedVerbatimIdentifier
            or AvroDiagnosticCode.UnexpectedToken
            or AvroDiagnosticCode.UnexpectedJsonValue
            or AvroDiagnosticCode.MisplacedAnnotation
            or AvroDiagnosticCode.MisplacedDocumentation
            or AvroDiagnosticCode.InvalidSource;

        public string MessageTemplate => code switch
        {
            AvroDiagnosticCode.InvalidCharacter => MessageTemplate.InvalidCharacter,
            AvroDiagnosticCode.InvalidEscapeSequence => MessageTemplate.InvalidEscapeSequence,
            AvroDiagnosticCode.InvalidNumber => MessageTemplate.InvalidNumber,
            AvroDiagnosticCode.UnterminatedDocumentation => MessageTemplate.UnterminatedDocumentation,
            AvroDiagnosticCode.UnterminatedComment => MessageTemplate.UnterminatedComment,
            AvroDiagnosticCode.UnterminatedString => MessageTemplate.UnterminatedString,
            AvroDiagnosticCode.UnterminatedVerbatimIdentifier => MessageTemplate.UnterminatedVerbatimIdentifier,
            AvroDiagnosticCode.UnexpectedToken => MessageTemplate.UnexpectedToken,
            AvroDiagnosticCode.UnexpectedJsonValue => MessageTemplate.UnexpectedJsonValue,
            AvroDiagnosticCode.MisplacedAnnotation => MessageTemplate.MisplacedAnnotation,
            AvroDiagnosticCode.MisplacedDocumentation => MessageTemplate.MisplacedDocumentation,
            AvroDiagnosticCode.InvalidSource => MessageTemplate.InvalidSource,
            AvroDiagnosticCode.InvalidSchema => MessageTemplate.InvalidSchema,
            AvroDiagnosticCode.InvalidJson => MessageTemplate.InvalidJson,
            AvroDiagnosticCode.UnknownError => MessageTemplate.UnknownError,
            AvroDiagnosticCode.DuplicateSchema => MessageTemplate.DuplicateSchema,
            AvroDiagnosticCode.InvalidImport => MessageTemplate.InvalidImport,
            AvroDiagnosticCode.MissingReferences => MessageTemplate.MissingReferences,
            AvroDiagnosticCode.NoAvroLibraryDetected => MessageTemplate.NoAvroLibraryDetected,
            AvroDiagnosticCode.MultipleAvroLibrariesDetected => MessageTemplate.MultipleAvroLibrariesDetected,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        };
    }
}
