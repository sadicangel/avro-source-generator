using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;

internal static class DiagnosticDescriptors
{
    private const string CompilerCategory = "Compiler";
    private const string ConfigurationCategory = "Configuration";

    internal static readonly DiagnosticDescriptor UnsupportedSourceType = new DiagnosticDescriptor("AVROSG0001", "Unsupported Source Type", MessageTemplate.Get(AvroDiagnosticCode.UnsupportedSourceType), CompilerCategory, DiagnosticSeverity.Error, true, "The input file extension is not supported. Use .avsc for schemas, .avpr for protocols, or .avdl for IDL.");
    internal static readonly DiagnosticDescriptor EmptySource = new DiagnosticDescriptor("AVROSG0002", "Empty Source", MessageTemplate.Get(AvroDiagnosticCode.EmptySource), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro input contains no non-whitespace content. Add a schema, protocol, or IDL declaration.");
    internal static readonly DiagnosticDescriptor DuplicateSourcePath = new DiagnosticDescriptor("AVROSG0003", "Duplicate Source Path", MessageTemplate.Get(AvroDiagnosticCode.DuplicateSourcePath), CompilerCategory, DiagnosticSeverity.Error, true, "Two AdditionalFiles resolve to the same canonical source path. Give every Avro input a distinct path.");

    internal static readonly DiagnosticDescriptor EmptyJson = new DiagnosticDescriptor("AVROSG1000", "Empty JSON", MessageTemplate.Get(AvroDiagnosticCode.EmptyJson), CompilerCategory, DiagnosticSeverity.Error, true, "A JSON value was expected before the end of the input. Provide a complete Avro schema or protocol JSON value.");
    internal static readonly DiagnosticDescriptor TrailingJsonContent = new DiagnosticDescriptor("AVROSG1001", "Trailing JSON Content", MessageTemplate.Get(AvroDiagnosticCode.TrailingJsonContent), CompilerCategory, DiagnosticSeverity.Error, true, "The file contains content after its root JSON value. Remove the trailing content.");
    internal static readonly DiagnosticDescriptor UnexpectedJsonValue = new DiagnosticDescriptor("AVROSG1002", "Unexpected JSON Value", MessageTemplate.Get(AvroDiagnosticCode.UnexpectedJsonValue), CompilerCategory, DiagnosticSeverity.Error, true, "Supply a JSON value at the reported location.");
    internal static readonly DiagnosticDescriptor InvalidCharacter = new DiagnosticDescriptor("AVROSG1003", "Invalid Character", MessageTemplate.Get(AvroDiagnosticCode.InvalidCharacter), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro source contains a character that is invalid in the current lexical context.");
    internal static readonly DiagnosticDescriptor InvalidEscapeSequence = new DiagnosticDescriptor("AVROSG1004", "Invalid Escape Sequence", MessageTemplate.Get(AvroDiagnosticCode.InvalidEscapeSequence), CompilerCategory, DiagnosticSeverity.Error, true, "A string or identifier contains an unsupported or incomplete escape sequence.");
    internal static readonly DiagnosticDescriptor InvalidNumber = new DiagnosticDescriptor("AVROSG1005", "Invalid Number", MessageTemplate.Get(AvroDiagnosticCode.InvalidNumber), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro source contains a malformed numeric literal.");
    internal static readonly DiagnosticDescriptor UnterminatedString = new DiagnosticDescriptor("AVROSG1006", "Unterminated String", MessageTemplate.Get(AvroDiagnosticCode.UnterminatedString), CompilerCategory, DiagnosticSeverity.Error, true, "A string literal reaches the end of a line or file before its closing quotation mark.");
    internal static readonly DiagnosticDescriptor UnexpectedToken = new DiagnosticDescriptor("AVROSG1007", "Unexpected Token", MessageTemplate.Get(AvroDiagnosticCode.UnexpectedToken), CompilerCategory, DiagnosticSeverity.Error, true, "The parser expected a different token at the reported location.");
    internal static readonly DiagnosticDescriptor JsonCommentNotAllowed = new DiagnosticDescriptor("AVROSG1008", "JSON Comment Not Allowed", MessageTemplate.Get(AvroDiagnosticCode.JsonCommentNotAllowed), CompilerCategory, DiagnosticSeverity.Error, true, "Remove the highlighted comment from the JSON schema or protocol.");
    internal static readonly DiagnosticDescriptor JsonPropertyNameUnpairedSurrogate = new DiagnosticDescriptor("AVROSG1009", "Unpaired Surrogate in JSON Property Name", MessageTemplate.Get(AvroDiagnosticCode.JsonPropertyNameUnpairedSurrogate), CompilerCategory, DiagnosticSeverity.Error, true, "Replace the unpaired UTF-16 surrogate in the JSON property name with valid Unicode text.");
    internal static readonly DiagnosticDescriptor JsonNestingLimitExceeded = new DiagnosticDescriptor("AVROSG1010", "JSON Nesting Limit Exceeded", MessageTemplate.Get(AvroDiagnosticCode.JsonNestingLimitExceeded), CompilerCategory, DiagnosticSeverity.Error, true, "Reduce the number of nested JSON arrays and objects.");

    internal static readonly DiagnosticDescriptor UnterminatedDocumentation = new DiagnosticDescriptor("AVROSG2000", "Unterminated Documentation", MessageTemplate.Get(AvroDiagnosticCode.UnterminatedDocumentation), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL documentation comment reaches the end of the file before its closing delimiter.");
    internal static readonly DiagnosticDescriptor UnterminatedComment = new DiagnosticDescriptor("AVROSG2001", "Unterminated Comment", MessageTemplate.Get(AvroDiagnosticCode.UnterminatedComment), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL block comment reaches the end of the file before its closing delimiter.");
    internal static readonly DiagnosticDescriptor UnterminatedVerbatimIdentifier = new DiagnosticDescriptor("AVROSG2002", "Unterminated Verbatim Identifier", MessageTemplate.Get(AvroDiagnosticCode.UnterminatedVerbatimIdentifier), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL verbatim identifier reaches the end of the input before its closing delimiter.");
    internal static readonly DiagnosticDescriptor MisplacedAnnotation = new DiagnosticDescriptor("AVROSG2003", "Misplaced Annotation", MessageTemplate.Get(AvroDiagnosticCode.MisplacedAnnotation), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL annotation is applied to a declaration or type that does not support it.");
    internal static readonly DiagnosticDescriptor MisplacedDocumentation = new DiagnosticDescriptor("AVROSG2004", "Misplaced Documentation", MessageTemplate.Get(AvroDiagnosticCode.MisplacedDocumentation), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL documentation comment precedes a construct that cannot receive documentation.");

    internal static readonly DiagnosticDescriptor SchemaExpected = new DiagnosticDescriptor("AVROSG3000", "Schema Expected", MessageTemplate.Get(AvroDiagnosticCode.SchemaExpected), CompilerCategory, DiagnosticSeverity.Error, true, "Provide a type name, schema object with a type property, or union array.");
    internal static readonly DiagnosticDescriptor ProtocolExpected = new DiagnosticDescriptor("AVROSG3001", "Protocol Expected", MessageTemplate.Get(AvroDiagnosticCode.ProtocolExpected), CompilerCategory, DiagnosticSeverity.Error, true, "Provide a protocol object with a protocol property.");
    internal static readonly DiagnosticDescriptor MissingRootSchema = new DiagnosticDescriptor("AVROSG3002", "Missing Root Schema", MessageTemplate.Get(AvroDiagnosticCode.MissingRootSchema), CompilerCategory, DiagnosticSeverity.Error, true, "The input contains no named schema or protocol that can serve as a root declaration.");
    internal static readonly DiagnosticDescriptor InvalidIdlDocument = new DiagnosticDescriptor("AVROSG3003", "Invalid IDL Document", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlDocument), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro IDL document has an invalid top-level structure. Provide a main schema directive or a single protocol declaration.");
    internal static readonly DiagnosticDescriptor InvalidIdlDeclaration = new DiagnosticDescriptor("AVROSG3004", "Invalid IDL Declaration", MessageTemplate.Get(AvroDiagnosticCode.InvalidIdlDeclaration), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL declaration is not valid in its current scope. Move, replace, or remove the declaration.");
    internal static readonly DiagnosticDescriptor DuplicateIdlDirective = new DiagnosticDescriptor("AVROSG3005", "Duplicate IDL Directive", MessageTemplate.Get(AvroDiagnosticCode.DuplicateIdlDirective), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL document contains a repeated schema or namespace directive. Keep at most one directive of each kind, even when their values are identical.");

    internal static readonly DiagnosticDescriptor MissingSchemaProperty = new DiagnosticDescriptor("AVROSG4000", "Missing Schema Property", MessageTemplate.Get(AvroDiagnosticCode.MissingSchemaProperty), CompilerCategory, DiagnosticSeverity.Error, true, "A required property is missing from a schema or protocol object. Add the property named by the diagnostic.");
    internal static readonly DiagnosticDescriptor InvalidSchemaName = new DiagnosticDescriptor("AVROSG4001", "Invalid Schema Name", MessageTemplate.Get(AvroDiagnosticCode.InvalidSchemaName), CompilerCategory, DiagnosticSeverity.Error, true, "Property 'name' must be a non-empty string containing a valid Avro name, optionally prefixed with its namespace.");
    internal static readonly DiagnosticDescriptor InvalidProtocolName = new DiagnosticDescriptor("AVROSG4002", "Invalid Protocol Name", MessageTemplate.Get(AvroDiagnosticCode.InvalidProtocolName), CompilerCategory, DiagnosticSeverity.Error, true, "Property 'protocol' must be a non-empty string containing a valid Avro name, optionally prefixed with its namespace.");
    internal static readonly DiagnosticDescriptor InvalidFieldName = new DiagnosticDescriptor("AVROSG4003", "Invalid Field Name", MessageTemplate.Get(AvroDiagnosticCode.InvalidFieldName), CompilerCategory, DiagnosticSeverity.Error, true, "A field name must be a non-empty Avro name.");
    internal static readonly DiagnosticDescriptor InvalidRequestParameterName = new DiagnosticDescriptor("AVROSG4004", "Invalid Request Parameter Name", MessageTemplate.Get(AvroDiagnosticCode.InvalidRequestParameterName), CompilerCategory, DiagnosticSeverity.Error, true, "A request parameter name must be a non-empty Avro name.");
    internal static readonly DiagnosticDescriptor InvalidNamespace = new DiagnosticDescriptor("AVROSG4005", "Invalid Namespace", MessageTemplate.Get(AvroDiagnosticCode.InvalidNamespace), CompilerCategory, DiagnosticSeverity.Error, true, "A namespace must be null or a string containing valid dot-separated Avro names.");
    internal static readonly DiagnosticDescriptor InvalidAliases = new DiagnosticDescriptor("AVROSG4006", "Invalid Aliases", MessageTemplate.Get(AvroDiagnosticCode.InvalidAliases), CompilerCategory, DiagnosticSeverity.Error, true, "Property 'aliases' must be an array of non-empty strings, or null.");
    internal static readonly DiagnosticDescriptor InvalidDoc = new DiagnosticDescriptor("AVROSG4007", "Invalid Doc", MessageTemplate.Get(AvroDiagnosticCode.InvalidDoc), CompilerCategory, DiagnosticSeverity.Error, true, "A doc property must be a string or null.");
    internal static readonly DiagnosticDescriptor InvalidLogicalType = new DiagnosticDescriptor("AVROSG4008", "Invalid Logical Type", MessageTemplate.Get(AvroDiagnosticCode.InvalidLogicalType), CompilerCategory, DiagnosticSeverity.Error, true, "A logicalType property must be a non-empty string.");
    internal static readonly DiagnosticDescriptor InvalidDecimalPrecision = new DiagnosticDescriptor("AVROSG4009", "Invalid Decimal Precision", MessageTemplate.Get(AvroDiagnosticCode.InvalidDecimalPrecision), CompilerCategory, DiagnosticSeverity.Error, true, "Decimal precision must be a 32-bit integer.");
    internal static readonly DiagnosticDescriptor InvalidDecimalScale = new DiagnosticDescriptor("AVROSG4010", "Invalid Decimal Scale", MessageTemplate.Get(AvroDiagnosticCode.InvalidDecimalScale), CompilerCategory, DiagnosticSeverity.Error, true, "Decimal scale must be a 32-bit integer.");
    internal static readonly DiagnosticDescriptor InvalidSchemaType = new DiagnosticDescriptor("AVROSG4011", "Invalid Schema Type", MessageTemplate.Get(AvroDiagnosticCode.InvalidSchemaType), CompilerCategory, DiagnosticSeverity.Error, true, "Property 'type' must be a non-empty string naming an Avro type or referencing a named type.");
    internal static readonly DiagnosticDescriptor InvalidFieldType = new DiagnosticDescriptor("AVROSG4012", "Invalid Field Type", MessageTemplate.Get(AvroDiagnosticCode.InvalidFieldType), CompilerCategory, DiagnosticSeverity.Error, true, "Field property 'type' must be a primitive type name, a named type reference, an inline type definition, or a union.");
    internal static readonly DiagnosticDescriptor InvalidParameterType = new DiagnosticDescriptor("AVROSG4013", "Invalid Parameter Type", MessageTemplate.Get(AvroDiagnosticCode.InvalidParameterType), CompilerCategory, DiagnosticSeverity.Error, true, "Request parameter property 'type' must be a primitive type name, a named type reference, an inline type definition, or a union.");
    internal static readonly DiagnosticDescriptor InvalidProtocolDeclarationType = new DiagnosticDescriptor("AVROSG4014", "Invalid Protocol Declaration Type", MessageTemplate.Get(AvroDiagnosticCode.InvalidProtocolDeclarationType), CompilerCategory, DiagnosticSeverity.Error, true, "A type declaration inside a protocol must be a record, error, enum, or fixed schema.");
    internal static readonly DiagnosticDescriptor InvalidFields = new DiagnosticDescriptor("AVROSG4015", "Invalid Fields", MessageTemplate.Get(AvroDiagnosticCode.InvalidFields), CompilerCategory, DiagnosticSeverity.Error, true, "A fields property must be an array of field objects.");
    internal static readonly DiagnosticDescriptor InvalidOrder = new DiagnosticDescriptor("AVROSG4016", "Invalid Field Order", MessageTemplate.Get(AvroDiagnosticCode.InvalidOrder), CompilerCategory, DiagnosticSeverity.Error, true, "A field order property requires a non-empty string value, or null.");
    internal static readonly DiagnosticDescriptor InvalidItems = new DiagnosticDescriptor("AVROSG4017", "Invalid Items", MessageTemplate.Get(AvroDiagnosticCode.InvalidItems), CompilerCategory, DiagnosticSeverity.Error, true, "Property 'items' must be a primitive type name, a named type reference, an inline type definition, or a union.");
    internal static readonly DiagnosticDescriptor InvalidValues = new DiagnosticDescriptor("AVROSG4018", "Invalid Values", MessageTemplate.Get(AvroDiagnosticCode.InvalidValues), CompilerCategory, DiagnosticSeverity.Error, true, "Property 'values' must be a primitive type name, a named type reference, an inline type definition, or a union.");
    internal static readonly DiagnosticDescriptor InvalidSymbols = new DiagnosticDescriptor("AVROSG4019", "Invalid Symbols", MessageTemplate.Get(AvroDiagnosticCode.InvalidSymbols), CompilerCategory, DiagnosticSeverity.Error, true, "A symbols property must be an array of non-empty Avro names.");
    internal static readonly DiagnosticDescriptor InvalidEnumDefault = new DiagnosticDescriptor("AVROSG4020", "Invalid Enum Default", MessageTemplate.Get(AvroDiagnosticCode.InvalidEnumDefault), CompilerCategory, DiagnosticSeverity.Error, true, "An enum default property must be a string or null.");
    internal static readonly DiagnosticDescriptor InvalidFixedSize = new DiagnosticDescriptor("AVROSG4021", "Invalid Fixed Size", MessageTemplate.Get(AvroDiagnosticCode.InvalidFixedSize), CompilerCategory, DiagnosticSeverity.Error, true, "A fixed schema size must be a positive 32-bit integer.");
    internal static readonly DiagnosticDescriptor InvalidTypes = new DiagnosticDescriptor("AVROSG4022", "Invalid Types", MessageTemplate.Get(AvroDiagnosticCode.InvalidTypes), CompilerCategory, DiagnosticSeverity.Error, true, "Property 'types' must be an array of record, error, enum, or fixed definitions.");
    internal static readonly DiagnosticDescriptor InvalidMessages = new DiagnosticDescriptor("AVROSG4023", "Invalid Messages", MessageTemplate.Get(AvroDiagnosticCode.InvalidMessages), CompilerCategory, DiagnosticSeverity.Error, true, "A messages property must be an object whose values are message objects.");
    internal static readonly DiagnosticDescriptor InvalidRequest = new DiagnosticDescriptor("AVROSG4024", "Invalid Request", MessageTemplate.Get(AvroDiagnosticCode.InvalidRequest), CompilerCategory, DiagnosticSeverity.Error, true, "A request property must be an array of parameter objects.");
    internal static readonly DiagnosticDescriptor InvalidResponse = new DiagnosticDescriptor("AVROSG4025", "Invalid Response", MessageTemplate.Get(AvroDiagnosticCode.InvalidResponse), CompilerCategory, DiagnosticSeverity.Error, true, "Property 'response' must be a primitive type name, a named type reference, an inline type definition, or a union.");
    internal static readonly DiagnosticDescriptor InvalidErrors = new DiagnosticDescriptor("AVROSG4026", "Invalid Errors", MessageTemplate.Get(AvroDiagnosticCode.InvalidErrors), CompilerCategory, DiagnosticSeverity.Error, true, "Property 'errors' must be null or an array of primitive type names, named type references, inline type definitions, or unions.");
    internal static readonly DiagnosticDescriptor InvalidOneWay = new DiagnosticDescriptor("AVROSG4027", "Invalid One Way", MessageTemplate.Get(AvroDiagnosticCode.InvalidOneWay), CompilerCategory, DiagnosticSeverity.Error, true, "Property 'one-way' must be a boolean or null.");

    internal static readonly DiagnosticDescriptor InvalidSchemaReference = new DiagnosticDescriptor("AVROSG5000", "Invalid Schema Reference", MessageTemplate.Get(AvroDiagnosticCode.InvalidSchemaReference), CompilerCategory, DiagnosticSeverity.Error, true, "A type reference must contain a valid Avro name, optionally prefixed with its namespace.");
    internal static readonly DiagnosticDescriptor DuplicateSchema = new DiagnosticDescriptor("AVROSG5001", "Duplicate Schema", MessageTemplate.Get(AvroDiagnosticCode.DuplicateSchema), CompilerCategory, DiagnosticSeverity.Error, true, "Multiple declarations define the same fully qualified schema name. Rename or remove a declaration; only use AvroSourceGeneratorDuplicateResolution=Ignore when an arbitrary winner is acceptable.");
    internal static readonly DiagnosticDescriptor MissingReferences = new DiagnosticDescriptor("AVROSG5002", "Missing References", MessageTemplate.Get(AvroDiagnosticCode.MissingReferences), CompilerCategory, DiagnosticSeverity.Error, true, "A named schema reference is not visible to the source file. Supply or import the referenced schema, or correct its fully qualified name.");
    internal static readonly DiagnosticDescriptor RecursiveSchemaDefinition = new DiagnosticDescriptor("AVROSG5003", "Recursive Schema Definition", MessageTemplate.Get(AvroDiagnosticCode.RecursiveSchemaDefinition), CompilerCategory, DiagnosticSeverity.Error, true, "A schema is nested within its own unfinished definition. Use a named reference to the enclosing schema instead.");
    internal static readonly DiagnosticDescriptor DuplicateField = new DiagnosticDescriptor("AVROSG5004", "Duplicate Field", MessageTemplate.Get(AvroDiagnosticCode.DuplicateField), CompilerCategory, DiagnosticSeverity.Error, true, "Record and error field names must be unique within their declaration, using case-sensitive Avro names.");
    internal static readonly DiagnosticDescriptor DuplicateEnumSymbol = new DiagnosticDescriptor("AVROSG5005", "Duplicate Enum Symbol", MessageTemplate.Get(AvroDiagnosticCode.DuplicateEnumSymbol), CompilerCategory, DiagnosticSeverity.Error, true, "An enum cannot repeat a symbol. Remove or rename the repeated symbol.");
    internal static readonly DiagnosticDescriptor DuplicateUnionBranch = new DiagnosticDescriptor("AVROSG5006", "Duplicate Union Branch", MessageTemplate.Get(AvroDiagnosticCode.DuplicateUnionBranch), CompilerCategory, DiagnosticSeverity.Error, true, "Union branches must have distinct Avro types, or distinct fullnames for record, error, enum, and fixed types. Logical annotations do not change branch identity.");
    internal static readonly DiagnosticDescriptor NestedUnion = new DiagnosticDescriptor("AVROSG5007", "Nested Union", MessageTemplate.Get(AvroDiagnosticCode.NestedUnion), CompilerCategory, DiagnosticSeverity.Error, true, "A union cannot immediately contain another union. Correct the offending branch in the source schema.");
    internal static readonly DiagnosticDescriptor InvalidOneWayMessage = new DiagnosticDescriptor("AVROSG5008", "Invalid One Way Message", MessageTemplate.Get(AvroDiagnosticCode.InvalidOneWayMessage), CompilerCategory, DiagnosticSeverity.Error, true, "A one-way protocol message has a non-null response or declares errors. It must return null and declare no errors.");

    internal static readonly DiagnosticDescriptor ImportCycle = new DiagnosticDescriptor("AVROSG6000", "Import Cycle", MessageTemplate.Get(AvroDiagnosticCode.ImportCycle), CompilerCategory, DiagnosticSeverity.Error, true, "The Avro IDL import graph contains a cycle. Remove or redirect an import so it is acyclic.");
    internal static readonly DiagnosticDescriptor InvalidImportFileExtension = new DiagnosticDescriptor("AVROSG6001", "Invalid Import File Extension", MessageTemplate.Get(AvroDiagnosticCode.InvalidImportFileExtension), CompilerCategory, DiagnosticSeverity.Error, true, "An import kind targets a file with an incompatible extension. Use .avdl, .avpr, or .avsc for the corresponding import kind.");
    internal static readonly DiagnosticDescriptor MissingImport = new DiagnosticDescriptor("AVROSG6002", "Missing Import", MessageTemplate.Get(AvroDiagnosticCode.MissingImport), CompilerCategory, DiagnosticSeverity.Error, true, "An Avro IDL import path does not match a supplied AdditionalFile relative to the importing file.");
    internal static readonly DiagnosticDescriptor InvalidImportTarget = new DiagnosticDescriptor("AVROSG6003", "Invalid Import Target", MessageTemplate.Get(AvroDiagnosticCode.InvalidImportTarget), CompilerCategory, DiagnosticSeverity.Error, true, "An imported file exists but does not contain the Avro definition kind required by the import directive.");
    internal static readonly DiagnosticDescriptor UnusedImport = new DiagnosticDescriptor("AVROSG6004", "Unused Import", MessageTemplate.Get(AvroDiagnosticCode.UnusedImport), CompilerCategory, DiagnosticSeverity.Warning, true, "No schema reference requires this import. Remove it or reference a declaration from the imported file.");

    internal static readonly DiagnosticDescriptor NoAvroLibraryDetected = new DiagnosticDescriptor("AVROSG7000", "No Avro Library Detected", MessageTemplate.Get(AvroDiagnosticCode.NoAvroLibraryDetected), ConfigurationCategory, DiagnosticSeverity.Warning, true, "AvroLibrary is Auto, but no supported runtime library was detected. Install one, select one explicitly, or configure None for library-neutral generation.");
    internal static readonly DiagnosticDescriptor MultipleAvroLibrariesDetected = new DiagnosticDescriptor("AVROSG7001", "Multiple Avro Libraries Detected", MessageTemplate.Get(AvroDiagnosticCode.MultipleAvroLibrariesDetected), ConfigurationCategory, DiagnosticSeverity.Warning, true, "AvroLibrary is Auto, but multiple supported runtime libraries were detected. Select the intended library explicitly or remove extra package references.");
    internal static readonly DiagnosticDescriptor ChrAvroSupportRemoved = new DiagnosticDescriptor("AVROSG7002", "Chr.Avro Support Removed", MessageTemplate.Get(AvroDiagnosticCode.ChrAvroSupportRemoved), ConfigurationCategory, DiagnosticSeverity.Error, true, "Chr.Avro is no longer supported. Explicitly select Apache or None, or continue using AvroSourceGenerator 0.7.x. Chr.Avro selection falls back to library-neutral models.");

    internal static DiagnosticDescriptor ToDiagnosticDescriptor(this AvroDiagnosticCode code) => code switch
    {
        AvroDiagnosticCode.UnsupportedSourceType => UnsupportedSourceType,
        AvroDiagnosticCode.EmptySource => EmptySource,
        AvroDiagnosticCode.DuplicateSourcePath => DuplicateSourcePath,

        AvroDiagnosticCode.EmptyJson => EmptyJson,
        AvroDiagnosticCode.TrailingJsonContent => TrailingJsonContent,
        AvroDiagnosticCode.UnexpectedJsonValue => UnexpectedJsonValue,
        AvroDiagnosticCode.InvalidCharacter => InvalidCharacter,
        AvroDiagnosticCode.InvalidEscapeSequence => InvalidEscapeSequence,
        AvroDiagnosticCode.InvalidNumber => InvalidNumber,
        AvroDiagnosticCode.UnterminatedString => UnterminatedString,
        AvroDiagnosticCode.UnexpectedToken => UnexpectedToken,
        AvroDiagnosticCode.JsonCommentNotAllowed => JsonCommentNotAllowed,
        AvroDiagnosticCode.JsonPropertyNameUnpairedSurrogate => JsonPropertyNameUnpairedSurrogate,
        AvroDiagnosticCode.JsonNestingLimitExceeded => JsonNestingLimitExceeded,

        AvroDiagnosticCode.UnterminatedDocumentation => UnterminatedDocumentation,
        AvroDiagnosticCode.UnterminatedComment => UnterminatedComment,
        AvroDiagnosticCode.UnterminatedVerbatimIdentifier => UnterminatedVerbatimIdentifier,
        AvroDiagnosticCode.MisplacedAnnotation => MisplacedAnnotation,
        AvroDiagnosticCode.MisplacedDocumentation => MisplacedDocumentation,

        AvroDiagnosticCode.SchemaExpected => SchemaExpected,
        AvroDiagnosticCode.ProtocolExpected => ProtocolExpected,
        AvroDiagnosticCode.MissingRootSchema => MissingRootSchema,
        AvroDiagnosticCode.InvalidIdlDocument => InvalidIdlDocument,
        AvroDiagnosticCode.InvalidIdlDeclaration => InvalidIdlDeclaration,
        AvroDiagnosticCode.DuplicateIdlDirective => DuplicateIdlDirective,

        AvroDiagnosticCode.MissingSchemaProperty => MissingSchemaProperty,
        AvroDiagnosticCode.InvalidSchemaName => InvalidSchemaName,
        AvroDiagnosticCode.InvalidProtocolName => InvalidProtocolName,
        AvroDiagnosticCode.InvalidFieldName => InvalidFieldName,
        AvroDiagnosticCode.InvalidRequestParameterName => InvalidRequestParameterName,
        AvroDiagnosticCode.InvalidNamespace => InvalidNamespace,
        AvroDiagnosticCode.InvalidAliases => InvalidAliases,
        AvroDiagnosticCode.InvalidDoc => InvalidDoc,
        AvroDiagnosticCode.InvalidLogicalType => InvalidLogicalType,
        AvroDiagnosticCode.InvalidDecimalPrecision => InvalidDecimalPrecision,
        AvroDiagnosticCode.InvalidDecimalScale => InvalidDecimalScale,
        AvroDiagnosticCode.InvalidSchemaType => InvalidSchemaType,
        AvroDiagnosticCode.InvalidFieldType => InvalidFieldType,
        AvroDiagnosticCode.InvalidParameterType => InvalidParameterType,
        AvroDiagnosticCode.InvalidProtocolDeclarationType => InvalidProtocolDeclarationType,
        AvroDiagnosticCode.InvalidFields => InvalidFields,
        AvroDiagnosticCode.InvalidOrder => InvalidOrder,
        AvroDiagnosticCode.InvalidItems => InvalidItems,
        AvroDiagnosticCode.InvalidValues => InvalidValues,
        AvroDiagnosticCode.InvalidSymbols => InvalidSymbols,
        AvroDiagnosticCode.InvalidEnumDefault => InvalidEnumDefault,
        AvroDiagnosticCode.InvalidFixedSize => InvalidFixedSize,
        AvroDiagnosticCode.InvalidTypes => InvalidTypes,
        AvroDiagnosticCode.InvalidMessages => InvalidMessages,
        AvroDiagnosticCode.InvalidRequest => InvalidRequest,
        AvroDiagnosticCode.InvalidResponse => InvalidResponse,
        AvroDiagnosticCode.InvalidErrors => InvalidErrors,
        AvroDiagnosticCode.InvalidOneWay => InvalidOneWay,

        AvroDiagnosticCode.InvalidSchemaReference => InvalidSchemaReference,
        AvroDiagnosticCode.DuplicateSchema => DuplicateSchema,
        AvroDiagnosticCode.MissingReferences => MissingReferences,
        AvroDiagnosticCode.RecursiveSchemaDefinition => RecursiveSchemaDefinition,
        AvroDiagnosticCode.DuplicateField => DuplicateField,
        AvroDiagnosticCode.DuplicateEnumSymbol => DuplicateEnumSymbol,
        AvroDiagnosticCode.DuplicateUnionBranch => DuplicateUnionBranch,
        AvroDiagnosticCode.NestedUnion => NestedUnion,
        AvroDiagnosticCode.InvalidOneWayMessage => InvalidOneWayMessage,

        AvroDiagnosticCode.ImportCycle => ImportCycle,
        AvroDiagnosticCode.InvalidImportFileExtension => InvalidImportFileExtension,
        AvroDiagnosticCode.MissingImport => MissingImport,
        AvroDiagnosticCode.InvalidImportTarget => InvalidImportTarget,
        AvroDiagnosticCode.UnusedImport => UnusedImport,

        AvroDiagnosticCode.NoAvroLibraryDetected => NoAvroLibraryDetected,
        AvroDiagnosticCode.MultipleAvroLibrariesDetected => MultipleAvroLibrariesDetected,
        AvroDiagnosticCode.ChrAvroSupportRemoved => ChrAvroSupportRemoved,

        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
    };
}
