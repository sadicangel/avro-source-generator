namespace AvroSourceGenerator.Avdl;

internal interface INameSyntax : ISyntaxNode
{
    string FullName { get; }
}
