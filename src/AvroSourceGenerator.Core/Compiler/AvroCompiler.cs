using System.Collections.Immutable;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler;

public static class AvroCompiler
{
    public static AvroCompilation Compile(
        IEnumerable<SourceText> sources,
        AvroParseOptions parseOptions,
        AvroCompilationOptions compilationOptions = default,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var files = ImmutableArray.CreateBuilder<AvroFile>();
        foreach (var source in sources)
            files.Add(AvroFile.Parse(source, parseOptions, cancellationToken));
        var parsed = files.DrainToImmutable();
        var symbols = SymbolTable.FromFiles(parsed, cancellationToken);
        var bound = ImmutableArray.CreateBuilder<BoundAvroFile>(parsed.Length);
        foreach (var file in parsed)
            bound.Add(BoundAvroFile.Bind(LinkedAvroFile.Link(file, symbols, cancellationToken), cancellationToken));
        return AvroCompilation.Create(bound.MoveToImmutable(), compilationOptions, cancellationToken);
    }
}
