using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;

internal static class DiagnosticDescriptors
{
    private const string CompilerCategory = "Compiler";
    private const string ConfigurationCategory = "Configuration";

    internal static readonly DiagnosticDescriptor UnsupportedSourceType = new("AVROSG0001", "Unsupported Source Type", MessageTemplate.Get(AvroDiagnosticCode.UnsupportedSourceType), CompilerCategory, DiagnosticSeverity.Error, true, "The input file extension is not supported. Use .avsc for schemas, .avpr for protocols, or .avdl for IDL.");
    internal static readonly DiagnosticDescriptor EmptySource = new("AVROSG0002", "Empty Source", MessageTemplate.Get(AvroDiagnosticCode.EmptySource), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro input contains no non-whitespace content. Add a schema, protocol, or IDL declaration.");
    internal static readonly DiagnosticDescriptor DuplicateSourcePath = new("AVROSG0003", "Duplicate Source Path", MessageTemplate.Get(AvroDiagnosticCode.DuplicateSourcePath), CompilerCategory, DiagnosticSeverity.Error, true, "Two AdditionalFiles resolve to the same canonical source path. Give every Avro input a distinct path.");
    internal static readonly DiagnosticDescriptor DuplicateSchema = new("AVROSG0004", "Duplicate Schema", MessageTemplate.Get(AvroDiagnosticCode.DuplicateSchema), CompilerCategory, DiagnosticSeverity.Error, true, "Multiple declarations define the same fully qualified schema name. Rename or remove a declaration; only use AvroSourceGeneratorDuplicateResolution=Ignore when an arbitrary winner is acceptable.");
    internal static readonly DiagnosticDescriptor MissingReferences = new("AVROSG0005", "Missing References", MessageTemplate.Get(AvroDiagnosticCode.MissingReferences), CompilerCategory, DiagnosticSeverity.Error, true, "A named schema reference is not visible to the source file. Supply or import the referenced schema, or correct its fully qualified name.");

    internal static readonly DiagnosticDescriptor InvalidJson = new("AVROSG1000", "Invalid Json", MessageTemplate.Get(AvroDiagnosticCode.InvalidJson), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro schema or protocol file is not valid JSON. Correct the JSON syntax at the reported location.");
    internal static readonly DiagnosticDescriptor EmptyJson = new("AVROSG1001", "Empty Json", MessageTemplate.Get(AvroDiagnosticCode.EmptyJson), CompilerCategory, DiagnosticSeverity.Error, true, "A JSON value was expected before the end of the input. Provide a complete Avro schema or protocol JSON value.");
    internal static readonly DiagnosticDescriptor TrailingJsonContent = new("AVROSG1002", "Trailing Json Content", MessageTemplate.Get(AvroDiagnosticCode.TrailingJsonContent), CompilerCategory, DiagnosticSeverity.Error, true, "The file contains content after its root JSON value. Remove the trailing content.");

    internal static readonly DiagnosticDescriptor SchemaExpected = new("AVROSG2000", "Schema Expected", MessageTemplate.Get(AvroDiagnosticCode.SchemaExpected), CompilerCategory, DiagnosticSeverity.Error, true, "Provide a type name, schema object with a type property, or union array.");
    internal static readonly DiagnosticDescriptor ProtocolExpected = new("AVROSG2001", "Protocol Expected", MessageTemplate.Get(AvroDiagnosticCode.ProtocolExpected), CompilerCategory, DiagnosticSeverity.Error, true, "Provide a protocol object with a protocol property.");
    internal static readonly DiagnosticDescriptor MissingRootSchema = new("AVROSG2002", "Missing Root Schema", MessageTemplate.Get(AvroDiagnosticCode.MissingRootSchema), CompilerCategory, DiagnosticSeverity.Error, true, "The input contains no named schema or protocol that can serve as a root declaration.");
    internal static readonly DiagnosticDescriptor InvalidSchemaValue = new("AVROSG2003", "Invalid Schema Value", MessageTemplate.Get(AvroDiagnosticCode.InvalidSchemaValue), CompilerCategory, DiagnosticSeverity.Error, true, "The JSON value cannot be interpreted as an Avro schema. Use a type name, schema object, or union array.");
    internal static readonly DiagnosticDescriptor ObjectExpected = new("AVROSG2004", "Object Expected", MessageTemplate.Get(AvroDiagnosticCode.ObjectExpected), CompilerCategory, DiagnosticSeverity.Error, true, "This Avro construct must be represented by a JSON object. Replace the value with an object of the required shape.");
    internal static readonly DiagnosticDescriptor MissingSchemaProperty = new("AVROSG2005", "Missing Schema Property", MessageTemplate.Get(AvroDiagnosticCode.MissingSchemaProperty), CompilerCategory, DiagnosticSeverity.Error, true, "A required property is missing from a schema or protocol object. Add the property named by the diagnostic.");
    internal static readonly DiagnosticDescriptor InvalidStringProperty = new("AVROSG2006", "Invalid String Property", MessageTemplate.Get(AvroDiagnosticCode.InvalidStringProperty), CompilerCategory, DiagnosticSeverity.Error, true, "A property that requires a string has a value of the wrong kind or an empty required value. Supply an appropriate string.");
    internal static readonly DiagnosticDescriptor InvalidArrayProperty = new("AVROSG2007", "Invalid Array Property", MessageTemplate.Get(AvroDiagnosticCode.InvalidArrayProperty), CompilerCategory, DiagnosticSeverity.Error, true, "A property that requires a JSON array has a value of another kind. Replace it with an array of expected elements.");
    internal static readonly DiagnosticDescriptor InvalidObjectProperty = new("AVROSG2008", "Invalid Object Property", MessageTemplate.Get(AvroDiagnosticCode.InvalidObjectProperty), CompilerCategory, DiagnosticSeverity.Error, true, "A property that requires a JSON object has a value of another kind. Replace it with an object of the expected shape.");
    internal static readonly DiagnosticDescriptor InvalidBooleanProperty = new("AVROSG2009", "Invalid Boolean Property", MessageTemplate.Get(AvroDiagnosticCode.InvalidBooleanProperty), CompilerCategory, DiagnosticSeverity.Error, true, "A property that requires a JSON boolean has a value of another kind. Set it to true or false.");
    internal static readonly DiagnosticDescriptor InvalidStringArrayElement = new("AVROSG2010", "Invalid String Array Element", MessageTemplate.Get(AvroDiagnosticCode.InvalidStringArrayElement), CompilerCategory, DiagnosticSeverity.Error, true, "A string-array property contains an element that is not a non-empty string. Replace or remove the invalid element.");
    internal static readonly DiagnosticDescriptor InvalidFixedSize = new("AVROSG2011", "Invalid Fixed Size", MessageTemplate.Get(AvroDiagnosticCode.InvalidFixedSize), CompilerCategory, DiagnosticSeverity.Error, true, "A fixed schema size is not a positive 32-bit integer. Set size to the number of bytes in each fixed value.");
    internal static readonly DiagnosticDescriptor InvalidAvroName = new("AVROSG2012", "Invalid Avro Name", MessageTemplate.Get(AvroDiagnosticCode.InvalidAvroName), CompilerCategory, DiagnosticSeverity.Error, true, "A schema, field, request parameter, namespace component, or enum symbol violates Avro naming rules.");
    internal static readonly DiagnosticDescriptor UnknownSchemaType = new("AVROSG2013", "Unknown Schema Type", MessageTemplate.Get(AvroDiagnosticCode.UnknownSchemaType), CompilerCategory, DiagnosticSeverity.Error, true, "A named declaration uses an unsupported type value. Use record, error, enum, or fixed.");
    internal static readonly DiagnosticDescriptor RecursiveSchemaDefinition = new("AVROSG2014", "Recursive Schema Definition", MessageTemplate.Get(AvroDiagnosticCode.RecursiveSchemaDefinition), CompilerCategory, DiagnosticSeverity.Error, true, "A schema is nested within its own unfinished definition. Use a named reference to the enclosing schema instead.");
    internal static readonly DiagnosticDescriptor InvalidOneWayMessage = new("AVROSG2015", "Invalid One Way Message", MessageTemplate.Get(AvroDiagnosticCode.InvalidOneWayMessage), CompilerCategory, DiagnosticSeverity.Error, true, "A one-way protocol message has a non-null response or declares errors. It must return null and declare no errors.");

    internal static readonly DiagnosticDescriptor InvalidCharacter = new("AVROSG3000", "Invalid Character", MessageTemplate.Get(AvroDiagnosticCode.InvalidCharacter), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro IDL source contains a character that is invalid in the current lexical context.");
    internal static readonly DiagnosticDescriptor InvalidEscapeSequence = new("AVROSG3001", "Invalid Escape Sequence", MessageTemplate.Get(AvroDiagnosticCode.InvalidEscapeSequence), CompilerCategory, DiagnosticSeverity.Error, true, "A string or identifier contains an unsupported or incomplete escape sequence.");
    internal static readonly DiagnosticDescriptor InvalidNumber = new("AVROSG3002", "Invalid Number", MessageTemplate.Get(AvroDiagnosticCode.InvalidNumber), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro IDL source contains a malformed numeric literal.");
    internal static readonly DiagnosticDescriptor UnterminatedDocumentation = new("AVROSG3003", "Unterminated Documentation", MessageTemplate.Get(AvroDiagnosticCode.UnterminatedDocumentation), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL documentation comment reaches the end of the file before its closing delimiter.");
    internal static readonly DiagnosticDescriptor UnterminatedComment = new("AVROSG3004", "Unterminated Comment", MessageTemplate.Get(AvroDiagnosticCode.UnterminatedComment), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL block comment reaches the end of the file before its closing delimiter.");
    internal static readonly DiagnosticDescriptor UnterminatedString = new("AVROSG3005", "Unterminated String", MessageTemplate.Get(AvroDiagnosticCode.UnterminatedString), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL string literal reaches the end of a line or file before its closing quotation mark.");
    internal static readonly DiagnosticDescriptor UnterminatedVerbatimIdentifier = new("AVROSG3006", "Unterminated Verbatim Identifier", MessageTemplate.Get(AvroDiagnosticCode.UnterminatedVerbatimIdentifier), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL verbatim identifier reaches the end of the input before its closing delimiter.");
    internal static readonly DiagnosticDescriptor UnexpectedToken = new("AVROSG3007", "Unexpected Token", MessageTemplate.Get(AvroDiagnosticCode.UnexpectedToken), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro IDL parser encountered a token that is not valid in the current grammar production.");
    internal static readonly DiagnosticDescriptor UnexpectedJsonValue = new("AVROSG3008", "Unexpected Json Value", MessageTemplate.Get(AvroDiagnosticCode.UnexpectedJsonValue), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL annotation or default contains a JSON token that is not valid at the reported location.");
    internal static readonly DiagnosticDescriptor MisplacedAnnotation = new("AVROSG3009", "Misplaced Annotation", MessageTemplate.Get(AvroDiagnosticCode.MisplacedAnnotation), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL annotation is applied to a declaration or type that does not support it.");
    internal static readonly DiagnosticDescriptor MisplacedDocumentation = new("AVROSG3010", "Misplaced Documentation", MessageTemplate.Get(AvroDiagnosticCode.MisplacedDocumentation), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL documentation comment precedes a construct that cannot receive documentation.");

    internal static readonly DiagnosticDescriptor InvalidIdlDocument = new("AVROSG4000", "Invalid Idl Document", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlDocument), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro IDL document has an invalid top-level structure. Provide a main schema directive or a single protocol declaration.");
    internal static readonly DiagnosticDescriptor InvalidIdlDeclaration = new("AVROSG4001", "Invalid Idl Declaration", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlDeclaration), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL declaration is not valid in its current scope. Move, replace, or remove the declaration.");
    internal static readonly DiagnosticDescriptor InvalidIdlType = new("AVROSG4002", "Invalid Idl Type", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlType), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro IDL syntax cannot be converted to a supported Avro type.");
    internal static readonly DiagnosticDescriptor InvalidIdlSchemaDeclaration = new("AVROSG4003", "Invalid Idl Schema Declaration", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlSchemaDeclaration), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro IDL declaration cannot be converted to a named Avro schema.");
    internal static readonly DiagnosticDescriptor InvalidIdlPrimitive = new("AVROSG4004", "Invalid Idl Primitive", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlPrimitive), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro IDL type keyword is not a supported primitive type in this context.");
    internal static readonly DiagnosticDescriptor InvalidIdlFixedSize = new("AVROSG4005", "Invalid Idl Fixed Size", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlFixedSize), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL fixed declaration specifies a size that is not a positive integer.");
    internal static readonly DiagnosticDescriptor InvalidIdlDecimalPrecision = new("AVROSG4006", "Invalid Idl Decimal Precision", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlDecimalPrecision), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL decimal logical type specifies a precision that is not an integer.");
    internal static readonly DiagnosticDescriptor InvalidIdlDecimalScale = new("AVROSG4007", "Invalid Idl Decimal Scale", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlDecimalScale), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL decimal logical type specifies a scale that is not an integer.");
    internal static readonly DiagnosticDescriptor InvalidIdlLogicalType = new("AVROSG4008", "Invalid Idl Logical Type", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlLogicalType), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro IDL logical type syntax is unsupported or has an incompatible underlying type.");
    internal static readonly DiagnosticDescriptor InvalidIdlOneWayMessage = new("AVROSG4009", "Invalid Idl One Way Message", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlOneWayMessage), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL one-way message has a non-null response or declares errors.");

    internal static readonly DiagnosticDescriptor ImportCycle = new("AVROSG5000", "Import Cycle", MessageTemplate.Get(AvroDiagnosticCode.ImportCycle), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro IDL import graph contains a cycle. Remove or redirect an import so it is acyclic.");
    internal static readonly DiagnosticDescriptor InvalidImportFileExtension = new("AVROSG5001", "Invalid Import File Extension", MessageTemplate.Get(AvroDiagnosticCode.InvalidImportFileExtension), CompilerCategory, DiagnosticSeverity.Error, true, "An import kind targets a file with an incompatible extension. Use .avdl, .avpr, or .avsc for the corresponding import kind.");
    internal static readonly DiagnosticDescriptor MissingImport = new("AVROSG5002", "Missing Import", MessageTemplate.Get(AvroDiagnosticCode.MissingImport), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL import path does not match a supplied AdditionalFile relative to the importing file.");
    internal static readonly DiagnosticDescriptor InvalidImportTarget = new("AVROSG5003", "Invalid Import Target", MessageTemplate.Get(AvroDiagnosticCode.InvalidImportTarget), CompilerCategory, DiagnosticSeverity.Error, true, "An imported file exists but does not contain the Avro definition kind required by the import directive.");
    internal static readonly DiagnosticDescriptor UnusedImport = new("AVROSG5004", "Unused Import", MessageTemplate.Get(AvroDiagnosticCode.UnusedImport), CompilerCategory, DiagnosticSeverity.Warning, true, "No schema reference requires this import. Remove it or reference a declaration from the imported file.");

    internal static readonly DiagnosticDescriptor NoAvroLibraryDetected = new("AVROSG6000", "No Avro Library Detected", MessageTemplate.Get(AvroDiagnosticCode.NoAvroLibraryDetected), ConfigurationCategory, DiagnosticSeverity.Warning, true, "AvroLibrary is Auto, but no supported runtime library was detected. Install one, select one explicitly, or configure None for library-neutral generation.");
    internal static readonly DiagnosticDescriptor MultipleAvroLibrariesDetected = new("AVROSG6001", "Multiple Avro Libraries Detected", MessageTemplate.Get(AvroDiagnosticCode.MultipleAvroLibrariesDetected), ConfigurationCategory, DiagnosticSeverity.Warning, true, "AvroLibrary is Auto, but multiple supported runtime libraries were detected. Select the intended library explicitly or remove extra package references.");

    internal static DiagnosticDescriptor ToDiagnosticDescriptor(this AvroDiagnosticCode code) => code switch
    {
        AvroDiagnosticCode.UnsupportedSourceType => UnsupportedSourceType,
        AvroDiagnosticCode.EmptySource => EmptySource,
        AvroDiagnosticCode.DuplicateSourcePath => DuplicateSourcePath,
        AvroDiagnosticCode.DuplicateSchema => DuplicateSchema,
        AvroDiagnosticCode.MissingReferences => MissingReferences,

        AvroDiagnosticCode.InvalidJson => InvalidJson,
        AvroDiagnosticCode.EmptyJson => EmptyJson,
        AvroDiagnosticCode.TrailingJsonContent => TrailingJsonContent,

        AvroDiagnosticCode.SchemaExpected => SchemaExpected,
        AvroDiagnosticCode.ProtocolExpected => ProtocolExpected,
        AvroDiagnosticCode.MissingRootSchema => MissingRootSchema,
        AvroDiagnosticCode.InvalidSchemaValue => InvalidSchemaValue,
        AvroDiagnosticCode.ObjectExpected => ObjectExpected,
        AvroDiagnosticCode.MissingSchemaProperty => MissingSchemaProperty,
        AvroDiagnosticCode.InvalidStringProperty => InvalidStringProperty,
        AvroDiagnosticCode.InvalidArrayProperty => InvalidArrayProperty,
        AvroDiagnosticCode.InvalidObjectProperty => InvalidObjectProperty,
        AvroDiagnosticCode.InvalidBooleanProperty => InvalidBooleanProperty,
        AvroDiagnosticCode.InvalidStringArrayElement => InvalidStringArrayElement,
        AvroDiagnosticCode.InvalidFixedSize => InvalidFixedSize,
        AvroDiagnosticCode.InvalidAvroName => InvalidAvroName,
        AvroDiagnosticCode.UnknownSchemaType => UnknownSchemaType,
        AvroDiagnosticCode.RecursiveSchemaDefinition => RecursiveSchemaDefinition,
        AvroDiagnosticCode.InvalidOneWayMessage => InvalidOneWayMessage,

        AvroDiagnosticCode.InvalidCharacter => InvalidCharacter,
        AvroDiagnosticCode.InvalidEscapeSequence => InvalidEscapeSequence,
        AvroDiagnosticCode.InvalidNumber => InvalidNumber,
        AvroDiagnosticCode.UnterminatedDocumentation => UnterminatedDocumentation,
        AvroDiagnosticCode.UnterminatedComment => UnterminatedComment,
        AvroDiagnosticCode.UnterminatedString => UnterminatedString,
        AvroDiagnosticCode.UnterminatedVerbatimIdentifier => UnterminatedVerbatimIdentifier,
        AvroDiagnosticCode.UnexpectedToken => UnexpectedToken,
        AvroDiagnosticCode.UnexpectedJsonValue => UnexpectedJsonValue,
        AvroDiagnosticCode.MisplacedAnnotation => MisplacedAnnotation,
        AvroDiagnosticCode.MisplacedDocumentation => MisplacedDocumentation,

        AvroDiagnosticCode.InvalidIdlDocument => InvalidIdlDocument,
        AvroDiagnosticCode.InvalidIdlDeclaration => InvalidIdlDeclaration,
        AvroDiagnosticCode.InvalidIdlType => InvalidIdlType,
        AvroDiagnosticCode.InvalidIdlSchemaDeclaration => InvalidIdlSchemaDeclaration,
        AvroDiagnosticCode.InvalidIdlPrimitive => InvalidIdlPrimitive,
        AvroDiagnosticCode.InvalidIdlFixedSize => InvalidIdlFixedSize,
        AvroDiagnosticCode.InvalidIdlDecimalPrecision => InvalidIdlDecimalPrecision,
        AvroDiagnosticCode.InvalidIdlDecimalScale => InvalidIdlDecimalScale,
        AvroDiagnosticCode.InvalidIdlLogicalType => InvalidIdlLogicalType,
        AvroDiagnosticCode.InvalidIdlOneWayMessage => InvalidIdlOneWayMessage,

        AvroDiagnosticCode.ImportCycle => ImportCycle,
        AvroDiagnosticCode.InvalidImportFileExtension => InvalidImportFileExtension,
        AvroDiagnosticCode.MissingImport => MissingImport,
        AvroDiagnosticCode.InvalidImportTarget => InvalidImportTarget,
        AvroDiagnosticCode.UnusedImport => UnusedImport,

        AvroDiagnosticCode.NoAvroLibraryDetected => NoAvroLibraryDetected,
        AvroDiagnosticCode.MultipleAvroLibrariesDetected => MultipleAvroLibrariesDetected,

        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
    };
}
