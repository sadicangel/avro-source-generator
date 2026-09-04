using System.Collections.Concurrent;
using AvroSourceGenerator.Configuration;
using Scriban;
using Scriban.Syntax;

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

internal sealed class TemplateRenderer
{
    private static readonly ScriptVariableGlobal s_schema = new("Schema");
    private static readonly ScriptVariableGlobal s_schemaJson = new("SchemaJson");
    private readonly TemplateContext _context;
    private readonly Template _template;

    public TemplateRenderer(RenderOptions options)
    {
        var templateLoader = new TemplateLoader(options);
        _context = new TemplateContext(new TemplateScriptObject(options))
        {
            MemberRenamer = member => member.Name,
            TemplateLoader = templateLoader,
        };

        var templatePath = _context.GetTemplatePathFromName("schema", callerContext: null)
            ?? throw new InvalidOperationException("Unreachable code");
        _template = _context.GetOrCreateTemplate(templatePath, callerContext: null);
    }

    public string Render(object schema, string? schemaJson)
    {
        _context.SetValue(s_schema, schema);
        _context.SetValue(s_schemaJson, schemaJson);
        return _template.Render(_context);
    }

    public void ClearSchemaValues()
    {
        // Context.Reset clears Scriban's parsed-template cache. Retain that cache, but remove the values which
        // otherwise retain the current project's schema graph and generated Apache JSON between generator runs.
        _context.SetValue(s_schema, null);
        _context.SetValue(s_schemaJson, null);
    }
}
