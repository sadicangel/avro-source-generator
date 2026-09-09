namespace AvroSourceGenerator.Diagnostics;

public static class MessageTemplate
{
    private const string InvalidSourcePrefix = "The provided Avro IDL source is invalid: ";

    public const string DuplicateSchema = "Multiple Avro schema files would generate the same type '{0}'. " + "By default this is treated as an error to avoid ambiguous code generation. " + "You can allow one schema to be chosen arbitrarily by setting " + "<AvroSourceGeneratorDuplicateResolution>Ignore</AvroSourceGeneratorDuplicateResolution> " + "in the project file.";
    public const string InvalidImport = "The Avro import is invalid: {0}";
    public const string MissingReferences = "The following schema references could not be resolved: {0}";
    public const string NoAvroLibraryDetected = "AvroLibrary is set to 'Auto', but no supported Avro library was found. " + "AvroSourceGenerator will fall back to 'None' (no library-specific code). " + "To target a specific library, install one of: {0}. " + "To keep this behavior without warnings, set AvroSourceGeneratorAvroLibrary to 'None' in your .csproj.";
    public const string MultipleAvroLibrariesDetected = "Multiple Avro libraries are referenced: {0}. " + "Generation will fall back to 'None' (no library-specific code). " + "To target a specific library, set <AvroSourceGeneratorAvroLibrary> property to one of the following: {1} " + "in your .csproj or remove extra packages." + "To keep this behavior without warnings, set AvroSourceGeneratorAvroLibrary to 'None'.";
    public const string InvalidCharacter = InvalidSourcePrefix + "Invalid character input: '{0}'";
    public const string InvalidEscapeSequence = InvalidSourcePrefix + "Invalid escape sequence: '{0}'";
    public const string InvalidNumber = InvalidSourcePrefix + "Invalid number literal: '{0}'";
    public const string UnterminatedDocumentation = InvalidSourcePrefix + "Unterminated documentation comment";
    public const string UnterminatedComment = InvalidSourcePrefix + "Unterminated comment";
    public const string UnterminatedString = InvalidSourcePrefix + "Unterminated string literal";
    public const string UnterminatedVerbatimIdentifier = InvalidSourcePrefix + "Unterminated verbatim identifier";
    public const string UnexpectedToken = InvalidSourcePrefix + "Unexpected token '{0}'. Expected '{1}'";
    public const string UnexpectedJsonValue = InvalidSourcePrefix + "Unexpected JSON value token '{0}'";
    public const string MisplacedAnnotation = InvalidSourcePrefix + "Annotation '@{0}' is not valid on {1}";
    public const string MisplacedDocumentation = InvalidSourcePrefix + "Documentation comment is not valid on {0}";
    public const string InvalidSource = InvalidSourcePrefix + "{0}";
    public const string InvalidSchema = "The schema defined in the JSON is invalid: {0}";
    public const string InvalidJson = "The provided JSON is invalid: {0}";
    public const string UnknownError = "An unknown error occurred in Avro Source Generator: {0}";
}
