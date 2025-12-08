namespace AvroSourceGenerator.Configuration;

internal enum AvroLibrary
{
    None,
    Apache,
    Auto = 2147483647,
}

internal enum AvroLibraryReference
{
    Apache,
}

internal static class AvroLibraryExtensions
{
    extension(AvroLibraryReference reference)
    {
        public AvroLibrary ToAvroLibrary() => reference switch
        {
            AvroLibraryReference.Apache => AvroLibrary.Apache,
            _ => throw new ArgumentOutOfRangeException(nameof(reference), reference, null)
        };

        public string PackageName => reference switch
        {
            AvroLibraryReference.Apache => "Apache.Avro",
            _ => throw new InvalidOperationException($"Invalid {nameof(AvroLibraryReference)} '{reference}'"),
        };
    }
}
