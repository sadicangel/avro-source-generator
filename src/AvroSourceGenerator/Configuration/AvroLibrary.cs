namespace AvroSourceGenerator.Configuration;

internal enum AvroLibrary
{
    None,
    Apache,
    Chr,
    Auto = 2147483647,
}

internal enum AvroLibraryReference
{
    Apache,
    Chr
}

internal static class AvroLibraryReferenceExtensions
{
    private static readonly string s_supportedPackageList =
        string.Join(", ", Enum.GetValues(typeof(AvroLibraryReference)).OfType<AvroLibraryReference>().Select(x => $"'{x.PackageName}'"));

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

        public static string SupportedPackageList => s_supportedPackageList;
    }
}
