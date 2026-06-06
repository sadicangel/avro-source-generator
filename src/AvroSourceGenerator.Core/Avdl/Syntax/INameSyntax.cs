namespace AvroSourceGenerator.Avdl.Syntax;

public interface INameSyntax : ISyntaxNode
{
    string FullName { get; }
}
