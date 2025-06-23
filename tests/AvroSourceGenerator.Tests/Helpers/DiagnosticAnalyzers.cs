using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AvroSourceGenerator.Tests.Helpers;

internal static class DiagnosticAnalyzers
{
    public static ImmutableArray<DiagnosticAnalyzer> Analyzers { get; }

    static DiagnosticAnalyzers()
    {
        var folder = GetAnalyzersFolder("9");

        var asmLoader = new AnalyzerAssemblyLoaderImplementation();
        var analyzerReferences = Directory.EnumerateFiles(folder, "*.dll")
            .Select(dll => new AnalyzerFileReference(dll, asmLoader))
            .SelectMany(x => x.GetAnalyzers(LanguageNames.CSharp))
            .ToImmutableArray();

        Analyzers = analyzerReferences;
    }

    private static string GetAnalyzersFolder(string sdkHint)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--list-sdks",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        var sdks = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line =>
            {
                var parts = line.Split(['[', ']'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return (Version: parts[0], Path: Path.Combine(parts[1], parts[0]));
            })
            .OrderBy(x => x.Version)
            .ToList();

        var sdk = sdks.Find(x => x.Version.StartsWith(sdkHint)).Path ?? sdks[^1].Path;

        return Path.Combine(sdk, "Sdks", "Microsoft.NET.Sdk", "analyzers");
    }

    private sealed class AnalyzerAssemblyLoaderImplementation : IAnalyzerAssemblyLoader
    {
        public void AddDependencyLocation(string fullPath) { }
        public Assembly LoadFromPath(string fullPath) => Assembly.LoadFrom(fullPath);
    }
}
