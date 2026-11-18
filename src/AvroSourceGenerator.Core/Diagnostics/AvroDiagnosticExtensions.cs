using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Syntax;
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
        public static AvroDiagnostic DuplicateField(SourceSpan span, string name, SchemaName owner) => new AvroDiagnostic(AvroDiagnosticCode.DuplicateField, span, name, owner.FullName);
        public static AvroDiagnostic DuplicateSymbol(SourceSpan span, string name, SchemaName owner) => new AvroDiagnostic(AvroDiagnosticCode.DuplicateEnumSymbol, span, name, owner.FullName);
        public static AvroDiagnostic DuplicateUnionBranch(SourceSpan span, string type) => new AvroDiagnostic(AvroDiagnosticCode.DuplicateUnionBranch, span, type);
        public static AvroDiagnostic NestedUnion(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.NestedUnion, span);
        public static AvroDiagnostic MissingReferences(SourceSpan span, IReadOnlyCollection<SchemaName> names) => new AvroDiagnostic(AvroDiagnosticCode.MissingReferences, span, string.Join(", ", names.Select(static name => $"'{name.FullName}'")));
        public static AvroDiagnostic EmptyJson(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.EmptyJson, span);
        public static AvroDiagnostic TrailingJson(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.TrailingJsonContent, span);
        public static AvroDiagnostic JsonCommentNotAllowed(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.JsonCommentNotAllowed, span);
        public static AvroDiagnostic JsonPropertyNameUnpairedSurrogate(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.JsonPropertyNameUnpairedSurrogate, span);
        public static AvroDiagnostic JsonNestingLimitExceeded(SourceSpan span, int maxDepth) => new AvroDiagnostic(AvroDiagnosticCode.JsonNestingLimitExceeded, span, maxDepth);
        public static AvroDiagnostic SchemaExpected(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.SchemaExpected, span);
        public static AvroDiagnostic ProtocolExpected(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.ProtocolExpected, span);
        public static AvroDiagnostic MissingRootSchema(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.MissingRootSchema, span);
        public static AvroDiagnostic RecursiveDefinition(SourceSpan span, SchemaName name) => new AvroDiagnostic(AvroDiagnosticCode.RecursiveSchemaDefinition, span, name.FullName);

        public static AvroDiagnostic MissingProperty(SourceSpan span, string name) => new AvroDiagnostic(AvroDiagnosticCode.MissingSchemaProperty, span, name);
        public static AvroDiagnostic InvalidSchemaName(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidSchemaName, span);
        public static AvroDiagnostic InvalidProtocolName(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidProtocolName, span);
        public static AvroDiagnostic InvalidFieldName(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidFieldName, span);
        public static AvroDiagnostic InvalidRequestParameterName(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidRequestParameterName, span);
        public static AvroDiagnostic InvalidNamespace(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidNamespace, span);
        public static AvroDiagnostic InvalidAliases(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidAliases, span);
        public static AvroDiagnostic InvalidDoc(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidDoc, span);
        public static AvroDiagnostic InvalidLogicalType(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidLogicalType, span);
        public static AvroDiagnostic InvalidDecimalPrecision(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidDecimalPrecision, span);
        public static AvroDiagnostic InvalidDecimalScale(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidDecimalScale, span);
        public static AvroDiagnostic InvalidSchemaType(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidSchemaType, span);
        public static AvroDiagnostic InvalidFieldType(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidFieldType, span);
        public static AvroDiagnostic InvalidParameterType(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidParameterType, span);
        public static AvroDiagnostic InvalidProtocolDeclarationType(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidProtocolDeclarationType, span);
        public static AvroDiagnostic InvalidFields(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidFields, span);
        public static AvroDiagnostic InvalidOrder(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidOrder, span);
        public static AvroDiagnostic InvalidItems(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidItems, span);
        public static AvroDiagnostic InvalidValues(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidValues, span);
        public static AvroDiagnostic InvalidSymbols(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidSymbols, span);
        public static AvroDiagnostic InvalidEnumDefault(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidEnumDefault, span);
        public static AvroDiagnostic InvalidFixedSize(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidFixedSize, span);
        public static AvroDiagnostic InvalidTypes(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidTypes, span);
        public static AvroDiagnostic InvalidMessages(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidMessages, span);
        public static AvroDiagnostic InvalidRequest(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidRequest, span);
        public static AvroDiagnostic InvalidResponse(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidResponse, span);
        public static AvroDiagnostic InvalidErrors(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidErrors, span);
        public static AvroDiagnostic InvalidOneWay(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidOneWay, span);

        public static AvroDiagnostic InvalidSchemaReference(SourceSpan span, string value) => new AvroDiagnostic(AvroDiagnosticCode.InvalidSchemaReference, span, value);
        public static AvroDiagnostic InvalidOneWayMessage(SourceSpan span, string name) => new AvroDiagnostic(AvroDiagnosticCode.InvalidOneWayMessage, span, name);
        public static AvroDiagnostic MissingIdlRoot(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.MissingRootSchema, span);
        public static AvroDiagnostic InvalidIdlDocument(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidIdlDocument, span);
        public static AvroDiagnostic InvalidIdlDeclaration(SourceSpan span) => new AvroDiagnostic(AvroDiagnosticCode.InvalidIdlDeclaration, span);
        public static AvroDiagnostic DuplicateIdlDirective(SourceSpan span, string directive) => new AvroDiagnostic(AvroDiagnosticCode.DuplicateIdlDirective, span, directive);
        public static AvroDiagnostic ImportCycle(SourceSpan span, string cycle) => new AvroDiagnostic(AvroDiagnosticCode.ImportCycle, span, cycle);
        public static AvroDiagnostic InvalidImportFileExtension(SourceSpan span, string kind, string extension, string path) => new AvroDiagnostic(AvroDiagnosticCode.InvalidImportFileExtension, span, kind, extension, path);
        public static AvroDiagnostic MissingImport(SourceSpan span, string path) => new AvroDiagnostic(AvroDiagnosticCode.MissingImport, span, path);
        public static AvroDiagnostic InvalidImportTarget(SourceSpan span, string path, string kind) => new AvroDiagnostic(AvroDiagnosticCode.InvalidImportTarget, span, path, kind is "idl" ? "Avro IDL document" : kind);
        public static AvroDiagnostic UnusedImport(SourceSpan span, string path) => new AvroDiagnostic(AvroDiagnosticCode.UnusedImport, span, path);
    }
}
