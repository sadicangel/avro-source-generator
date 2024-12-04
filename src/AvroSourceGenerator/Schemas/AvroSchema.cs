using System.Collections.Immutable;

namespace AvroSourceGenerator.Schemas;

// Represents an Avro named schema, which is the only
// kind we're interested in generating code for.
internal abstract record class AvroSchema(
    SchemaType Type,
    QualifiedName QualifiedName,
    string? Documentation,
    ImmutableArray<string> Aliases)
{
    public string Name => QualifiedName.Name;
    public string? Namespace => QualifiedName.Namespace;
    public bool IsNullable => Name[^1] is '?';
}
