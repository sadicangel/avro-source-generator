using AvroSourceGenerator.Avdl.Syntax;
using AvroSourceGenerator.Avsc.Syntax;
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

        // TODO: These messages pig bag on other diagnostics. Do we want to follow this way or just unwrap everything into its own code? Probably we do.

        // JSON
        public static AvroDiagnostic EmptyJson(SourceSpan sourceSpan) => AvroDiagnostic.InvalidJson(sourceSpan, "Expected a JSON value.");
        public static AvroDiagnostic TrailingJson(SourceSpan sourceSpan) => AvroDiagnostic.InvalidJson(sourceSpan, "Additional text encountered after the JSON value.");

        // AVRO
        public static AvroDiagnostic MissingRootSchema(SourceSpan sourceSpan) => AvroDiagnostic.InvalidSchema(sourceSpan, $"At least a named schema must be present in schema: {sourceSpan.SourceText.Text}");
        public static AvroDiagnostic RecursiveDefinition(SourceSpan sourceSpan, SchemaName schemaName) => AvroDiagnostic.InvalidSchema(sourceSpan, $"Recursive schema definition detected for schema '{schemaName}'.");
        public static AvroDiagnostic InvalidJsonString(JsonSyntax syntax) => AvroDiagnostic.InvalidSchema(syntax.SourceSpan, $"Expected a non-empty, non-whitespace string (found '{syntax.GetDisplayText()}') in schema: {syntax.GetRawText()}");
        public static AvroDiagnostic InvalidSchemaNameLeadingOrTrailingDot(JsonSyntax syntax) => AvroDiagnostic.InvalidSchema(syntax.SourceSpan, "Argument has an invalid name format: 'cannot start or end with a dot'");
        public static AvroDiagnostic InvalidSchemaNameLeadingOrTrailingDot(JsonPropertySyntax property) => AvroDiagnostic.InvalidSchema(property.Value.SourceSpan, $"Property '{property.Name.Value}' has an invalid format: 'cannot start or end with a dot' in schema: {property.Parent?.GetRawText()}");
        public static AvroDiagnostic InvalidSchemaNameConsecutiveDots(JsonPropertySyntax property) => AvroDiagnostic.InvalidSchema(property.Value.SourceSpan, $"Property '{property.Name.Value}' has an invalid format: 'consecutive dots are not allowed in names or namespaces' in schema: {property.Parent?.GetRawText()}");
        public static AvroDiagnostic MissingProperty(JsonSyntax syntax, string propertyName) => AvroDiagnostic.InvalidSchema(syntax.SourceSpan, $"'{propertyName}' property is required in schema: {syntax.GetRawText()}");
        public static AvroDiagnostic InvalidArray(JsonPropertySyntax property) => AvroDiagnostic.InvalidSchema(property.Value.SourceSpan, $"'{property.Name.Value}' property must be an array (found '{property.Value.GetDisplayText()}') in schema: {property.Parent?.GetRawText()}");
        public static AvroDiagnostic InvalidSchemaValue(JsonSyntax syntax) => AvroDiagnostic.InvalidSchema(syntax.SourceSpan, $"Invalid schema: {syntax.GetRawText()}");
        public static AvroDiagnostic InvalidPropertyString(JsonPropertySyntax property, bool required) => AvroDiagnostic.InvalidSchema(property.Value.SourceSpan, $"'{property.Name.Value}' property must be a {(required ? "non-empty, non-whitespace string" : "string")} (found '{property.Value.GetDisplayText()}') in schema: {property.Parent?.GetRawText()}");
        public static AvroDiagnostic InvalidPropertyBoolean(JsonSyntax syntax, JsonSyntax schema, string propertyName) => AvroDiagnostic.InvalidSchema(syntax.SourceSpan, $"'{propertyName}' property must be a boolean (found '{syntax.GetDisplayText()}') in schema: {schema.GetRawText()}");
        public static AvroDiagnostic InvalidObject(JsonSyntax syntax, JsonSyntax schema, string propertyName) => AvroDiagnostic.InvalidSchema(syntax.SourceSpan, $"'{propertyName}' property must be an object (found '{syntax.GetDisplayText()}') in schema: {schema.GetRawText()}");
        public static AvroDiagnostic InvalidStringArray(JsonPropertySyntax property) => AvroDiagnostic.InvalidSchema(property.Value.SourceSpan, $"'{property.Name.Value}' property must be an array of non-empty, non-whitespace strings in schema: {property.Parent?.GetRawText()}");
        public static AvroDiagnostic InvalidFixedSize(JsonPropertySyntax property) => AvroDiagnostic.InvalidSchema(property.Value.SourceSpan, $"'size' property must be a positive integer (found '{property.Value.GetDisplayText()}') in schema: {property.Parent?.GetRawText()}");
        public static AvroDiagnostic UnknownSchemaType(JsonSyntax syntax, string type) => AvroDiagnostic.InvalidSchema(syntax.SourceSpan, $"Unknown schema type '{type}' in {syntax.GetRawText()}");
        public static AvroDiagnostic InvalidOneWayMessage(JsonSyntax syntax, string name) => AvroDiagnostic.InvalidSchema(syntax.SourceSpan, $"One-way protocol message '{name}' must have a null response and no errors in schema: {syntax.GetRawText()}");

        // IDL
        public static AvroDiagnostic MissingIdlRoot(SourceSpan sourceSpan) => AvroDiagnostic.InvalidSource(sourceSpan, "At least a named schema must be present in source.");
        public static AvroDiagnostic InvalidIdlDocument(SourceSpan sourceSpan) => AvroDiagnostic.InvalidSource(sourceSpan, "Avro IDL files must contain a main schema directive or a single protocol declaration.");
        public static AvroDiagnostic InvalidIdlDeclaration(SourceSpan sourceSpan, SyntaxKind kind) => AvroDiagnostic.InvalidSource(sourceSpan, $"Invalid declaration in Avro IDL file: {kind}");
        public static AvroDiagnostic InvalidIdlType(SourceSpan sourceSpan, SyntaxKind kind) => AvroDiagnostic.InvalidSource(sourceSpan, $"Invalid type syntax: {kind}");
        public static AvroDiagnostic InvalidIdlSchemaDeclaration(SourceSpan sourceSpan, SyntaxKind kind) => AvroDiagnostic.InvalidSource(sourceSpan, $"Invalid declaration: {kind}");
        public static AvroDiagnostic InvalidIdlPrimitive(SourceSpan sourceSpan, SyntaxKind kind) => AvroDiagnostic.InvalidSource(sourceSpan, $"Invalid primitive type: {kind}");
        public static AvroDiagnostic InvalidIdlFixedSize(SourceSpan sourceSpan) => AvroDiagnostic.InvalidSource(sourceSpan, "Fixed size must be a positive integer.");
        public static AvroDiagnostic InvalidIdlDecimalPrecision(SourceSpan sourceSpan) => AvroDiagnostic.InvalidSource(sourceSpan, "Decimal precision must be an integer.");
        public static AvroDiagnostic InvalidIdlDecimalScale(SourceSpan sourceSpan) => AvroDiagnostic.InvalidSource(sourceSpan, "Decimal scale must be an integer.");
        public static AvroDiagnostic InvalidIdlLogicalType(SourceSpan sourceSpan, SyntaxKind kind) => AvroDiagnostic.InvalidSource(sourceSpan, $"Invalid logical type syntax: {kind}");
        public static AvroDiagnostic InvalidIdlOneWayMessage(SourceSpan sourceSpan, string name) => AvroDiagnostic.InvalidSource(sourceSpan, $"One-way protocol message '{name}' must have a null response and no errors.");
    }
}
