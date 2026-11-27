using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Compiler.Configuration;
using AvroSourceGenerator.Templating;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Cli;

internal static class CompilationRunner
{
    public static async Task<int> RunAsync(
        string[] inputs,
        string destination,
        AvroParseOptions parseOptions,
        AvroCompilationOptions compilationOptions,
        RenderOptions renderOptions,
        bool force,
        bool check,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        string[] paths;
        try
        {
            paths = InputDiscovery.Discover(inputs, cancellationToken);
            destination = Path.GetFullPath(destination);
        }
        catch (ArgumentException exception)
        {
            await error.WriteLineAsync($"avro-sg: {exception.Message}");
            return 2;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await error.WriteLineAsync($"avro-sg: {exception.Message}");
            return 1;
        }

        var sources = new List<SourceText>();
        try
        {
            foreach (var path in paths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                sources.Add(new SourceText(path, await File.ReadAllTextAsync(path, cancellationToken)));
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await error.WriteLineAsync($"avro-sg: {exception.Message}");
            return 1;
        }

        var compilation = AvroCompiler.Compile(sources, parseOptions, compilationOptions, cancellationToken);
        var failed = !compilation.IsValid;
        foreach (var diagnostic in compilation.Diagnostics)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var location = string.Empty;
            var span = diagnostic.SourceSpan;
            if (!span.IsNone)
            {
                var line = span.SourceText.GetLineIndex(span.Offset);
                cancellationToken.ThrowIfCancellationRequested();
                location = $"{span.SourceText.Path.OriginalPath}({line + 1},{span.Offset - span.SourceText.Lines[line].SourceSpan.Offset + 1}): ";
            }
            await error.WriteLineAsync($"{location}{diagnostic.Severity.ToString().ToLowerInvariant()} AVROSG{(int)diagnostic.Code:D4}: {diagnostic.GetMessage()}");
        }

        var rendered = compilation.Files.SelectMany(file =>
            AvroTemplate.Render(RenderableAvroFile.Create(file, compilation, renderOptions, cancellationToken), cancellationToken));
        return await GeneratedFileWriter.WriteAsync(destination, rendered, force, check, failed, emitUtf8Bom: true, output, error, cancellationToken);
    }
}
