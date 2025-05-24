// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using AvroSourceGenerator.AvroIDL.Diagnostics;
using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Declarations;
using AvroSourceGenerator.AvroIDL.Syntax.Names;
using AvroSourceGenerator.AvroIDL.Syntax.Types;
using AvroSourceGenerator.AvroIDL.Text;
using Spectre.Console;

Console.WriteLine("Hello, World!");

var syntaxTree = SyntaxTree.Parse(new SourceText(File.ReadAllText("IDL/user_management.avdl")));

if (syntaxTree.Diagnostics.Count > 0)
{
    foreach (var diagnostic in syntaxTree.Diagnostics)
    {
        AnsiConsole.Console.Write(diagnostic);
    }
}
else
{
    AnsiConsole.Console.Write(syntaxTree);
}

static class Extensions
{
    public static void Write(this IAnsiConsole console, SyntaxTree syntaxTree)
    {
        var tree = new Tree($"[aqua]{syntaxTree.CompilationUnit.SyntaxKind}[/]")
            .Style("dim white");

        WriteTo(syntaxTree.CompilationUnit, tree);

        console.Write(new Panel(tree).Header(nameof(SyntaxTree)));

        static void WriteTo(SyntaxNode syntaxNode, IHasTreeNodes treeNode)
        {
            foreach (var child in syntaxNode.Children())
            {
                switch (child)
                {
                    case SyntaxToken token:
                        foreach (var trivia in token.LeadingTrivia.Where(x => x.SyntaxKind is SyntaxKind.SingleLineCommentTrivia or SyntaxKind.MultiLineCommentTrivia))
                            treeNode.AddNode($"[aqua]{trivia.SyntaxKind}[/] [grey66 i]{trivia.SourceSpan.Text}[/]");
                        if (token.Value is not null)
                            treeNode.AddNode($"[aqua]{token.SyntaxKind}[/] {FormatLiteral(token)}");
                        else
                            treeNode.AddNode($"[aqua]{token.SyntaxKind}[/] [darkseagreen2 i]{Markup.Escape(token.SourceSpan.ToString())}[/]");
                        foreach (var trivia in token.TrailingTrivia.Where(x => x.SyntaxKind is SyntaxKind.SingleLineCommentTrivia or SyntaxKind.MultiLineCommentTrivia))
                            treeNode.AddNode($"[aqua]{trivia.SyntaxKind}[/] [grey66 i]{trivia.SourceSpan.Text}[/]");
                        break;

                    case NameSyntax name:
                        treeNode.AddNode($"[aqua]{name.SyntaxKind}[/] [darkseagreen2 i]{Markup.Escape(name.FullName)}[/]");
                        break;

                    case TypeSyntax type:
                        treeNode.AddNode($"[darkseagreen2 i]{Markup.Escape(type.ToString())}[/]");
                        break;

                    case JsonValueSyntax jsonValue:
                        treeNode.AddNode($"[aqua]{jsonValue.SyntaxKind}[/] [darkseagreen2 i]{Markup.Escape(jsonValue.Json?.ToJsonString() ?? "null")}[/]");
                        break;

                    default:
                        WriteTo(child, treeNode.AddNode($"[aqua]{child.SyntaxKind}[/]"));
                        break;
                }
            }
        }

        static string FormatLiteral(SyntaxToken token)
        {
            return token.SyntaxKind switch
            {
                SyntaxKind.IntegerLiteralToken or SyntaxKind.FloatLiteralToken => $"[gold3]{token.Value}[/]",
                SyntaxKind.StringLiteralToken => $"[darkorange3]\"{Markup.Escape(token.Value?.ToString() ?? "")}\"[/]",
                SyntaxKind.TrueKeyword or
                SyntaxKind.FalseKeyword or
                SyntaxKind.NullKeyword => $"[blue3_1]{token.Value}[/]",
                _ => throw new UnreachableException($"Unexpected {nameof(SyntaxKind)} '{token.SyntaxKind}'"),
            };
        }
    }

    public static void Write(this IAnsiConsole console, Diagnostic diagnostic)
    {
        var fileName = diagnostic.SourceSpan.FileName;
        var startLine = diagnostic.SourceSpan.StartLine + 1;
        var startCharacter = diagnostic.SourceSpan.StartCharacter + 1;
        var endLine = diagnostic.SourceSpan.EndLine + 1;
        var endCharacter = diagnostic.SourceSpan.EndCharacter + 1;

        var span = diagnostic.SourceSpan;
        var lineIndex = diagnostic.SourceSpan.SourceText.GetLineIndex(span.Offset);
        var line = diagnostic.SourceSpan.SourceText.Lines[lineIndex];

        var colour = diagnostic.Severity switch
        {
            DiagnosticSeverity.Error => "red",
            DiagnosticSeverity.Warning => "gold3",
            DiagnosticSeverity.Information => "darkslategray3",
            _ => throw new UnreachableException($"Unexpected {nameof(DiagnosticSeverity)} '{diagnostic.Severity}'"),
        };

        var prefixSpan = new Range(line.SourceSpan.Offset, span.Offset);
        var suffixSpan = new Range(span.Offset + span.Length, line.SourceSpan.Offset + line.SourceSpan.Length);

        var prefix = diagnostic.SourceSpan.SourceText.Text.AsSpan(prefixSpan);
        var highlight = span.Text;
        var suffix = diagnostic.SourceSpan.SourceText.Text.AsSpan(suffixSpan);

        var underline = string.Empty;
        if (startLine == endLine)
        {
            underline = string.Create(diagnostic.SourceSpan.StartCharacter + highlight.Length, diagnostic.SourceSpan.StartCharacter, static (span, start) =>
            {
                span[..start].Fill(' ');
                span[start..].Fill('^');
            });
        }

        console.MarkupInterpolated($"""
            [{colour}]{Markup.Escape(fileName)}({startLine},{startCharacter},{endLine},{endCharacter}): {Markup.Escape(diagnostic.Message)}[/]
                {prefix.ToString()}[{colour}]{highlight.ToString()}[/]{suffix.ToString()}
                {underline}
            """);
    }
}
