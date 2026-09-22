using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Avsc;

internal static class AvscSchemaParser
{
    public static AvroFile Parse(SourceText source, AvroParseOptions options, CancellationToken cancellationToken) =>
        Syntax.AvscParser.Parse(source, options, cancellationToken);
}
