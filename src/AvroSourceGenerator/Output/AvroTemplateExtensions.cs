using System.Collections.Immutable;
using AvroSourceGenerator.Templating;

namespace AvroSourceGenerator.Output;

internal static class AvroTemplateExtensions
{
    extension(AvroTemplate)
    {
        public static ImmutableArray<RenderedSchema> Render(RenderableAvroFile input, CancellationToken cancellationToken) =>
            AvroTemplate.Render(input.EmittedSchemas, input.ProjectSchemas, input.Options, cancellationToken);
    }
}
