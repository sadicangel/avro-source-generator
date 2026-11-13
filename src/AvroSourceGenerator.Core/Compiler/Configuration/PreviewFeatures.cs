namespace AvroSourceGenerator.Compiler.Configuration;

[Flags]
public enum PreviewFeatures
{
    None = 0,
    Unions = 1 << 0,
}
