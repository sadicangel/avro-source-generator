namespace AvroSourceGenerator.Compiler;

public readonly record struct AvroCompilationOptions(
    ReferenceResolution ReferenceResolution = ReferenceResolution.Strict,
    DuplicateResolution DuplicateResolution = DuplicateResolution.Error);
