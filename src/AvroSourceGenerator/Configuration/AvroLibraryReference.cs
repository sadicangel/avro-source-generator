namespace AvroSourceGenerator.Configuration;

internal enum AvroLibraryReference
{
    Apache,
    Chr
}

internal static class AvroLibraryReferenceExtensions
{
    extension(AvroLibraryReference reference)
    {
        public AvroLibrary ToAvroLibrary() => reference switch
        {
            AvroLibraryReference.Apache => AvroLibrary.Apache,
            AvroLibraryReference.Chr => AvroLibrary.Chr,
            _ => throw new ArgumentOutOfRangeException(nameof(reference), reference, null)
        };

        public string PackageName => reference switch
        {
            AvroLibraryReference.Apache => "Apache.Avro",
            AvroLibraryReference.Chr => "Chr.Avro",
            _ => throw new InvalidOperationException($"Invalid {nameof(AvroLibraryReference)} '{reference}'"),
        };
    }
}
