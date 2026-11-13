using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler;

internal sealed class AvscParser(SourceText sourceText, AvroParseOptions options, CancellationToken cancellationToken) : AvjsParser(sourceText, options, cancellationToken)
{
    public static AvroFile ParseFile(SourceText sourceText, AvroParseOptions options, CancellationToken cancellationToken)
    {
        var parser = new AvscParser(sourceText, options, cancellationToken);
        if (parser.ParseSchema() is not AvroSchema schema)
            return AvroFile.Invalid(sourceText, [.. parser.Diagnostics], options);

        return new AvroFile(
            sourceText,
            schema,
            [.. parser.Declarations],
            [.. parser.DeclarationSpans],
            parser.GetReferences(),
            parser.GetReferenceSpans(),
            parser.GetDependencies(),
            [],
            [.. parser.Diagnostics],
            options);
    }
}
