namespace AvroSourceGenerator.Diagnostics;

public enum AvroDiagnosticCode
{
    None = 0,
    UnsupportedSourceType = 1, EmptySource = 2, DuplicateSourcePath = 3, DuplicateSchema = 4, MissingReferences = 5,
    InvalidJson = 1000, EmptyJson = 1001, TrailingJsonContent = 1002,
    SchemaExpected = 2000, ProtocolExpected = 2001,
    MissingRootSchema = 2002, InvalidSchemaValue = 2003, ObjectExpected = 2004, MissingSchemaProperty = 2005, InvalidStringProperty = 2006,
    InvalidArrayProperty = 2007, InvalidObjectProperty = 2008, InvalidBooleanProperty = 2009, InvalidStringArrayElement = 2010,
    InvalidFixedSize = 2011, InvalidAvroName = 2012, UnknownSchemaType = 2013, RecursiveSchemaDefinition = 2014, InvalidOneWayMessage = 2015,
    InvalidCharacter = 3000, InvalidEscapeSequence = 3001, InvalidNumber = 3002, UnterminatedDocumentation = 3003, UnterminatedComment = 3004,
    UnterminatedString = 3005, UnterminatedVerbatimIdentifier = 3006, UnexpectedToken = 3007, UnexpectedJsonValue = 3008,
    MisplacedAnnotation = 3009, MisplacedDocumentation = 3010,
    InvalidIdlDocument = 4000, InvalidIdlDeclaration = 4001, InvalidIdlType = 4002, InvalidIdlSchemaDeclaration = 4003, InvalidIdlPrimitive = 4004,
    InvalidIdlFixedSize = 4005, InvalidIdlDecimalPrecision = 4006, InvalidIdlDecimalScale = 4007, InvalidIdlLogicalType = 4008, InvalidIdlOneWayMessage = 4009,
    ImportCycle = 5000, InvalidImportFileExtension = 5001, MissingImport = 5002, InvalidImportTarget = 5003,
    NoAvroLibraryDetected = 6000, MultipleAvroLibrariesDetected = 6001,
}

public static class AvroDiagnosticCodeExtensions
{
    extension(AvroDiagnosticCode code)
    {
        public AvroDiagnosticSeverity Severity => code switch
        {
            AvroDiagnosticCode.None => AvroDiagnosticSeverity.Hidden,
            AvroDiagnosticCode.NoAvroLibraryDetected or AvroDiagnosticCode.MultipleAvroLibrariesDetected => AvroDiagnosticSeverity.Warning,
            _ => AvroDiagnosticSeverity.Error,
        };
        public string MessageTemplate => MessageTemplate.Get(code);
    }
}
