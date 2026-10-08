namespace AvroSourceGenerator.Configuration;

[Flags]
internal enum PreviewFeatures
{
    None = 0,
    Unions = 1 << 0,
}
