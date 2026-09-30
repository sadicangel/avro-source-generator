namespace AvroSourceGenerator.Diagnostics;

public enum AvroDiagnosticCode
{
    None = 0,
    UnsupportedSourceType = 1, EmptySource = 2, DuplicateSourcePath = 3, DuplicateSchema = 4, MissingReferences = 5,
    InvalidJson = 1000, EmptyJson = 1001, TrailingJsonContent = 1002,
    SchemaExpected = 2000, ProtocolExpected = 2001, MissingRootSchema = 2002, InvalidSchemaReference = 2003, MissingSchemaProperty = 2004,
    InvalidSchemaName = 2005, InvalidProtocolName = 2006, InvalidFieldName = 2007, InvalidRequestParameterName = 2008,
    InvalidNamespace = 2009, InvalidSchemaType = 2010, InvalidFieldType = 2011, InvalidParameterType = 2012,
    InvalidFields = 2013, InvalidItems = 2014, InvalidValues = 2015, InvalidSymbols = 2016, InvalidAliases = 2017,
    InvalidDoc = 2018, InvalidLogicalType = 2019, InvalidTypes = 2020, InvalidMessages = 2021, InvalidRequest = 2022,
    InvalidResponse = 2023, InvalidErrors = 2024, InvalidOneWay = 2025, InvalidEnumDefault = 2026,
    InvalidProtocolDeclarationType = 2027, InvalidFixedSize = 2029,
    RecursiveSchemaDefinition = 2030, InvalidOneWayMessage = 2031,
    InvalidCharacter = 3000, InvalidEscapeSequence = 3001, InvalidNumber = 3002, UnterminatedDocumentation = 3003, UnterminatedComment = 3004,
    UnterminatedString = 3005, UnterminatedVerbatimIdentifier = 3006, UnexpectedToken = 3007, UnexpectedJsonValue = 3008,
    MisplacedAnnotation = 3009, MisplacedDocumentation = 3010,
    InvalidIdlDocument = 4000, InvalidIdlDeclaration = 4001,
    InvalidIdlNamespace = 4002, InvalidIdlAliases = 4003, InvalidIdlLogicalTypeAnnotation = 4004, InvalidIdlOrder = 4005, InvalidIdlEnumDefault = 4006,
    InvalidIdlFixedSize = 4010, InvalidIdlDecimalPrecision = 4011, InvalidIdlDecimalScale = 4012, InvalidIdlOneWayMessage = 4014,
    ImportCycle = 5000, InvalidImportFileExtension = 5001, MissingImport = 5002, InvalidImportTarget = 5003, UnusedImport = 5004,
    NoAvroLibraryDetected = 6000, MultipleAvroLibrariesDetected = 6001,
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
