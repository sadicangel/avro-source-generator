using System.Collections.Immutable;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Templating;

namespace AvroSourceGenerator.Extensions;

internal static class AvroCompilationExtensions
{
    extension(AvroCompilation)
    {
        public static AvroCompilation FromInput((ImmutableArray<BoundAvroFile> Files, AvroCompilationOptions Options) input, CancellationToken cancellationToken) =>
            AvroCompilation.Create(input.Files, input.Options, cancellationToken);
    }

    extension(RenderableAvroFile)
    {
        public static RenderableAvroFile FromInput(((BoundAvroFile File, AvroCompilation Compilation) Input, RenderOptions Options) input, CancellationToken cancellationToken) =>
            RenderableAvroFile.Create(input.Input.File, input.Input.Compilation, input.Options, cancellationToken);
    }
}
