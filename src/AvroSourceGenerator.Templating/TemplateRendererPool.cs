using System.Collections.Concurrent;

namespace AvroSourceGenerator.Templating;

internal static class TemplateRendererPool
{
    private static readonly ConcurrentDictionary<RenderOptions, ConcurrentBag<TemplateRenderer>> s_renderers = [];

    public static TemplateRenderer Rent(RenderOptions options)
    {
        var renderers = s_renderers.GetOrAdd(options, static _ => []);
        return renderers.TryTake(out var renderer) ? renderer : new TemplateRenderer(options);
    }

    public static void Return(RenderOptions options, TemplateRenderer renderer)
    {
        renderer.ClearSchemaValues();
        var renderers = s_renderers.GetOrAdd(options, static _ => []);
        renderers.Add(renderer);
    }
}
