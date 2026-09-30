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

    internal static readonly DiagnosticDescriptor InvalidJson = new("AVROSG1000", "Invalid JSON", MessageTemplate.Get(AvroDiagnosticCode.InvalidJson), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro schema or protocol file is not valid JSON. Correct the JSON syntax at the reported location.");
    internal static readonly DiagnosticDescriptor EmptyJson = new("AVROSG1001", "Empty JSON", MessageTemplate.Get(AvroDiagnosticCode.EmptyJson), CompilerCategory, DiagnosticSeverity.Error, true, "A JSON value was expected before the end of the input. Provide a complete Avro schema or protocol JSON value.");
    internal static readonly DiagnosticDescriptor TrailingJsonContent = new("AVROSG1002", "Trailing JSON Content", MessageTemplate.Get(AvroDiagnosticCode.TrailingJsonContent), CompilerCategory, DiagnosticSeverity.Error, true, "The file contains content after its root JSON value. Remove the trailing content.");

    internal static readonly DiagnosticDescriptor SchemaExpected = new("AVROSG2000", "Schema Expected", MessageTemplate.Get(AvroDiagnosticCode.SchemaExpected), CompilerCategory, DiagnosticSeverity.Error, true, "Provide a type name, schema object with a type property, or union array.");
    internal static readonly DiagnosticDescriptor ProtocolExpected = new("AVROSG2001", "Protocol Expected", MessageTemplate.Get(AvroDiagnosticCode.ProtocolExpected), CompilerCategory, DiagnosticSeverity.Error, true, "Provide a protocol object with a protocol property.");
    internal static readonly DiagnosticDescriptor MissingRootSchema = new("AVROSG2002", "Missing Root Schema", MessageTemplate.Get(AvroDiagnosticCode.MissingRootSchema), CompilerCategory, DiagnosticSeverity.Error, true, "The input contains no named schema or protocol that can serve as a root declaration.");
    internal static readonly DiagnosticDescriptor InvalidSchemaReference = new("AVROSG2003", "Invalid Schema Reference", MessageTemplate.Get(AvroDiagnosticCode.InvalidSchemaReference), CompilerCategory, DiagnosticSeverity.Error, true, "A type reference must contain a valid Avro name, optionally prefixed with its namespace.");
    internal static readonly DiagnosticDescriptor MissingSchemaProperty = new("AVROSG2004", "Missing Schema Property", MessageTemplate.Get(AvroDiagnosticCode.MissingSchemaProperty), CompilerCategory, DiagnosticSeverity.Error, true, "A required property is missing from a schema or protocol object. Add the property named by the diagnostic.");
    internal static readonly DiagnosticDescriptor InvalidSchemaName = new("AVROSG2005", "Invalid Schema Name", MessageTemplate.Get(AvroDiagnosticCode.InvalidSchemaName), CompilerCategory, DiagnosticSeverity.Error, true, "Property 'name' must be a non-empty string containing a valid Avro name, optionally prefixed with its namespace.");
    internal static readonly DiagnosticDescriptor InvalidProtocolName = new("AVROSG2006", "Invalid Protocol Name", MessageTemplate.Get(AvroDiagnosticCode.InvalidProtocolName), CompilerCategory, DiagnosticSeverity.Error, true, "Property 'protocol' must be a non-empty string containing a valid Avro name, optionally prefixed with its namespace.");
    internal static readonly DiagnosticDescriptor InvalidFieldName = new("AVROSG2007", "Invalid Field Name", MessageTemplate.Get(AvroDiagnosticCode.InvalidFieldName), CompilerCategory, DiagnosticSeverity.Error, true, "A field name must be a non-empty Avro name.");
    internal static readonly DiagnosticDescriptor InvalidRequestParameterName = new("AVROSG2008", "Invalid Request Parameter Name", MessageTemplate.Get(AvroDiagnosticCode.InvalidRequestParameterName), CompilerCategory, DiagnosticSeverity.Error, true, "A request parameter name must be a non-empty Avro name.");
    internal static readonly DiagnosticDescriptor InvalidNamespace = new("AVROSG2009", "Invalid Namespace", MessageTemplate.Get(AvroDiagnosticCode.InvalidNamespace), CompilerCategory, DiagnosticSeverity.Error, true, "A namespace must be null or a string containing valid dot-separated Avro names.");
    internal static readonly DiagnosticDescriptor InvalidSchemaType = new("AVROSG2010", "Invalid Schema Type", MessageTemplate.Get(AvroDiagnosticCode.InvalidSchemaType), CompilerCategory, DiagnosticSeverity.Error, true, "Property 'type' must be a non-empty string naming an Avro type or referencing a named type.");
    internal static readonly DiagnosticDescriptor InvalidFieldType = new("AVROSG2011", "Invalid Field Type", MessageTemplate.Get(AvroDiagnosticCode.InvalidFieldType), CompilerCategory, DiagnosticSeverity.Error, true, "Field property 'type' must be a primitive type name, a named type reference, an inline type definition, or a union.");
    internal static readonly DiagnosticDescriptor InvalidParameterType = new("AVROSG2012", "Invalid Parameter Type", MessageTemplate.Get(AvroDiagnosticCode.InvalidParameterType), CompilerCategory, DiagnosticSeverity.Error, true, "Request parameter property 'type' must be a primitive type name, a named type reference, an inline type definition, or a union.");
    internal static readonly DiagnosticDescriptor InvalidFields = new("AVROSG2013", "Invalid Fields", MessageTemplate.Get(AvroDiagnosticCode.InvalidFields), CompilerCategory, DiagnosticSeverity.Error, true, "A fields property must be an array of field objects.");
    internal static readonly DiagnosticDescriptor InvalidItems = new("AVROSG2014", "Invalid Items", MessageTemplate.Get(AvroDiagnosticCode.InvalidItems), CompilerCategory, DiagnosticSeverity.Error, true, "Property 'items' must be a primitive type name, a named type reference, an inline type definition, or a union.");
    internal static readonly DiagnosticDescriptor InvalidValues = new("AVROSG2015", "Invalid Values", MessageTemplate.Get(AvroDiagnosticCode.InvalidValues), CompilerCategory, DiagnosticSeverity.Error, true, "Property 'values' must be a primitive type name, a named type reference, an inline type definition, or a union.");
    internal static readonly DiagnosticDescriptor InvalidSymbols = new("AVROSG2016", "Invalid Symbols", MessageTemplate.Get(AvroDiagnosticCode.InvalidSymbols), CompilerCategory, DiagnosticSeverity.Error, true, "A symbols property must be an array of non-empty Avro names.");
    internal static readonly DiagnosticDescriptor InvalidAliases = new("AVROSG2017", "Invalid Aliases", MessageTemplate.Get(AvroDiagnosticCode.InvalidAliases), CompilerCategory, DiagnosticSeverity.Error, true, "Property 'aliases' must be an array of non-empty strings, or null.");
    internal static readonly DiagnosticDescriptor InvalidDoc = new("AVROSG2018", "Invalid Doc", MessageTemplate.Get(AvroDiagnosticCode.InvalidDoc), CompilerCategory, DiagnosticSeverity.Error, true, "A doc property must be a string or null.");
    internal static readonly DiagnosticDescriptor InvalidLogicalType = new("AVROSG2019", "Invalid Logical Type", MessageTemplate.Get(AvroDiagnosticCode.InvalidLogicalType), CompilerCategory, DiagnosticSeverity.Error, true, "A logicalType property must be a non-empty string.");
    internal static readonly DiagnosticDescriptor InvalidTypes = new("AVROSG2020", "Invalid Types", MessageTemplate.Get(AvroDiagnosticCode.InvalidTypes), CompilerCategory, DiagnosticSeverity.Error, true, "Property 'types' must be an array of record, error, enum, or fixed definitions.");
    internal static readonly DiagnosticDescriptor InvalidMessages = new("AVROSG2021", "Invalid Messages", MessageTemplate.Get(AvroDiagnosticCode.InvalidMessages), CompilerCategory, DiagnosticSeverity.Error, true, "A messages property must be an object whose values are message objects.");
    internal static readonly DiagnosticDescriptor InvalidRequest = new("AVROSG2022", "Invalid Request", MessageTemplate.Get(AvroDiagnosticCode.InvalidRequest), CompilerCategory, DiagnosticSeverity.Error, true, "A request property must be an array of parameter objects.");
    internal static readonly DiagnosticDescriptor InvalidResponse = new("AVROSG2023", "Invalid Response", MessageTemplate.Get(AvroDiagnosticCode.InvalidResponse), CompilerCategory, DiagnosticSeverity.Error, true, "Property 'response' must be a primitive type name, a named type reference, an inline type definition, or a union.");
    internal static readonly DiagnosticDescriptor InvalidErrors = new("AVROSG2024", "Invalid Errors", MessageTemplate.Get(AvroDiagnosticCode.InvalidErrors), CompilerCategory, DiagnosticSeverity.Error, true, "Property 'errors' must be null or an array of primitive type names, named type references, inline type definitions, or unions.");
    internal static readonly DiagnosticDescriptor InvalidOneWay = new("AVROSG2025", "Invalid One Way", MessageTemplate.Get(AvroDiagnosticCode.InvalidOneWay), CompilerCategory, DiagnosticSeverity.Error, true, "Property 'one-way' must be a boolean or null.");
    internal static readonly DiagnosticDescriptor InvalidEnumDefault = new("AVROSG2026", "Invalid Enum Default", MessageTemplate.Get(AvroDiagnosticCode.InvalidEnumDefault), CompilerCategory, DiagnosticSeverity.Error, true, "An enum default property must be a string or null.");
    internal static readonly DiagnosticDescriptor InvalidProtocolDeclarationType = new("AVROSG2027", "Invalid Protocol Declaration Type", MessageTemplate.Get(AvroDiagnosticCode.InvalidProtocolDeclarationType), CompilerCategory, DiagnosticSeverity.Error, true, "A type declaration inside a protocol must be a record, error, enum, or fixed schema.");
    internal static readonly DiagnosticDescriptor InvalidFixedSize = new("AVROSG2029", "Invalid Fixed Size", MessageTemplate.Get(AvroDiagnosticCode.InvalidFixedSize), CompilerCategory, DiagnosticSeverity.Error, true, "A fixed schema size must be a positive 32-bit integer.");
    internal static readonly DiagnosticDescriptor RecursiveSchemaDefinition = new("AVROSG2030", "Recursive Schema Definition", MessageTemplate.Get(AvroDiagnosticCode.RecursiveSchemaDefinition), CompilerCategory, DiagnosticSeverity.Error, true, "A schema is nested within its own unfinished definition. Use a named reference to the enclosing schema instead.");
    internal static readonly DiagnosticDescriptor InvalidOneWayMessage = new("AVROSG2031", "Invalid One Way Message", MessageTemplate.Get(AvroDiagnosticCode.InvalidOneWayMessage), CompilerCategory, DiagnosticSeverity.Error, true, "A one-way protocol message has a non-null response or declares errors. It must return null and declare no errors.");

    internal static readonly DiagnosticDescriptor InvalidCharacter = new("AVROSG3000", "Invalid Character", MessageTemplate.Get(AvroDiagnosticCode.InvalidCharacter), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro IDL source contains a character that is invalid in the current lexical context.");
    internal static readonly DiagnosticDescriptor InvalidEscapeSequence = new("AVROSG3001", "Invalid Escape Sequence", MessageTemplate.Get(AvroDiagnosticCode.InvalidEscapeSequence), CompilerCategory, DiagnosticSeverity.Error, true, "A string or identifier contains an unsupported or incomplete escape sequence.");
    internal static readonly DiagnosticDescriptor InvalidNumber = new("AVROSG3002", "Invalid Number", MessageTemplate.Get(AvroDiagnosticCode.InvalidNumber), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro IDL source contains a malformed numeric literal.");
    internal static readonly DiagnosticDescriptor UnterminatedDocumentation = new("AVROSG3003", "Unterminated Documentation", MessageTemplate.Get(AvroDiagnosticCode.UnterminatedDocumentation), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL documentation comment reaches the end of the file before its closing delimiter.");
    internal static readonly DiagnosticDescriptor UnterminatedComment = new("AVROSG3004", "Unterminated Comment", MessageTemplate.Get(AvroDiagnosticCode.UnterminatedComment), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL block comment reaches the end of the file before its closing delimiter.");
    internal static readonly DiagnosticDescriptor UnterminatedString = new("AVROSG3005", "Unterminated String", MessageTemplate.Get(AvroDiagnosticCode.UnterminatedString), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL string literal reaches the end of a line or file before its closing quotation mark.");
    internal static readonly DiagnosticDescriptor UnterminatedVerbatimIdentifier = new("AVROSG3006", "Unterminated Verbatim Identifier", MessageTemplate.Get(AvroDiagnosticCode.UnterminatedVerbatimIdentifier), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL verbatim identifier reaches the end of the input before its closing delimiter.");
    internal static readonly DiagnosticDescriptor UnexpectedToken = new("AVROSG3007", "Unexpected Token", MessageTemplate.Get(AvroDiagnosticCode.UnexpectedToken), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro IDL parser encountered a token that is not valid in the current grammar production.");
    internal static readonly DiagnosticDescriptor UnexpectedJsonValue = new("AVROSG3008", "Unexpected JSON Value", MessageTemplate.Get(AvroDiagnosticCode.UnexpectedJsonValue), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL annotation or default contains a JSON token that is not valid at the reported location.");
    internal static readonly DiagnosticDescriptor MisplacedAnnotation = new("AVROSG3009", "Misplaced Annotation", MessageTemplate.Get(AvroDiagnosticCode.MisplacedAnnotation), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL annotation is applied to a declaration or type that does not support it.");
    internal static readonly DiagnosticDescriptor MisplacedDocumentation = new("AVROSG3010", "Misplaced Documentation", MessageTemplate.Get(AvroDiagnosticCode.MisplacedDocumentation), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL documentation comment precedes a construct that cannot receive documentation.");

    internal static readonly DiagnosticDescriptor InvalidIdlDocument = new("AVROSG4000", "Invalid IDL Document", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlDocument), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro IDL document has an invalid top-level structure. Provide a main schema directive or a single protocol declaration.");
    internal static readonly DiagnosticDescriptor InvalidIdlDeclaration = new("AVROSG4001", "Invalid IDL Declaration", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlDeclaration), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL declaration is not valid in its current scope. Move, replace, or remove the declaration.");
    internal static readonly DiagnosticDescriptor InvalidIdlNamespace = new("AVROSG4002", "Invalid IDL Namespace", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlNamespace), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL namespace annotation requires a string value.");
    internal static readonly DiagnosticDescriptor InvalidIdlAliases = new("AVROSG4003", "Invalid IDL Aliases", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlAliases), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL aliases annotation requires an array of strings.");
    internal static readonly DiagnosticDescriptor InvalidIdlLogicalTypeAnnotation = new("AVROSG4004", "Invalid IDL Logical Type Annotation", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlLogicalTypeAnnotation), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL logicalType annotation requires a string value.");
    internal static readonly DiagnosticDescriptor InvalidIdlOrder = new("AVROSG4005", "Invalid IDL Order", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlOrder), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL order annotation requires a string value.");
    internal static readonly DiagnosticDescriptor InvalidIdlEnumDefault = new("AVROSG4006", "Invalid IDL Enum Default", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlEnumDefault), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL enum default requires a string or null.");
    internal static readonly DiagnosticDescriptor InvalidIdlFixedSize = new("AVROSG4010", "Invalid IDL Fixed Size", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlFixedSize), CompilerCategory, DiagnosticSeverity.Error, true, "Fixed size must be a positive 32-bit integer.");
    internal static readonly DiagnosticDescriptor InvalidIdlDecimalPrecision = new("AVROSG4011", "Invalid IDL Decimal Precision", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlDecimalPrecision), CompilerCategory, DiagnosticSeverity.Error, true, "Decimal precision must be a 32-bit integer.");
    internal static readonly DiagnosticDescriptor InvalidIdlDecimalScale = new("AVROSG4012", "Invalid IDL Decimal Scale", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlDecimalScale), CompilerCategory, DiagnosticSeverity.Error, true, "Decimal scale must be a 32-bit integer.");
    internal static readonly DiagnosticDescriptor InvalidIdlOneWayMessage = new("AVROSG4014", "Invalid IDL One Way Message", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlOneWayMessage), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL one-way message has a non-null response or declares errors.");

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
        AvroDiagnosticCode.MissingSchemaProperty => MissingSchemaProperty,
        AvroDiagnosticCode.InvalidSchemaName => InvalidSchemaName,
        AvroDiagnosticCode.InvalidProtocolName => InvalidProtocolName,
        AvroDiagnosticCode.InvalidFieldName => InvalidFieldName,
        AvroDiagnosticCode.InvalidRequestParameterName => InvalidRequestParameterName,
        AvroDiagnosticCode.InvalidNamespace => InvalidNamespace,
        AvroDiagnosticCode.InvalidSchemaType => InvalidSchemaType,
        AvroDiagnosticCode.InvalidFieldType => InvalidFieldType,
        AvroDiagnosticCode.InvalidParameterType => InvalidParameterType,
        AvroDiagnosticCode.InvalidFields => InvalidFields,
        AvroDiagnosticCode.InvalidItems => InvalidItems,
        AvroDiagnosticCode.InvalidValues => InvalidValues,
        AvroDiagnosticCode.InvalidSymbols => InvalidSymbols,
        AvroDiagnosticCode.InvalidAliases => InvalidAliases,
        AvroDiagnosticCode.InvalidDoc => InvalidDoc,
        AvroDiagnosticCode.InvalidLogicalType => InvalidLogicalType,
        AvroDiagnosticCode.InvalidTypes => InvalidTypes,
        AvroDiagnosticCode.InvalidMessages => InvalidMessages,
        AvroDiagnosticCode.InvalidRequest => InvalidRequest,
        AvroDiagnosticCode.InvalidResponse => InvalidResponse,
        AvroDiagnosticCode.InvalidErrors => InvalidErrors,
        AvroDiagnosticCode.InvalidOneWay => InvalidOneWay,
        AvroDiagnosticCode.InvalidEnumDefault => InvalidEnumDefault,
        AvroDiagnosticCode.InvalidProtocolDeclarationType => InvalidProtocolDeclarationType,
        AvroDiagnosticCode.InvalidSchemaReference => InvalidSchemaReference,
        AvroDiagnosticCode.InvalidFixedSize => InvalidFixedSize,
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
        AvroDiagnosticCode.InvalidIdlNamespace => InvalidIdlNamespace,
        AvroDiagnosticCode.InvalidIdlAliases => InvalidIdlAliases,
        AvroDiagnosticCode.InvalidIdlLogicalTypeAnnotation => InvalidIdlLogicalTypeAnnotation,
        AvroDiagnosticCode.InvalidIdlOrder => InvalidIdlOrder,
        AvroDiagnosticCode.InvalidIdlEnumDefault => InvalidIdlEnumDefault,
        AvroDiagnosticCode.InvalidIdlFixedSize => InvalidIdlFixedSize,
        AvroDiagnosticCode.InvalidIdlDecimalPrecision => InvalidIdlDecimalPrecision,
        AvroDiagnosticCode.InvalidIdlDecimalScale => InvalidIdlDecimalScale,
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
