namespace AvroSourceGenerator.Extensions;

internal static class StringAvroSchemaExtensions
{
    extension(string qualifiedName)
    {
        public bool TrySplitQualifiedName(out string name, out string? @namespace)
        {
            var indexOfLast = qualifiedName.LastIndexOf('.');
            if (indexOfLast < 0)
            {
                name = qualifiedName;
                @namespace = null;
                return false;
            }

            name = qualifiedName[(indexOfLast + 1)..];
            @namespace = qualifiedName[..indexOfLast];

            return true;
        }
    }
}
