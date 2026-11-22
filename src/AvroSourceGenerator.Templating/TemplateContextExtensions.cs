using System.Runtime.CompilerServices;
using AvroSourceGenerator.Schemas;
using Scriban;

namespace AvroSourceGenerator.Templating;

internal static class TemplateContextExtensions
{
    extension(TemplateContext context)
    {
        public TemplateContext WriteCode(string? text) => context.Write(text);

#pragma warning disable IDE0060 // Remove unused parameter
        public TemplateContext WriteCode([InterpolatedStringHandlerArgument(nameof(context))] WriteCodeInterpolatedStringHandler handler) => context;
#pragma warning restore IDE0060 // Remove unused parameter

        public TemplateContext WriteIndent(int level)
        {
            const string Indent = "    ";
            for (var i = 0; i < level; i++)
                context.WriteCode(Indent);
            return context;
        }

    }

    [InterpolatedStringHandler]
#pragma warning disable CS9113 // Parameter is unread.
    internal readonly ref struct WriteCodeInterpolatedStringHandler(int literalLength, int formattedCount, TemplateContext context)
#pragma warning restore CS9113 // Parameter is unread.
    {
        public void AppendLiteral(string value) => context.WriteCode(value);

        public void AppendFormatted(string? value) => context.WriteCode(value);

        public void AppendFormatted(AvroSchema value) => context.WriteCode(value.CSharpName.FullName);
    }
}
