using AvroSourceGenerator.Avdl.Syntax;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Avdl;

internal static class AvdlSchemaParser
{
    public static AvroFile Parse(SourceText source, AvroParseOptions options, CancellationToken cancellationToken) =>
        AvdlParser.ParseFile(source, options, cancellationToken);
}
