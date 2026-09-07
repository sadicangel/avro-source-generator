namespace AvroSourceGenerator.Compiler;

public readonly record struct AvroImport(AvroImportKind Kind, string Path);
