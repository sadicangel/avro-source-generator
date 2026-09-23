using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler;

public sealed class AvprParser(SourceText sourceText, AvroParseOptions options, CancellationToken cancellationToken) : AvjsParser(sourceText, options, cancellationToken)
{
    public static AvroFile ParseFile(SourceText sourceText, AvroParseOptions options, CancellationToken cancellationToken)
    {
        var parser = new AvprParser(sourceText, options, cancellationToken);
        var protocol = parser.ParseProtocol();

        if (protocol is null)
            return AvroFile.Invalid(sourceText, [.. parser.Diagnostics], options);

        return new AvroFile(
            sourceText,
            protocol,
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
