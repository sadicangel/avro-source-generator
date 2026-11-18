namespace AvroSourceGenerator.Syntax;

internal interface INameSyntax : ISyntaxNode
{
    string FullName { get; }
}
