using System.Collections.Immutable;
using System.Text;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Parsing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.Emit;

internal static class Emitter
{
    public static void Emit(SourceProductionContext context, (ImmutableArray<RenderResult> results, RenderSettings settings) source)
    {
        var (results, settings) = source;

        var seenNames = new HashSet<string>();

        foreach (var result in results)
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }

            foreach (var schema in result.Schemas)
            {
                if (seenNames.Add(schema.HintName))
                {
                    context.AddSource(schema.HintName, SourceText.From(schema.SourceText, Encoding.UTF8));
                }
                else if (settings.DuplicateResolution is not DuplicateResolution.Ignore)
                {
                    context.ReportDiagnostic(DuplicateSchemaOutputDiagnostic.Create(LocationInfo.None, schema.HintName));
                }
            }
        }
    }
}
