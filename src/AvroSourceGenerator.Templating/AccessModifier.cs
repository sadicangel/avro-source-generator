namespace AvroSourceGenerator.Templating;

public enum AccessModifier
{
    Public,
    Internal,
}

internal static class AccessModifierExtensions
{
    extension(AccessModifier accessModifier)
    {
        public string Keyword =>
            accessModifier switch
            {
                AccessModifier.Public => "public",
                AccessModifier.Internal => "internal",
                _ => throw new ArgumentOutOfRangeException(nameof(accessModifier), accessModifier, null)
            };
    }
}
