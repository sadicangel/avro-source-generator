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

    private static string Display(JsonSyntax syntax) => syntax.GetDisplayText();
    private static string? Display(SyntaxKind kind) => SyntaxFacts.GetDisplayText(kind) ?? kind.ToString();

    extension(AvroDiagnostic)
    {
        public static AvroDiagnostic InvalidCharacter(SourceSpan span) => new(AvroDiagnosticCode.InvalidCharacter, span, span.ToString());
        public static AvroDiagnostic InvalidEscapeSequence(SourceSpan span) => new(AvroDiagnosticCode.InvalidEscapeSequence, span, span.ToString());
        public static AvroDiagnostic InvalidNumber(SourceSpan span) => new(AvroDiagnosticCode.InvalidNumber, span, span.ToString());
        public static AvroDiagnostic UnterminatedDocumentation(SourceSpan span) => new(AvroDiagnosticCode.UnterminatedDocumentation, span);
        public static AvroDiagnostic UnterminatedComment(SourceSpan span) => new(AvroDiagnosticCode.UnterminatedComment, span);
        public static AvroDiagnostic UnterminatedString(SourceSpan span) => new(AvroDiagnosticCode.UnterminatedString, span);
        public static AvroDiagnostic UnterminatedVerbatimIdentifier(SourceSpan span) => new(AvroDiagnosticCode.UnterminatedVerbatimIdentifier, span);
        public static AvroDiagnostic UnexpectedToken(SyntaxKind expected, SyntaxToken actual) => new(AvroDiagnosticCode.UnexpectedToken, actual.SourceSpan, SyntaxFacts.GetDisplayText(actual.SyntaxKind) ?? actual.ValueText, Display(expected));
        public static AvroDiagnostic UnexpectedJsonValue(SyntaxToken actual) => new(AvroDiagnosticCode.UnexpectedJsonValue, actual.SourceSpan, actual.ValueText);
        public static AvroDiagnostic MisplacedAnnotation(SourceSpan span, string name, string target) => new(AvroDiagnosticCode.MisplacedAnnotation, span, name, target);
        public static AvroDiagnostic MisplacedDocumentation(SourceSpan span, string target) => new(AvroDiagnosticCode.MisplacedDocumentation, span, target);
        public static AvroDiagnostic UnsupportedSourceType(SourceSpan span) => new(AvroDiagnosticCode.UnsupportedSourceType, span);
        public static AvroDiagnostic EmptySource(SourceSpan span) => new(AvroDiagnosticCode.EmptySource, span);
        public static AvroDiagnostic DuplicateSourcePath(SourceSpan span, string path, string original) => new(AvroDiagnosticCode.DuplicateSourcePath, span, path, original);
        public static AvroDiagnostic DuplicateSchema(SourceSpan span, string name) => new(AvroDiagnosticCode.DuplicateSchema, span, name);
        public static AvroDiagnostic MissingReferences(SourceSpan span, IReadOnlyCollection<SchemaName> names) => new(AvroDiagnosticCode.MissingReferences, span, string.Join(", ", names.Select(static name => name.FullName)));
        public static AvroDiagnostic InvalidJson(SourceSpan span, string message) => new(AvroDiagnosticCode.InvalidJson, span, message);
        public static AvroDiagnostic EmptyJson(SourceSpan span) => new(AvroDiagnosticCode.EmptyJson, span);
        public static AvroDiagnostic TrailingJson(SourceSpan span) => new(AvroDiagnosticCode.TrailingJsonContent, span);
        public static AvroDiagnostic SchemaExpected(SourceSpan span) => new(AvroDiagnosticCode.SchemaExpected, span);
        public static AvroDiagnostic ProtocolExpected(SourceSpan span) => new(AvroDiagnosticCode.ProtocolExpected, span);
        public static AvroDiagnostic MissingRootSchema(SourceSpan span) => new(AvroDiagnosticCode.MissingRootSchema, span);
        public static AvroDiagnostic RecursiveDefinition(SourceSpan span, SchemaName name) => new(AvroDiagnosticCode.RecursiveSchemaDefinition, span, name.FullName);
        public static AvroDiagnostic InvalidJsonString(JsonSyntax syntax) => new(AvroDiagnosticCode.InvalidStringProperty, syntax.SourceSpan, "value", "a non-empty string", Display(syntax));
        public static AvroDiagnostic InvalidAvroName(JsonSyntax syntax) => new(AvroDiagnosticCode.InvalidAvroName, syntax.SourceSpan, Display(syntax));
        public static AvroDiagnostic InvalidAvroName(JsonPropertySyntax property) => new(AvroDiagnosticCode.InvalidAvroName, property.Value.SourceSpan, property.Value.GetDisplayText());
        public static AvroDiagnostic MissingProperty(JsonSyntax syntax, string name) => new(AvroDiagnosticCode.MissingSchemaProperty, syntax.SourceSpan, name);
        public static AvroDiagnostic InvalidArray(JsonPropertySyntax property) => new(AvroDiagnosticCode.InvalidArrayProperty, property.Value.SourceSpan, property.Name.Value, Display(property.Value));
        public static AvroDiagnostic InvalidSchemaValue(JsonSyntax syntax) => new(AvroDiagnosticCode.InvalidSchemaValue, syntax.SourceSpan, Display(syntax));
        public static AvroDiagnostic InvalidSchemaValue(SourceSpan span, string value) => new(AvroDiagnosticCode.InvalidSchemaValue, span, value);
        public static AvroDiagnostic InvalidObject(JsonSyntax value, JsonSyntax _, string name) => new(AvroDiagnosticCode.InvalidObjectProperty, value.SourceSpan, name, Display(value));
        public static AvroDiagnostic ObjectExpected(JsonSyntax syntax) => new(AvroDiagnosticCode.ObjectExpected, syntax.SourceSpan, Display(syntax));
        public static AvroDiagnostic InvalidPropertyString(JsonPropertySyntax property, bool required) => new(AvroDiagnosticCode.InvalidStringProperty, property.Value.SourceSpan, property.Name.Value, required ? "a non-empty string" : "a string", Display(property.Value));
        public static AvroDiagnostic InvalidPropertyBoolean(JsonSyntax value, JsonSyntax _, string name) => new(AvroDiagnosticCode.InvalidBooleanProperty, value.SourceSpan, name, Display(value));
        public static AvroDiagnostic InvalidStringArray(JsonPropertySyntax property) => new(AvroDiagnosticCode.InvalidStringArrayElement, property.Value.SourceSpan, property.Name.Value);
        public static AvroDiagnostic InvalidFixedSize(JsonPropertySyntax property) => new(AvroDiagnosticCode.InvalidFixedSize, property.Value.SourceSpan, Display(property.Value));
        public static AvroDiagnostic UnknownSchemaType(JsonSyntax _, string type) => new(AvroDiagnosticCode.UnknownSchemaType, _.SourceSpan, type);
        public static AvroDiagnostic InvalidOneWayMessage(JsonSyntax syntax, string name) => new(AvroDiagnosticCode.InvalidOneWayMessage, syntax.SourceSpan, name);
        public static AvroDiagnostic MissingIdlRoot(SourceSpan span) => new(AvroDiagnosticCode.MissingRootSchema, span);
        public static AvroDiagnostic InvalidIdlDocument(SourceSpan span) => new(AvroDiagnosticCode.InvalidIdlDocument, span);
        public static AvroDiagnostic InvalidIdlDeclaration(SourceSpan span, SyntaxKind kind) => new(AvroDiagnosticCode.InvalidIdlDeclaration, span, kind);
        public static AvroDiagnostic InvalidIdlDeclaration(SourceSpan span, string description) => new(AvroDiagnosticCode.InvalidIdlDeclaration, span, description);
        public static AvroDiagnostic InvalidIdlType(SourceSpan span, SyntaxKind kind) => new(AvroDiagnosticCode.InvalidIdlType, span, kind);
        public static AvroDiagnostic InvalidIdlSchemaDeclaration(SourceSpan span, SyntaxKind kind) => new(AvroDiagnosticCode.InvalidIdlSchemaDeclaration, span, kind);
        public static AvroDiagnostic InvalidIdlPrimitive(SourceSpan span, SyntaxKind kind) => new(AvroDiagnosticCode.InvalidIdlPrimitive, span, kind);
        public static AvroDiagnostic InvalidIdlFixedSize(SourceSpan span) => new(AvroDiagnosticCode.InvalidIdlFixedSize, span);
        public static AvroDiagnostic InvalidIdlDecimalPrecision(SourceSpan span) => new(AvroDiagnosticCode.InvalidIdlDecimalPrecision, span);
        public static AvroDiagnostic InvalidIdlDecimalScale(SourceSpan span) => new(AvroDiagnosticCode.InvalidIdlDecimalScale, span);
        public static AvroDiagnostic InvalidIdlLogicalType(SourceSpan span, SyntaxKind kind) => new(AvroDiagnosticCode.InvalidIdlLogicalType, span, kind);
        public static AvroDiagnostic InvalidIdlOneWayMessage(SourceSpan span, string name) => new(AvroDiagnosticCode.InvalidIdlOneWayMessage, span, name);
        public static AvroDiagnostic ImportCycle(SourceSpan span, string cycle) => new(AvroDiagnosticCode.ImportCycle, span, cycle);
        public static AvroDiagnostic InvalidImportFileExtension(SourceSpan span, string kind, string extension, string path) => new(AvroDiagnosticCode.InvalidImportFileExtension, span, kind, extension, path);
        public static AvroDiagnostic MissingImport(SourceSpan span, string path) => new(AvroDiagnosticCode.MissingImport, span, path);
        public static AvroDiagnostic InvalidImportTarget(SourceSpan span, string path, string kind) => new(AvroDiagnosticCode.InvalidImportTarget, span, path, kind);
    }
}
