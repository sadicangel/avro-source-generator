using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Tests.Infrastructure;

public interface ISnapshot<TSnapshot>
{
    static abstract ImmutableArray<MetadataReference> References { get; }
    static abstract ProjectConfig ProjectConfig { get; }

    static virtual ImmutableArray<Diagnostic> FilterDiagnostics(ImmutableArray<Diagnostic> diagnostics) => diagnostics;
}

public delegate ProjectConfig ConfigureProject(ProjectConfig config);

public static class SnapshotExtensions
{
    extension<TSnapshot>(TSnapshot) where TSnapshot : ISnapshot<TSnapshot>
    {
        private static GeneratorOutput Run(ImmutableArray<ProjectFile> projectFiles, ConfigureProject? configure = null) =>
            GeneratorOutput.Create(GeneratorInput.Create(projectFiles, TSnapshot.References, configure?.Invoke(TSnapshot.ProjectConfig) ?? TSnapshot.ProjectConfig));

        public static SettingsTask Schema([StringSyntax(StringSyntaxAttribute.Json)] string schema, ConfigureProject? configure = null, [CallerFilePath] string sourceFile = "") =>
            Files<TSnapshot>([ProjectFile.Schema(schema)], configure, sourceFile);

        public static SettingsTask Files(ImmutableArray<ProjectFile> projectFiles, ConfigureProject? configure = null, [CallerFilePath] string sourceFile = "")
        {
            var (diagnostics, documents) = Run<TSnapshot>(projectFiles, configure);

            diagnostics = TSnapshot.FilterDiagnostics(diagnostics);

            if (diagnostics.Length > 0)
            {
                Assert.Fail(
                    string.Join(
                        Environment.NewLine,
                        diagnostics.Select(d => $"{d.Id}: {d.GetMessage(CultureInfo.InvariantCulture)}")));
            }

            return Verify(documents.Select(document => new Target("txt", document.Content)), sourceFile: sourceFile);
        }

        public static SettingsTask Diagnostic([StringSyntax(StringSyntaxAttribute.Json)] string schema, ConfigureProject? configure = null, [CallerFilePath] string sourceFile = "") =>
            Diagnostic<TSnapshot>([ProjectFile.Schema(schema)], configure, sourceFile);

        public static SettingsTask Diagnostic(ImmutableArray<ProjectFile> projectFiles, ConfigureProject? configure = null, [CallerFilePath] string sourceFile = "")
        {
            var (diagnostics, _) = Run<TSnapshot>(projectFiles, configure);

            diagnostics = TSnapshot.FilterDiagnostics(diagnostics);

            return Verify(Assert.Single(diagnostics), sourceFile: sourceFile);
        }
    }
}
