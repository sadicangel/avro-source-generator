using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AvroSourceGenerator.Benchmarks;

internal sealed class BenchmarkEnvironment : IDisposable
{
    private static readonly CSharpParseOptions s_parseOptions = new(LanguageVersion.CSharp12);

    private readonly BenchmarkScenario _scenario;
    private readonly CSharpCompilation _compilation;
    private readonly AnalyzerConfigOptionsProvider _optionsProvider;
    private readonly LoadedGenerator _lastGa;
    private readonly LoadedGenerator _current;
    private GeneratorDriver? _lastGaPrimedDriver;
    private GeneratorDriver? _currentPrimedDriver;

    private BenchmarkEnvironment(BenchmarkScenario scenario)
    {
        _scenario = scenario;
        var options = new Dictionary<string, string>
        {
            ["build_property.AvroSourceGeneratorAvroLibrary"] = "Apache",
            ["build_property.AvroSourceGeneratorLanguageFeatures"] = "CSharp12",
            ["build_property.AvroSourceGeneratorAccessModifier"] = "public",
            ["build_property.AvroSourceGeneratorRecordDeclaration"] = "record",
        };
        if (scenario.ChangeScenario == IncrementalChangeScenario.ReferencedSchemaContent)
        {
            options["build_property.AvroSourceGeneratorReferenceResolution"] = "Deferred";
        }

        _optionsProvider = new BenchmarkOptionsProvider(options);
        _compilation = CSharpCompilation.Create(
            assemblyName: "AvroSourceGenerator.BenchmarkConsumer",
            syntaxTrees:
            [
                CSharpSyntaxTree.ParseText(
                    "namespace AvroSourceGenerator.BenchmarkConsumer; internal sealed class Marker;",
                    s_parseOptions),
            ],
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        _lastGa = LoadedGenerator.Load(GeneratorLocations.LastGaAssemblyPath, "last-ga");
        _current = LoadedGenerator.Load(GeneratorLocations.CurrentAssemblyPath, "current-tip");
    }

    public static BenchmarkEnvironment Create(
        int schemaCount,
        bool prepareIncrementalRuns,
        IncrementalChangeScenario changeScenario = IncrementalChangeScenario.IndependentContent)
    {
        var environment = new BenchmarkEnvironment(BenchmarkScenario.Create(schemaCount, changeScenario));

        if (prepareIncrementalRuns)
        {
            environment._lastGaPrimedDriver = environment.Prime(environment._lastGa.Generator);
            environment._currentPrimedDriver = environment.Prime(environment._current.Generator);
        }

        return environment;
    }

    public GeneratorDriverRunResult RunFullLastGa() => RunFull(_lastGa.Generator);

    public GeneratorDriverRunResult RunFullCurrent() => RunFull(_current.Generator);

    public GeneratorDriverRunResult RunIncrementalLastGa() => RunIncremental(_lastGaPrimedDriver);

    public GeneratorDriverRunResult RunIncrementalCurrent() => RunIncremental(_currentPrimedDriver);

    public ValidationResult ValidateFullRuns()
    {
        var lastGa = Validate(RunFullLastGa(), "last GA");
        var current = Validate(RunFullCurrent(), "current tip");
        EnsureSourceCountsMatch(lastGa, current);
        return current;
    }

    public ValidationResult ValidateIncrementalRuns()
    {
        if (_scenario.ChangeScenario == IncrementalChangeScenario.ReferencedSchemaContent)
        {
            throw new InvalidOperationException(
                "The last GA baseline does not support Deferred cross-file references. Use ValidateCurrentIncrementalRun instead.");
        }

        var lastGa = Validate(RunIncrementalLastGa(), "last GA incremental run");
        var current = ValidateCurrentIncrementalRun();
        EnsureSourceCountsMatch(lastGa, current);
        return current;
    }

    public ValidationResult ValidateCurrentIncrementalRun() => Validate(RunIncrementalCurrent(), "current tip incremental run");

    public void Dispose()
    {
        _lastGa.Dispose();
        _current.Dispose();
    }

    private GeneratorDriverRunResult RunFull(ISourceGenerator generator)
    {
        var driver = CreateDriver(generator);
        return driver.RunGenerators(_compilation).GetRunResult();
    }

    private GeneratorDriver Prime(ISourceGenerator generator) => CreateDriver(generator).RunGenerators(_compilation);

    private GeneratorDriverRunResult RunIncremental(GeneratorDriver? primedDriver)
    {
        if (primedDriver is null)
        {
            throw new InvalidOperationException("Incremental runs were not prepared for this benchmark.");
        }

        var changedDriver = primedDriver.ReplaceAdditionalText(_scenario.OriginalFile, _scenario.ChangedFile);
        return changedDriver.RunGenerators(_compilation).GetRunResult();
    }

    private GeneratorDriver CreateDriver(ISourceGenerator generator) => CSharpGeneratorDriver.Create(
        generators: [generator],
        additionalTexts: _scenario.Files,
        parseOptions: s_parseOptions,
        optionsProvider: _optionsProvider);

    private ValidationResult Validate(GeneratorDriverRunResult result, string version)
    {
        var errors = result.Diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString())
            .ToArray();

        if (errors.Length != 0)
        {
            throw new InvalidOperationException(
                $"The {version} generator reported errors:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }

        var generatorResult = result.Results.Single();
        var generatedSourceCount = generatorResult.GeneratedSources.Length;
        if (generatedSourceCount != _scenario.SchemaCount)
        {
            throw new InvalidOperationException(
                $"The {version} generator produced {generatedSourceCount} sources; expected {_scenario.SchemaCount}.");
        }

        return new ValidationResult(generatedSourceCount);
    }

    private static void EnsureSourceCountsMatch(ValidationResult lastGa, ValidationResult current)
    {
        if (lastGa.GeneratedSourceCount != current.GeneratedSourceCount)
        {
            throw new InvalidOperationException(
                $"The versions produced different source counts: GA={lastGa.GeneratedSourceCount}, current={current.GeneratedSourceCount}.");
        }
    }

    internal readonly record struct ValidationResult(int GeneratedSourceCount);

    private sealed class BenchmarkOptionsProvider(IReadOnlyDictionary<string, string> options)
        : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new BenchmarkOptions(options);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
    }

    private sealed class BenchmarkOptions(IReadOnlyDictionary<string, string> options) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) =>
            options.TryGetValue(key, out value);
    }
}

