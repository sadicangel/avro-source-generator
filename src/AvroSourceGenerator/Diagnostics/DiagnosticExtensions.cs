using AvroSourceGenerator.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.Diagnostics;

internal static class DiagnosticExtensions
{
    public static Diagnostic ToDiagnostic(this AvroDiagnostic diagnostic) =>
        Diagnostic.Create(diagnostic.Code.ToDiagnosticDescriptor(), diagnostic.SourceSpan.ToLocation(), diagnostic.Arguments.ToArray());

    public static Location ToLocation(this SourceSpan span)
    {
        if (span.IsNone || span.SourceText.Path.IsEmpty) return Location.None;
        var text = span.SourceText;
        var startLine = text.GetLineIndex(span.Offset);
        var endLine = text.GetLineIndex(span.Offset + span.Length);
        var start = new LinePosition(startLine, span.Offset - text.Lines[startLine].SourceSpan.Offset);
        var end = new LinePosition(endLine, span.Offset + span.Length - text.Lines[endLine].SourceSpan.Offset);
        return Location.Create(text.Path, new TextSpan(span.Offset, span.Length), new LinePositionSpan(start, end));
    }
}
