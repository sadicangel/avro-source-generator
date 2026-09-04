using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Schemas;
using Scriban.Functions;

namespace AvroSourceGenerator.Templating;

public static class AvroTemplate
{
    internal static ImmutableArray<RenderedSchema> Render(
        ImmutableArray<TopLevelSchema> schemas,
        ImmutableDictionary<SchemaName, TopLevelSchema> schemasByName,
        RenderOptions options,
        CancellationToken cancellationToken)
    {
        var renderer = TemplateRendererPool.Rent(options);
        var completed = false;
        try
        {
            var renderedSchemas = ImmutableArray.CreateRange(schemas.Select(schema =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var schemaJson = options.TargetProfile is TargetProfile.Apache
                    ? GetSchemaJson(schema, schemasByName, options)
                    : null;
                var hintName = $"{schema.SchemaName.FullName}.Avro.g.cs";
                var sourceText = renderer.Render(schema, schemaJson);
                return new RenderedSchema(hintName, sourceText);
            }));

            completed = true;
            return renderedSchemas;
        }
        finally
        {
            if (completed)
                TemplateRendererPool.Return(options, renderer);
        }
    }

    private static string GetSchemaJson(TopLevelSchema schema, ImmutableDictionary<SchemaName, TopLevelSchema> schemasByName, RenderOptions options)
    {
        if (options.UseRawStringLiterals)
        {
            return $""""
                """
                {schema.ToJsonString(schemasByName, new JsonWriterOptions { Indented = true })}
                """
                """";
        }

        return StringFunctions.Literal(schema.ToJsonString(schemasByName))
            ?? throw new InvalidOperationException("Unreachable code");
    }
}
