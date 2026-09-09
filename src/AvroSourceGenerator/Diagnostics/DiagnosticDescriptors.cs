using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Diagnostics;

internal static class DiagnosticDescriptors
{
    private const string CompilerCategory = "Compiler";
    private const string ConfigurationCategory = "Configuration";
    private const string InvalidSyntaxId = "AVROSG1000";
    private const string InvalidSyntaxTitle = "Invalid Avro IDL";
    private const string InvalidSyntaxDescription = "The Avro IDL source contains syntax or source-level validation errors. Fix the IDL definition.";

    internal static readonly DiagnosticDescriptor InvalidJson = new(
        id: "AVROSG0001",
        title: "Invalid JSON",
        messageFormat: MessageTemplate.InvalidJson,
        category: CompilerCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The JSON supplied for an Avro schema could not be parsed. " +
        "Fix the JSON syntax (quotes, commas, braces, etc.). If the error reports a path, check that location first.");

    internal static readonly DiagnosticDescriptor InvalidSchema = new(
        id: "AVROSG0002",
        title: "Invalid Schema",
        messageFormat: MessageTemplate.InvalidSchema,
        category: CompilerCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The JSON parsed, but the Avro schema is not valid according to the Avro specification " +
        "(e.g., duplicate field names, invalid union members, unresolved references, etc.).");

    internal static readonly DiagnosticDescriptor NoAvroLibraryDetected = new(
        id: "AVROSG0003",
        title: "No Avro library detected (Auto)",
        messageFormat: MessageTemplate.NoAvroLibraryDetected,
        category: ConfigurationCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "No supported Avro libraries were detected in the project while AvroLibrary=Auto. " +
        "The generator will disable library-specific code (AvroLibrary=None). " +
        "Install a supported library to enable generation, or set <AvroSourceGeneratorAvroLibrary>None</AvroSourceGeneratorAvroLibrary> " +
        "explicitly to silence this warning.");

    internal static readonly DiagnosticDescriptor MultipleAvroLibrariesDetected = new(
        id: "AVROSG0004",
        title: "Multiple Avro libraries detected",
        messageFormat: MessageTemplate.MultipleAvroLibrariesDetected,
        category: ConfigurationCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The generator found more than one supported Avro library (e.g., Apache.Avro and Chr.Avro). " +
        "To avoid ambiguous generation, it will disable library-specific code (AvroLibrary=None). " +
        "Choose one library via the <AvroLibrary> property or uninstall the extras.");

    internal static readonly DiagnosticDescriptor DuplicateSchema = new(
        id: "AVROSG0005",
        title: "Duplicate Avro schema detected",
        messageFormat: MessageTemplate.DuplicateSchema,
        category: CompilerCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Two or more Avro schema input files map to the same generated type. " +
        "This creates an ambiguous generation result. " +
        "By default, AvroSourceGenerator fails fast with an error. " +
        "If you intentionally want to allow duplicates and accept a nondeterministic " +
        "selection of the generated output, set the MSBuild property " +
        "<AvroSourceGeneratorDuplicateResolution>Ignore</AvroSourceGeneratorDuplicateResolution>.");

    internal static readonly DiagnosticDescriptor MissingReferences = new(
        id: "AVROSG0006",
        title: "Missing schema reference",
        messageFormat: MessageTemplate.MissingReferences,
        category: CompilerCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "One or more Avro schema references could not be resolved from the available schema inputs.");

    internal static readonly DiagnosticDescriptor InvalidCharacter = new(
        InvalidSyntaxId,
        InvalidSyntaxTitle,
        MessageTemplate.InvalidCharacter,
        CompilerCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        InvalidSyntaxDescription);

    internal static readonly DiagnosticDescriptor InvalidEscapeSequence = new(
        InvalidSyntaxId,
        InvalidSyntaxTitle,
        MessageTemplate.InvalidEscapeSequence,
        CompilerCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        InvalidSyntaxDescription);

