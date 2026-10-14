using System.CommandLine;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Templating;

namespace AvroSourceGenerator.Cli;

internal static class CliApplication
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error, CancellationToken cancellationToken = default)
    {
        var inputs = new Argument<string[]>("inputs")
        {
            Description = "Avro files, recursive directories, or quoted glob patterns, in declaration precedence order.",
            Arity = ArgumentArity.OneOrMore,
        };
        var destination = new Option<string>("--output", "-o") { Description = "Destination directory.", Required = true };
        var library = EnumOption("--library", AvroLibrary.None, "Runtime library. Chr support was removed in 0.8.0. Auto requires a consuming compilation.",
            value => value switch
            {
                AvroLibrary.Chr => MessageTemplate.ChrAvroSupportRemoved,
                AvroLibrary.Auto => "--library Auto requires a consuming compilation. Select None or Apache explicitly.",
                _ => null,
            });
        var language = new Option<string>("--language-version")
        {
            Description = "C# syntax ceiling: 7.3, 8 through 15, or preview.",
            DefaultValueFactory = _ => "14",
        };
        language.AcceptOnlyFromAmong("7.3", "8", "9", "10", "11", "12", "13", "14", "15", "preview");
        var features = EnumOption("--language-features", LanguageFeatures.All, "Language feature preset or comma-separated flags, capped by the C# version.");
        var preview = EnumOption("--preview-features", PreviewFeatures.None, "Preview feature opt-ins.");
        var access = EnumOption("--access-modifier", AccessModifier.Public, "Generated type visibility.");
        var declaration = new Option<string>("--record-declaration")
        {
            Description = "Generate record or class declarations when supported.",
            DefaultValueFactory = _ => "record",
        };
        declaration.AcceptOnlyFromAmong("record", "class");
        var references = EnumOption("--reference-resolution", ReferenceResolution.Strict, "Named reference resolution.");
        var duplicates = EnumOption("--duplicate-resolution", DuplicateResolution.Error, "Duplicate declaration handling; Ignore keeps the first definition.");
        var force = new Option<bool>("--force", "-f") { Description = "Allow replacement of existing generated files." };
        var check = new Option<bool>("--check") { Description = "Check expected files without writing. Extra files are ignored." };
        var compile = new Command("compile", "Generate C# models from Avro schemas, protocols, and IDL.")
        {
            inputs, destination, library, language, features, preview, access, declaration, references, duplicates, force, check,
        };
        compile.SetAction(async (result, token) =>
        {
            if (result.GetValue(force) && result.GetValue(check))
            {
                await error.WriteLineAsync("--force and --check cannot be combined.");
                return 2;
            }
            var target = result.GetValue(library) switch
            {
                AvroLibrary.Apache => GenerationTarget.Apache,
                _ when result.GetValue(language) is "7.3" or "8" or "9" => GenerationTarget.Legacy,
                _ => GenerationTarget.Modern,
            };
            var enabled = result.GetValue(features) & GetLanguageFeatures(result.GetValue(language)!);
            enabled = enabled
                .SetRecords(enabled.HasRecords && target != GenerationTarget.Legacy && result.GetValue(declaration) == "record")
                .SetUnions(enabled.HasUnions && (result.GetValue(preview) & PreviewFeatures.Unions) != 0);
            return await CompilationRunner.RunAsync(
                result.GetValue(inputs)!, result.GetValue(destination)!,
                new AvroParseOptions(target, enabled),
                new AvroCompilationOptions(result.GetValue(references), result.GetValue(duplicates)),
                new RenderOptions(target, enabled, result.GetValue(access)),
                result.GetValue(force), result.GetValue(check), output, error, token);
        });
        var root = new RootCommand("Avro Source Generator command-line tool.") { compile };
        var parsed = root.Parse(args);
        try
        {
            var exitCode = await parsed.InvokeAsync(new InvocationConfiguration
            {
                Output = output,
                Error = error,
                EnableDefaultExceptionHandler = false,
            }, cancellationToken);
            return parsed.Errors.Count > 0 && exitCode != 0 ? 2 : exitCode;
        }
        catch (OperationCanceledException)
        {
            return 130;
        }
    }

    private static Option<T> EnumOption<T>(string name, T defaultValue, string description, Func<T, string?>? validate = null) where T : struct, Enum
    {
        var names = Enum.GetNames<T>();
        var flags = typeof(T).IsDefined(typeof(FlagsAttribute), inherit: false);
        return new Option<T>(name)
        {
            Description = $"{description} Values: {string.Join(", ", names)}.",
            DefaultValueFactory = _ => defaultValue,
            CustomParser = result =>
            {
                if (result.Tokens.Count != 1)
                {
                    result.AddError($"{name} requires exactly one value.");
                    return default;
                }
                var value = result.Tokens.Single().Value;
                var parts = value.Split(',');
                if ((!flags && parts.Length != 1) ||
                    parts.Any(part => !names.Contains(part.Trim(), StringComparer.OrdinalIgnoreCase)) ||
                    !Enum.TryParse<T>(value, ignoreCase: true, out var parsed))
                {
                    result.AddError($"Invalid value '{value}' for {name}. Use {string.Join(", ", names)}.");
                    return default;
                }
                if (validate?.Invoke(parsed) is { } message)
                    result.AddError(message);
                return parsed;
            },
        };
    }

    private static LanguageFeatures GetLanguageFeatures(string version) => version switch
    {
        "7.3" => LanguageFeatures.CSharp7_3,
        "8" => LanguageFeatures.CSharp8,
        "9" => LanguageFeatures.CSharp9,
        "10" => LanguageFeatures.CSharp10,
        "11" => LanguageFeatures.CSharp11,
        "12" => LanguageFeatures.CSharp12,
        "13" => LanguageFeatures.CSharp13,
        "14" => LanguageFeatures.CSharp14,
        "15" or "preview" => LanguageFeatures.CSharp15,
        _ => throw new InvalidOperationException("Unexpected validated C# version."),
    };
}
