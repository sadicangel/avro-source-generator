using System.Text;
using AvroSourceGenerator.Templating;

namespace AvroSourceGenerator.Cli;

internal static class GeneratedFileWriter
{
    public static async Task<int> WriteAsync(
        string destination,
        IEnumerable<RenderedSchema> schemas,
        bool force,
        bool check,
        bool failed,
        bool emitUtf8Bom,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        // Finish rendering and check for collisions before writing any files.
        var rendered = new Dictionary<string, RenderedSchema>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (var schema in schemas)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!rendered.TryAdd(schema.HintName, schema))
            {
                await error.WriteLineAsync($"avro-sg: Generated filenames '{rendered[schema.HintName].HintName}' and '{schema.HintName}' resolve to the same output path.");
                return 1;
            }
        }
        foreach (var (hintName, schema) in rendered)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = Path.Combine(destination, hintName);
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
                await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: emitUtf8Bom));
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
