using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AvroSourceGenerator.Benchmarks;

internal sealed class BenchmarkEnvironment : IDisposable
{
    private readonly BenchmarkScenario _scenario;
    private readonly CSharpParseOptions _parseOptions;
    private readonly CSharpCompilation _compilation;
    private readonly AnalyzerConfigOptionsProvider _optionsProvider;
    private readonly LoadedGenerator _baseline;
    private readonly LoadedGenerator _current;
    private GeneratorDriver? _baselinePrimedDriver;
    private GeneratorDriver? _currentPrimedDriver;

    private BenchmarkEnvironment(BenchmarkScenario scenario, LanguageVersion languageVersion = LanguageVersion.CSharp12, bool nativeUnions = false)
    {
        _scenario = scenario;
        _parseOptions = new CSharpParseOptions(languageVersion);
        var options = new Dictionary<string, string>
        {
            ["build_property.AvroSourceGeneratorAvroLibrary"] = "Apache",
            ["build_property.AvroSourceGeneratorLanguageFeatures"] = languageVersion == LanguageVersion.Preview ? "CSharp15" : "CSharp12",
            ["build_property.AvroSourceGeneratorAccessModifier"] = "public",
            ["build_property.AvroSourceGeneratorRecordDeclaration"] = "record",
        };
        if (nativeUnions)
        {
            options["build_property.AvroSourceGeneratorPreviewFeatures"] = "Unions";
            options["build_property.AvroSourceGeneratorNativeUnions"] = "true";
        }

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
                    _parseOptions),
            ],
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        _baseline = LoadedGenerator.Load(GeneratorLocations.BaselineAssemblyPath, "baseline", GeneratorLocations.BaselineSharedDirectory);
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
            environment._baselinePrimedDriver = environment.Prime(environment._baseline.Generator);
            environment._currentPrimedDriver = environment.Prime(environment._current.Generator);
        }

        return environment;
    }

    public static BenchmarkEnvironment CreateCurrentIncremental(BenchmarkScenario scenario)
    {
        var environment = new BenchmarkEnvironment(scenario);
        environment._currentPrimedDriver = environment.Prime(environment._current.Generator);
        return environment;
    }

    public static BenchmarkEnvironment CreateUnionCollections(int schemaCount, UnionCollectionWorkload workload) =>
        new BenchmarkEnvironment(
            BenchmarkScenario.CreateUnionCollections(schemaCount, workload != UnionCollectionWorkload.NativeFieldUnions),
            LanguageVersion.Preview,
            nativeUnions: workload != UnionCollectionWorkload.CollectionUnionsOptOut);

    public GeneratorDriverRunResult RunFullBaseline() => RunFull(_baseline.Generator);

    public GeneratorDriverRunResult RunFullCurrent() => RunFull(_current.Generator);

    public GeneratorDriverRunResult RunIncrementalBaseline() => RunIncremental(_baselinePrimedDriver);

    public GeneratorDriverRunResult RunIncrementalCurrent() => RunIncremental(_currentPrimedDriver);

    public ValidationResult ValidateFullRuns(int? baselineSourceCount = null, int? currentSourceCount = null)
    {
        Validate(RunFullBaseline(), "baseline", baselineSourceCount);
        return Validate(RunFullCurrent(), "current tip", currentSourceCount);
    }

    public ValidationResult ValidateIncrementalRuns()
    {
        Validate(RunIncrementalBaseline(), "baseline incremental run");
        return ValidateCurrentIncrementalRun();
    }

    public ValidationResult ValidateCurrentIncrementalRun() => Validate(RunIncrementalCurrent(), "current tip incremental run");

    public void Dispose()
    {
        _baseline.Dispose();
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
        if (primedDriver is null || _scenario.ChangedFile is not { } changedFile)
        {
            throw new InvalidOperationException("Incremental runs were not prepared for this benchmark.");
        }

        var changedDriver = primedDriver.ReplaceAdditionalText(_scenario.OriginalFile, changedFile);
        return changedDriver.RunGenerators(_compilation).GetRunResult();
    }

    private GeneratorDriver CreateDriver(ISourceGenerator generator) => CSharpGeneratorDriver.Create(
        generators: [generator],
        additionalTexts: _scenario.Files,
        parseOptions: _parseOptions,
        optionsProvider: _optionsProvider);

    private ValidationResult Validate(GeneratorDriverRunResult result, string version, int? expectedSourceCount = null)
    {
        var errors = result.Diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString())
            .ToArray();

        if (errors.Length != 0)
        {
            throw new InvalidOperationException($"The {version} generator reported errors:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }

        var generatorResult = result.Results.Single();
        if (generatorResult.Exception is { } exception)
            throw new InvalidOperationException($"The {version} generator threw an exception.", exception);

        var generatedSourceCount = generatorResult.GeneratedSources.Length;
        var expected = expectedSourceCount ?? _scenario.SchemaCount;
        if (generatedSourceCount != expected)
        {
            throw new InvalidOperationException($"The {version} generator produced {generatedSourceCount} sources; expected {expected}.");
        }

        return new ValidationResult(generatedSourceCount);
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

    public static LoadedGenerator Load(string assemblyPath, string contextName, string? sharedDirectory = null)
    {
        var loadContext = new GeneratorLoadContext(contextName, assemblyPath, sharedDirectory);
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

internal sealed class GeneratorLoadContext(string name, string componentAssemblyPath, string? sharedDirectory) : AssemblyLoadContext(name, isCollectible: true)
{
    private readonly string _assemblyDirectory = Path.GetDirectoryName(componentAssemblyPath)!;
    private readonly AssemblyDependencyResolver _resolver = new AssemblyDependencyResolver(componentAssemblyPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (IsSharedAssembly(assemblyName.Name))
        {
            return Default.Assemblies.FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, assemblyName.Name, StringComparison.Ordinal));
        }

        var resolvedPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (resolvedPath is not null)
        {
            return LoadFromAssemblyPath(resolvedPath);
        }

        var adjacentPath = Path.Combine(_assemblyDirectory, $"{assemblyName.Name}.dll");
        if (File.Exists(adjacentPath))
            return LoadFromAssemblyPath(adjacentPath);

        var sharedPath = sharedDirectory is null ? null : Path.Combine(sharedDirectory, $"{assemblyName.Name}.dll");
        return sharedPath is not null && File.Exists(sharedPath) ? LoadFromAssemblyPath(sharedPath) : null;
    }

    private static bool IsSharedAssembly(string? assemblyName) =>
        assemblyName is "netstandard" or "mscorlib" or "System.Collections.Immutable" ||
        assemblyName?.StartsWith("System.", StringComparison.Ordinal) == true ||
        assemblyName?.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal) == true;
}

