namespace AvroSourceGenerator.Diagnostics;

public enum AvroDiagnosticCode
{
    None = 0,

    UnsupportedSourceType = 1,
    EmptySource = 2,
    DuplicateSourcePath = 3,

    EmptyJson = 1000,
    TrailingJsonContent = 1001,
    UnexpectedJsonValue = 1002,
    InvalidCharacter = 1003,
    InvalidEscapeSequence = 1004,
    InvalidNumber = 1005,
    UnterminatedString = 1006,
    UnexpectedToken = 1007,
    JsonCommentNotAllowed = 1008,
    JsonPropertyNameUnpairedSurrogate = 1009,
    JsonNestingLimitExceeded = 1010,

    UnterminatedDocumentation = 2000,
    UnterminatedComment = 2001,
    UnterminatedVerbatimIdentifier = 2002,
    MisplacedAnnotation = 2003,
    MisplacedDocumentation = 2004,

    SchemaExpected = 3000,
    ProtocolExpected = 3001,
    MissingRootSchema = 3002,
    InvalidIdlDocument = 3003,
    InvalidIdlDeclaration = 3004,
    DuplicateIdlDirective = 3005,

    MissingSchemaProperty = 4000,
    InvalidSchemaName = 4001,
    InvalidProtocolName = 4002,
    InvalidFieldName = 4003,
    InvalidRequestParameterName = 4004,
    InvalidNamespace = 4005,
    InvalidAliases = 4006,
    InvalidDoc = 4007,
    InvalidLogicalType = 4008,
    InvalidDecimalPrecision = 4009,
    InvalidDecimalScale = 4010,
    InvalidSchemaType = 4011,
    InvalidFieldType = 4012,
    InvalidParameterType = 4013,
    InvalidProtocolDeclarationType = 4014,
    InvalidFields = 4015,
    InvalidOrder = 4016,
    InvalidItems = 4017,
    InvalidValues = 4018,
    InvalidSymbols = 4019,
    InvalidEnumDefault = 4020,
    InvalidFixedSize = 4021,
    InvalidTypes = 4022,
    InvalidMessages = 4023,
    InvalidRequest = 4024,
    InvalidResponse = 4025,
    InvalidErrors = 4026,
    InvalidOneWay = 4027,

    InvalidSchemaReference = 5000,
    DuplicateSchema = 5001,
    MissingReferences = 5002,
    RecursiveSchemaDefinition = 5003,
    DuplicateField = 5004,
    DuplicateEnumSymbol = 5005,
    DuplicateUnionBranch = 5006,
    NestedUnion = 5007,
    InvalidOneWayMessage = 5008,

    ImportCycle = 6000,
    InvalidImportFileExtension = 6001,
    MissingImport = 6002,
    InvalidImportTarget = 6003,
    UnusedImport = 6004,

    NoAvroLibraryDetected = 7000,
    MultipleAvroLibrariesDetected = 7001,
    ChrAvroSupportRemoved = 7002,
}

public static class AvroDiagnosticCodeExtensions
{
    extension(AvroDiagnosticCode code)
    {
        public AvroDiagnosticSeverity Severity => code switch
        {
            AvroDiagnosticCode.None => AvroDiagnosticSeverity.Hidden,
            AvroDiagnosticCode.NoAvroLibraryDetected or AvroDiagnosticCode.MultipleAvroLibrariesDetected or AvroDiagnosticCode.UnusedImport => AvroDiagnosticSeverity.Warning,
            _ => AvroDiagnosticSeverity.Error,
        };
        public string MessageTemplate => MessageTemplate.Get(code);
    }
}
