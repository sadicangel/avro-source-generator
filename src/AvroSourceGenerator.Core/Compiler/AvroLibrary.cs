namespace AvroSourceGenerator.Compiler;

public enum AvroLibrary
{
    None,
    Apache,
    // Chr.Avro support was removed in 0.8.0. Retained only for migration diagnostics.
    Chr,
    Auto = 2147483647,
}
