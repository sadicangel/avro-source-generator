using AvroSourceGenerator.Avdl.Text;

namespace AvroSourceGenerator.Exceptions;

public sealed class InvalidSourceException(string message, SourceSpan sourceSpan) : Exception(message)
{
    public SourceSpan SourceSpan { get; } = sourceSpan;
}