    internal static readonly DiagnosticDescriptor InvalidNumber = new(
        InvalidSyntaxId,
        InvalidSyntaxTitle,
        MessageTemplate.InvalidNumber,
        CompilerCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        InvalidSyntaxDescription);

    internal static readonly DiagnosticDescriptor UnterminatedDocumentation = new(
        InvalidSyntaxId,
        InvalidSyntaxTitle,
        MessageTemplate.UnterminatedDocumentation,
        CompilerCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        InvalidSyntaxDescription);

    internal static readonly DiagnosticDescriptor UnterminatedComment = new(
        InvalidSyntaxId,
        InvalidSyntaxTitle,
        MessageTemplate.UnterminatedComment,
        CompilerCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        InvalidSyntaxDescription);

    internal static readonly DiagnosticDescriptor UnterminatedString = new(
        InvalidSyntaxId,
        InvalidSyntaxTitle,
        MessageTemplate.UnterminatedString,
        CompilerCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        InvalidSyntaxDescription);

    internal static readonly DiagnosticDescriptor UnterminatedVerbatimIdentifier = new(
        InvalidSyntaxId,
        InvalidSyntaxTitle,
        MessageTemplate.UnterminatedVerbatimIdentifier,
        CompilerCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        InvalidSyntaxDescription);

#pragma warning disable RS1032 // Preserve the existing diagnostic wording.
    internal static readonly DiagnosticDescriptor UnexpectedToken = new(
        InvalidSyntaxId,
        InvalidSyntaxTitle,
        MessageTemplate.UnexpectedToken,
        CompilerCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        InvalidSyntaxDescription);
#pragma warning restore RS1032

    internal static readonly DiagnosticDescriptor UnexpectedJsonValue = new(
        InvalidSyntaxId,
        InvalidSyntaxTitle,
        MessageTemplate.UnexpectedJsonValue,
        CompilerCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        InvalidSyntaxDescription);

    internal static readonly DiagnosticDescriptor MisplacedAnnotation = new(
        InvalidSyntaxId,
        InvalidSyntaxTitle,
        MessageTemplate.MisplacedAnnotation,
        CompilerCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        InvalidSyntaxDescription);

    internal static readonly DiagnosticDescriptor MisplacedDocumentation = new(
        InvalidSyntaxId,
        InvalidSyntaxTitle,
        MessageTemplate.MisplacedDocumentation,
        CompilerCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        InvalidSyntaxDescription);

    internal static readonly DiagnosticDescriptor InvalidSource = new(
        InvalidSyntaxId,
        InvalidSyntaxTitle,
        MessageTemplate.InvalidSource,
        CompilerCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        InvalidSyntaxDescription);

    internal static readonly DiagnosticDescriptor InvalidImport = new(
        id: "AVROSG1001",
        title: "Invalid Avro import",
        messageFormat: MessageTemplate.InvalidImport,
        category: CompilerCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An Avro IDL import path could not be resolved to a compatible AdditionalFile.");

    internal static readonly DiagnosticDescriptor UnknownError = new(
        id: "AVROSG9999",
        title: "Unknown error in Avro Source Generator",
        messageFormat: MessageTemplate.UnknownError,
        category: CompilerCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An unexpected error occurred during the execution of the Avro Source Generator. Please report this issue with details about how to reproduce it.");

    internal static DiagnosticDescriptor Get(AvroDiagnosticCode code) => code switch
    {
        AvroDiagnosticCode.InvalidJson => InvalidJson,
        AvroDiagnosticCode.InvalidSchema => InvalidSchema,
        AvroDiagnosticCode.NoAvroLibraryDetected => NoAvroLibraryDetected,
        AvroDiagnosticCode.MultipleAvroLibrariesDetected => MultipleAvroLibrariesDetected,
        AvroDiagnosticCode.DuplicateSchema => DuplicateSchema,
        AvroDiagnosticCode.MissingReferences => MissingReferences,
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
        AvroDiagnosticCode.InvalidSource => InvalidSource,
        AvroDiagnosticCode.InvalidImport => InvalidImport,
        AvroDiagnosticCode.UnknownError => UnknownError,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
    };
}
