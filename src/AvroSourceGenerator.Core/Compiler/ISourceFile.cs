using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler;

public interface ISourceFile
{
    SourceText Text { get; }

    SourcePath Path { get; }

    bool IsValid { get; }
}
