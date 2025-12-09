using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.Emit;

internal static class Emitter
{
    public static void Emit(SourceProductionContext context, RenderResult result)
    {
        foreach (var diagnostic in result.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

        foreach (var schema in result.Schemas)
        {
            context.AddSource(schema.HintName, SourceText.From(schema.SourceText, Encoding.UTF8));
        }
    }
}
