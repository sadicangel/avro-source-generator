namespace AvroSourceGenerator.Parsing;

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
