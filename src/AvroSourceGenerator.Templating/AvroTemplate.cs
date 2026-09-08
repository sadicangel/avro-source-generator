using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Schemas;
using Scriban.Functions;

namespace AvroSourceGenerator.Templating;

public static class AvroTemplate
{
    public static ImmutableArray<RenderedSchema> Render(RenderableAvroFile file, CancellationToken cancellationToken = default)
    {
        var renderer = TemplateRendererPool.Rent(file.Options);
        var completed = false;
        try
        {
            var renderedSchemas = ImmutableArray.CreateRange(
                file.EmittedSchemas.Select(schema =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var schemaJson = file.Options.GenerationTarget is GenerationTarget.Apache
                        ? GetSchemaJson(schema, file.ProjectSchemas, file.Options)
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
                TemplateRendererPool.Return(file.Options, renderer);
        }
    }

    private static string GetSchemaJson(TopLevelSchema schema, IReadOnlyDictionary<SchemaName, TopLevelSchema> schemasByName, RenderOptions options)
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
