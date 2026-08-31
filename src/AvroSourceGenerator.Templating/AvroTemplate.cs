using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Registry;
using AvroSourceGenerator.Schemas;
using Scriban.Functions;

namespace AvroSourceGenerator.Templating;

public static class AvroTemplate
{
    public static ImmutableArray<RenderedSchema> Render(in SchemaRegistry schemaRegistry, TemplateSettings settings)
    {
        var registeredSchemas = schemaRegistry.ToImmutableDictionary(x => x.SchemaName);
        var schemas = schemaRegistry.Where(ShouldEmitCode).ToImmutableArray();
        return Render(schemas, registeredSchemas, settings);
    }

    internal static ImmutableArray<RenderedSchema> Render(
        ImmutableArray<TopLevelSchema> schemas,
        ImmutableDictionary<SchemaName, TopLevelSchema> registeredSchemas,
        TemplateSettings settings)
    {
        var renderer = TemplateRendererPool.Rent(settings);
        var completed = false;
        try
        {
            var renderedSchemas = ImmutableArray.CreateRange(schemas.Select(schema =>
            {
                var schemaJson = settings.TargetProfile is TargetProfile.Apache
                    ? GetSchemaJson(schema, registeredSchemas, settings)
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
                TemplateRendererPool.Return(settings, renderer);
        }
    }

    private static bool ShouldEmitCode(TopLevelSchema schema) =>
        schema is not FixedSchema fixedSchema || fixedSchema.CSharpName != AvroSchema.Bytes.CSharpName;

    private static string GetSchemaJson(TopLevelSchema schema, ImmutableDictionary<SchemaName, TopLevelSchema> registeredSchemas, TemplateSettings settings)
    {
        if (settings.UseRawStringLiterals)
        {
            return $""""
                """
                {schema.ToJsonString(registeredSchemas, new JsonWriterOptions { Indented = true })}
                """
                """";
        }

        return StringFunctions.Literal(schema.ToJsonString(registeredSchemas))
            ?? throw new InvalidOperationException("Unreachable code");
    }
}
