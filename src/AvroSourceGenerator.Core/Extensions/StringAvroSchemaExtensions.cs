using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Extensions;

internal static class StringAvroSchemaExtensions
{
    extension(string name)
    {
        public SchemaName ToSchemaName(string? containingNamespace = null)
        {
            _ = name.TrySplitQualifiedName(out name, out var @namespace);

            if (string.IsNullOrWhiteSpace(name) || @namespace is "")
                throw new InvalidSchemaException("Argument has an invalid name format: 'cannot start or end with a dot'");

            return new SchemaName(name, @namespace ?? containingNamespace);
        }
    }

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
