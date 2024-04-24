
namespace AvroNet;

internal sealed record class AvroModelOptions
{
    public AvroModelOptions(string name, string schema, string @namespace, string accessModifier, string declarationType)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Schema = schema ?? throw new ArgumentNullException(nameof(name));
        Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
        AccessModifier = accessModifier ?? throw new ArgumentNullException(nameof(accessModifier));
        DeclarationType = declarationType ?? throw new ArgumentNullException(nameof(declarationType));
    }

    public string Name { get; }
    public string Schema { get; }
    public string Namespace { get; }
    public string AccessModifier { get; }
    public string DeclarationType { get; }
}
