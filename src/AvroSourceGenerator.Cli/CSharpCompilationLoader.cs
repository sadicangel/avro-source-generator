using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace AvroSourceGenerator.Cli;

internal sealed record CSharpInputCompilation(CSharpCompilation Compilation, SyntaxTree SyntaxTree);

internal static class CSharpCompilationLoader
{
    private static readonly StringComparison s_pathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    public static async Task<CSharpInputCompilation> LoadAsync(
        string sourcePath, string? projectPath, string? framework, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        sourcePath = ValidateFile(sourcePath, ".cs");
        if (projectPath is not null)
        {
            projectPath = ValidateFile(projectPath, ".csproj");
            return await LoadProjectAsync([projectPath], sourcePath, framework, cancellationToken)
                ?? throw new InputException($"Project '{projectPath}' does not include '{sourcePath}'.");
        }

        for (var directory = new FileInfo(sourcePath).Directory; directory is not null; directory = directory.Parent)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candidates = directory.EnumerateFiles("*.csproj").Select(file => file.FullName).Order(StringComparer.Ordinal).ToArray();
            if (candidates.Length > 0 && await LoadProjectAsync(candidates, sourcePath, framework, cancellationToken) is { } input)
                return input;
        }

        if (framework is not null)
            throw new InputException("--framework requires a project that includes the input file.");

        var text = await File.ReadAllTextAsync(sourcePath, cancellationToken);
        var tree = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.CSharp14), sourcePath, cancellationToken: cancellationToken);
        var compilation = CSharpCompilation.Create(
            Path.GetFileNameWithoutExtension(sourcePath),
            [tree],
            Net100.References.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        return new CSharpInputCompilation(compilation, tree);
    }

    private static string ValidateFile(string path, string extension)
    {
        if (!path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            throw new InputException($"Unsupported input file '{path}'. Expected {extension}.");
        if (!File.Exists(path))
            throw new InputException($"Input '{path}' does not exist.");
        return Path.GetFullPath(path);
    }

    private static async Task<CSharpInputCompilation?> LoadProjectAsync(
        string[] candidates, string sourcePath, string? framework, CancellationToken cancellationToken)
    {
        using var workspace = MSBuildWorkspace.Create();
        var progress = new FrameworkProgress();
        string? matchingPath = null;
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!GetVariants(workspace, candidate).Any())
            {
                await workspace.OpenProjectAsync(candidate, progress, cancellationToken);
                var failures = workspace.Diagnostics.Where(diagnostic => diagnostic.Kind == WorkspaceDiagnosticKind.Failure).ToArray();
                if (failures.Length > 0)
                    throw new InvalidDataException(string.Join(Environment.NewLine, failures.Select(diagnostic => diagnostic.Message)));
            }
            if (!GetVariants(workspace, candidate).Any(project => FindDocument(project, sourcePath) is not null))
                continue;
            if (matchingPath is not null)
                throw new InputException($"Both '{matchingPath}' and '{candidate}' include '{sourcePath}'. Specify --project.");
            matchingPath = candidate;
        }
        if (matchingPath is null)
            return null;

        var variants = GetVariants(workspace, matchingPath).ToArray();
        var project = SelectVariant(variants, matchingPath, framework, progress);
        var document = FindDocument(project, sourcePath)
            ?? throw new InputException($"Project '{matchingPath}' does not include '{sourcePath}' for '{framework}'.");
        var tree = await document.GetSyntaxTreeAsync(cancellationToken);
        var compilation = await project.GetCompilationAsync(cancellationToken);
        if (tree is null || compilation is not CSharpCompilation csharpCompilation)
            throw new InvalidOperationException($"Roslyn did not create a C# compilation and syntax tree for '{sourcePath}'.");
        if (csharpCompilation.GetSpecialType(SpecialType.System_Object).TypeKind == TypeKind.Error)
            throw new InvalidDataException($"Project '{matchingPath}' did not load its .NET reference assemblies. Run dotnet restore for the project and verify that its target framework reference pack is available.");
        return new CSharpInputCompilation(csharpCompilation, tree);
    }

    private static IEnumerable<Project> GetVariants(MSBuildWorkspace workspace, string path) =>
        workspace.CurrentSolution.Projects.Where(project => string.Equals(project.FilePath, path, s_pathComparison));

    private static Document? FindDocument(Project project, string path) =>
        project.Documents.FirstOrDefault(document => string.Equals(document.FilePath, path, s_pathComparison));

    private static Project SelectVariant(Project[] variants, string path, string? framework, FrameworkProgress progress)
    {
        if (framework is null)
        {
            if (variants.Length > 1)
                throw new InputException($"Project '{path}' targets multiple frameworks. Specify --framework.");
            return variants.Single();
        }

        // Roslyn appends the target framework to the project name when it loads multiple variants.
        var match = variants.Length > 1
            ? variants.SingleOrDefault(project => project.Name.EndsWith($"({framework})", StringComparison.OrdinalIgnoreCase))
            : progress.Contains(path, framework) ? variants.Single() : null;
        return match ?? throw new InputException($"Project '{path}' does not target '{framework}'.");
    }

    internal sealed class InputException(string message) : Exception(message);

    private sealed class FrameworkProgress : IProgress<ProjectLoadProgress>
    {
        private readonly List<ProjectLoadProgress> _frameworks = [];

        public void Report(ProjectLoadProgress value)
        {
            if (value.Operation == ProjectLoadOperation.Resolve)
                _frameworks.Add(value);
        }

        public bool Contains(string path, string framework) =>
            _frameworks.Any(value => string.Equals(value.FilePath, path, s_pathComparison) &&
                string.Equals(value.TargetFramework, framework, StringComparison.OrdinalIgnoreCase));
    }
}
