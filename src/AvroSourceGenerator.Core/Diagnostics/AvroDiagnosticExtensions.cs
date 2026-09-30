using System.Text.Json;
using AvroSourceGenerator.Avdl;
using AvroSourceGenerator.Avjs;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Diagnostics;

internal static class AvroDiagnosticExtensions
{
    extension(AvroDiagnostic diagnostic)
    {
        public bool IsError => diagnostic.Severity is AvroDiagnosticSeverity.Error;
    }

    extension(IEnumerable<AvroDiagnostic> diagnostics)
    {
        public bool HasErrors => diagnostics.Any(static diagnostic => diagnostic.IsError);
    }

    private static string? Display(SyntaxKind kind) => SyntaxFacts.GetDisplayText(kind) ?? kind.ToString();

    private static string GetJsonMessage(JsonException exception)
    {
        var message = exception.Message;
        if (exception.LineNumber is not { } line || exception.BytePositionInLine is not { } position)
            return message;

        var suffix = FormattableString.Invariant($"LineNumber: {line} | BytePositionInLine: {position}.");
        if (!message.EndsWith(suffix, StringComparison.Ordinal))
            return message;

        message = message[..^suffix.Length].TrimEnd();
        return message.EndsWith("|", StringComparison.Ordinal) ? message[..^1].TrimEnd() : message;
    }

    extension(AvroDiagnostic)
    {
        public static AvroDiagnostic InvalidCharacter(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidCharacter, span, span.ToString());
        public static AvroDiagnostic InvalidEscapeSequence(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidEscapeSequence, span, span.ToString());
        public static AvroDiagnostic InvalidNumber(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidNumber, span, span.ToString());
        public static AvroDiagnostic UnterminatedDocumentation(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.UnterminatedDocumentation, span);
        public static AvroDiagnostic UnterminatedComment(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.UnterminatedComment, span);
        public static AvroDiagnostic UnterminatedString(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.UnterminatedString, span);
        public static AvroDiagnostic UnterminatedVerbatimIdentifier(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.UnterminatedVerbatimIdentifier, span);
        public static AvroDiagnostic UnexpectedToken(SyntaxKind expected, SyntaxToken actual) => new AvroDiagnostic(AvroDiagnosticCode.UnexpectedToken, actual.SourceSpan, Display(expected));
        public static AvroDiagnostic UnexpectedJsonValue(SyntaxToken actual) => new AvroDiagnostic(AvroDiagnosticCode.UnexpectedJsonValue, actual.SourceSpan);
        public static AvroDiagnostic MisplacedAnnotation(SourceSpan span, string name, string target) => new AvroDiagnostic(AvroDiagnosticCode.MisplacedAnnotation, span, name, target);
        public static AvroDiagnostic MisplacedDocumentation(SourceSpan span, string target) => new AvroDiagnostic(AvroDiagnosticCode.MisplacedDocumentation, span, target);
        public static AvroDiagnostic UnsupportedSourceType(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.UnsupportedSourceType, span);
        public static AvroDiagnostic EmptySource(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.EmptySource, span);
        public static AvroDiagnostic DuplicateSourcePath(SourceSpan span, string path, string original) => new AvroDiagnostic(AvroDiagnosticCode.DuplicateSourcePath, span, path, original);
        public static AvroDiagnostic DuplicateSchema(SourceSpan span, string name) => new AvroDiagnostic(AvroDiagnosticCode.DuplicateSchema, span, name);
        public static AvroDiagnostic MissingReferences(SourceSpan span, IReadOnlyCollection<SchemaName> names) => new AvroDiagnostic(AvroDiagnosticCode.MissingReferences, span, string.Join(", ", names.Select(static name => $"'{name.FullName}'")));
        public static AvroDiagnostic InvalidJson(SourceSpan span, JsonException exception) => new AvroDiagnostic(AvroDiagnosticCode.InvalidJson, span, GetJsonMessage(exception));
        public static AvroDiagnostic EmptyJson(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.EmptyJson, span);
        public static AvroDiagnostic TrailingJson(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.TrailingJsonContent, span);
        public static AvroDiagnostic SchemaExpected(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.SchemaExpected, span);
        public static AvroDiagnostic ProtocolExpected(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.ProtocolExpected, span);
        public static AvroDiagnostic MissingRootSchema(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.MissingRootSchema, span);
        public static AvroDiagnostic RecursiveDefinition(SourceSpan span, SchemaName name) => new AvroDiagnostic(AvroDiagnosticCode.RecursiveSchemaDefinition, span, name.FullName);

        public static AvroDiagnostic MissingProperty(JsonSyntax syntax, string name) => new AvroDiagnostic(AvroDiagnosticCode.MissingSchemaProperty, syntax.SourceSpan, name);
        public static AvroDiagnostic InvalidSchemaReference(SourceSpan span, string value) => new AvroDiagnostic(AvroDiagnosticCode.InvalidSchemaReference, span, value);
        public static AvroDiagnostic InvalidOneWayMessage(JsonSyntax syntax, string name) => new AvroDiagnostic(AvroDiagnosticCode.InvalidOneWayMessage, syntax.SourceSpan, name);
        public static AvroDiagnostic MissingIdlRoot(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.MissingRootSchema, span);
        public static AvroDiagnostic InvalidIdlDocument(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidIdlDocument, span);
        public static AvroDiagnostic InvalidIdlDeclaration(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidIdlDeclaration, span);
        public static AvroDiagnostic InvalidIdlProperty(AvroDiagnosticCode code, SourceSpan span) => new AvroDiagnostic(code, span);
        public static AvroDiagnostic InvalidIdlFixedSize(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidIdlFixedSize, span);
        public static AvroDiagnostic InvalidIdlDecimalPrecision(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidIdlDecimalPrecision, span);
        public static AvroDiagnostic InvalidIdlDecimalScale(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidIdlDecimalScale, span);
        public static AvroDiagnostic InvalidIdlOneWayMessage(SourceSpan span, string name) => new AvroDiagnostic(AvroDiagnosticCode.InvalidIdlOneWayMessage, span, name);
        public static AvroDiagnostic ImportCycle(SourceSpan span, string cycle) => new AvroDiagnostic(AvroDiagnosticCode.ImportCycle, span, cycle);
        public static AvroDiagnostic InvalidImportFileExtension(SourceSpan span, string kind, string extension, string path) => new AvroDiagnostic(AvroDiagnosticCode.InvalidImportFileExtension, span, kind, extension, path);
        public static AvroDiagnostic MissingImport(SourceSpan span, string path) => new AvroDiagnostic(AvroDiagnosticCode.MissingImport, span, path);
        public static AvroDiagnostic InvalidImportTarget(SourceSpan span, string path, string kind) => new AvroDiagnostic(AvroDiagnosticCode.InvalidImportTarget, span, path, kind is "idl" ? "Avro IDL document" : kind);
        public static AvroDiagnostic UnusedImport(SourceSpan span, string path) => new AvroDiagnostic(AvroDiagnosticCode.UnusedImport, span, path);
    }
}
