using System.Collections.Immutable;
using System.Reflection;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace AvroSourceGenerator.Templating;

internal sealed class TemplateLoader(TemplateSettings settings) : ITemplateLoader
{
    private static readonly ImmutableDictionary<string, string> s_templatePaths = BuildTemplatePaths();
    private static readonly ImmutableArray<string> s_dynamicTemplateNames = ["apache.put", "fixed"];

    private readonly ImmutableDictionary<string, string> _templatePaths = s_templatePaths
        .SetItems(s_dynamicTemplateNames.ToDictionary(name => name, name => GetDynamicTemplatePath(name, settings)));

    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) =>
        _templatePaths[templateName];

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        var assembly = Assembly.GetExecutingAssembly().GetManifestResourceStream(templatePath)
            ?? throw new InvalidOperationException($"Template resource '{templatePath}' was not found.");

        using var reader = new StreamReader(assembly);
        return reader.ReadToEnd();
    }

    private static ImmutableDictionary<string, string> BuildTemplatePaths()
    {
        const string TemplateNamespace = "AvroSourceGenerator.Templating.Templates";
        const string TemplateExtension = ".sbncs";
        return Assembly.GetExecutingAssembly().GetManifestResourceNames()
            .Where(name => name.StartsWith(TemplateNamespace) && name.EndsWith(TemplateExtension))
            .ToImmutableDictionary(GetTemplateName);

        static string GetTemplateName(string templatePath) =>
           templatePath.AsSpan()[(TemplateNamespace.Length + 1)..^TemplateExtension.Length].ToString();
    }

    private static string GetDynamicTemplatePath(string templateName, TemplateSettings settings) => templateName switch
    {
        "apache.put" when !settings.UseInitOnlyProperties => s_templatePaths["apache.put_mutable"],
        "apache.put" when !settings.UseUnsafeAccessors => s_templatePaths["apache.put_immutable_reflection"],
        "apache.put" => s_templatePaths["apache.put_immutable_unsafe"],
        "fixed" => s_templatePaths["apache.fixed"],
        _ => throw new InvalidOperationException($"Template '{templateName}' is not supported."),
    };
}
