using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Compiler.Configuration;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Serialization;
using Scriban.Functions;

namespace AvroSourceGenerator.Templating;

public static class AvroTemplate
{
    public static ImmutableArray<RenderedSchema> Render(RenderableAvroFile file, CancellationToken cancellationToken = default)
    {
        var renderer = TemplateRendererPool.Rent(file.Options);
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

            return renderedSchemas;
        }
        finally
        {
            TemplateRendererPool.Return(file.Options, renderer);
        }
    }

    private static string GetSchemaJson(TopLevelSchema schema, FrozenDictionary<SchemaName, TopLevelSchema> schemaIndex, RenderOptions options)
    {
        if (options.LanguageFeatures.HasRawStringLiterals)
        {
            return $""""
                """
                {schema.ToAvjs(schemaIndex, new JsonWriterOptions { Indented = true })}
                """
                """";
        }

        return StringFunctions.Literal(schema.ToAvjs(schemaIndex))
            ?? throw new InvalidOperationException("Unreachable code");
    }
}