internal sealed class LoadedGenerator : IDisposable
{
    private GeneratorLoadContext? _loadContext;

    private LoadedGenerator(ISourceGenerator generator, GeneratorLoadContext loadContext)
    {
        Generator = generator;
        _loadContext = loadContext;
    }

    public ISourceGenerator Generator { get; private set; }

    public static LoadedGenerator Load(string assemblyPath, string contextName)
    {
        var loadContext = new GeneratorLoadContext(contextName, assemblyPath);
        var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
        var generatorType = assembly.GetType(
            "AvroSourceGenerator.AvroSourceGenerator",
            throwOnError: true,
            ignoreCase: false)!;
        var incrementalGenerator = (IIncrementalGenerator)Activator.CreateInstance(generatorType)!;
        return new LoadedGenerator(incrementalGenerator.AsSourceGenerator(), loadContext);
    }

    public void Dispose()
    {
        Generator = null!;
        _loadContext?.Unload();
        _loadContext = null;
    }
}

internal sealed class GeneratorLoadContext : AssemblyLoadContext
{
    private readonly string _assemblyDirectory;
    private readonly AssemblyDependencyResolver _resolver;

    public GeneratorLoadContext(string name, string componentAssemblyPath)
        : base(name, isCollectible: true)
    {
        _assemblyDirectory = Path.GetDirectoryName(componentAssemblyPath)!;
        _resolver = new AssemblyDependencyResolver(componentAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (IsSharedAssembly(assemblyName.Name))
        {
            return Default.Assemblies.FirstOrDefault(
                assembly => string.Equals(assembly.GetName().Name, assemblyName.Name, StringComparison.Ordinal));
        }

        var resolvedPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (resolvedPath is not null)
        {
            return LoadFromAssemblyPath(resolvedPath);
        }

        var adjacentPath = Path.Combine(_assemblyDirectory, $"{assemblyName.Name}.dll");
        return File.Exists(adjacentPath) ? LoadFromAssemblyPath(adjacentPath) : null;
    }

    private static bool IsSharedAssembly(string? assemblyName) =>
        assemblyName is "netstandard" or "mscorlib" or "System.Collections.Immutable" ||
        assemblyName?.StartsWith("System.", StringComparison.Ordinal) == true ||
        assemblyName?.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal) == true;
}

internal static class GeneratorLocations
{
    private const string MetadataPrefix = "AvroSourceGenerator.Benchmarks.";

    public static string LastGaVersion { get; } = GetMetadata("LastGaVersion");

    public static string LastGaAssemblyPath { get; } = Path.GetFullPath(GetMetadata("LastGaAssembly"));

    public static string CurrentAssemblyPath { get; } = Path.GetFullPath(GetMetadata("CurrentAssembly"));

    public static void EnsureAssembliesExist()
    {
        EnsureAssemblyExists(LastGaAssemblyPath, $"last GA ({LastGaVersion})");
        EnsureAssemblyExists(CurrentAssemblyPath, "current tip");
    }

    private static string GetMetadata(string name) => typeof(GeneratorLocations).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(attribute => attribute.Key == $"{MetadataPrefix}{name}")
        .Value!;

    private static void EnsureAssemblyExists(string path, string description)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Could not find the {description} generator assembly.", path);
        }
    }
}