internal static class GeneratorLocations
{
    private const string MetadataPrefix = "AvroSourceGenerator.Benchmarks.";
    private const string GeneratorAssemblyName = "AvroSourceGenerator.dll";
    private static readonly string s_baselineAnalyzersDirectory = Path.GetFullPath(GetMetadata("BaselineAnalyzersDirectory"));

    public static string BaselineVersion { get; } = GetMetadata("BaselineVersion");

    public static string BaselineSharedDirectory { get; } = Path.Combine(s_baselineAnalyzersDirectory, "cs");
    public static string BaselineAssemblyPath { get; } = FindBaselineAssembly();

    public static string CurrentAssemblyPath { get; } = Path.GetFullPath(GetMetadata("CurrentAssembly"));

    public static void EnsureAssembliesExist()
    {
        EnsureAssemblyExists(BaselineAssemblyPath, $"baseline ({BaselineVersion})");
        EnsureAssemblyExists(CurrentAssemblyPath, "current tip");
    }

    private static string FindBaselineAssembly()
    {
        var compilerVersion = typeof(CSharpCompilation).Assembly.GetName().Version!;
        const string prefix = "roslyn";
        var bestVersion = new Version(0, 0);
        var bestAssembly = Path.Combine(BaselineSharedDirectory, GeneratorAssemblyName);
        if (!Directory.Exists(s_baselineAnalyzersDirectory))
            return bestAssembly;

        foreach (var directory in Directory.EnumerateDirectories(s_baselineAnalyzersDirectory, prefix + "*"))
        {
            var candidate = Path.Combine(directory, "cs", GeneratorAssemblyName);
            if (Version.TryParse(Path.GetFileName(directory).AsSpan(prefix.Length), out var version)
                && version > bestVersion && version <= compilerVersion && File.Exists(candidate))
            {
                bestVersion = version;
                bestAssembly = candidate;
            }
        }
        return bestAssembly;
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
