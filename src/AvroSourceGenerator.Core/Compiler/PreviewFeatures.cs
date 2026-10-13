namespace AvroSourceGenerator.Compiler;

[Flags]
public enum PreviewFeatures
{
    None = 0,
    Unions = 1 << 0,
}
