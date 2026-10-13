using System.Text;
using AvroSourceGenerator.Compiler;
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
                var line = 0;
                for (var index = 1; index < span.SourceText.Lines.Length && span.SourceText.Lines[index].SourceSpan.Offset <= span.Offset; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    line = index;
                }
                location = $"{span.SourceText.Path.OriginalPath}({line + 1},{span.Offset - span.SourceText.Lines[line].SourceSpan.Offset + 1}): ";
            }
            await error.WriteLineAsync($"{location}{diagnostic.Severity.ToString().ToLowerInvariant()} AVROSG{(int)diagnostic.Code:D4}: {diagnostic.GetMessage()}");
        }

        // Render before performing filesystem mutations; internal rendering failures surface normally.
        var rendered = new List<RenderedSchema>();
        foreach (var file in compilation.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            rendered.AddRange(AvroTemplate.Render(RenderableAvroFile.Create(file, compilation, renderOptions, cancellationToken), cancellationToken));
        }
        foreach (var schema in rendered)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = Path.Combine(destination, schema.HintName);
            try
            {
                if (check)
                {
                    if (!File.Exists(path) ||
                        (await File.ReadAllTextAsync(path, cancellationToken)).ReplaceLineEndings("\n") != schema.SourceText.ReplaceLineEndings("\n"))
                    {
                        await error.WriteLineAsync($"avro-sg: Missing or changed output '{path}'.");
                        failed = true;
                    }
                    continue;
                }
                if (force && File.Exists(path) && await File.ReadAllTextAsync(path, cancellationToken) == schema.SourceText)
                    continue;
                Directory.CreateDirectory(destination);
                await using var stream = new FileStream(path, force ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true);
                await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                await writer.WriteAsync(schema.SourceText.AsMemory(), cancellationToken);
                await writer.FlushAsync(cancellationToken);
                await output.WriteLineAsync($"Generated {path}");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                await error.WriteLineAsync($"avro-sg: Cannot write or check '{path}': {exception.Message}");
                failed = true;
            }
        }
        return failed ? 1 : 0;
    }
}
