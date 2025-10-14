namespace AvroSourceGenerator.Configuration;

internal enum AvroLibrary
{
    None,
    Apache,
    Auto = 2147483647,
}

internal static class AvroLibraryExtensions
{
    extension(AvroLibrary library)
    {
        public string PackageName => library switch
        {
            AvroLibrary.Apache => "Apache.Avro",
            _ => throw new InvalidOperationException($"Invalid {nameof(AvroLibrary)} '{library}'"),
        };
    }
}
