namespace AvroSourceGenerator.Syntax.Json;

internal interface IJsonSyntaxNode : ISyntaxNode
{
    JsonSyntaxKind JsonSyntaxKind { get; }
}
