using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler;

public sealed class AvscParser(SourceText sourceText, AvroParseOptions options, CancellationToken cancellationToken) : AvjsParser(sourceText, options, cancellationToken)
{
    public static AvroFile ParseFile(SourceText sourceText, AvroParseOptions options, CancellationToken cancellationToken)
    {
        var parser = new AvscParser(sourceText, options, cancellationToken);
        var schema = parser.ParseSchema();

        if (schema is null)
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
