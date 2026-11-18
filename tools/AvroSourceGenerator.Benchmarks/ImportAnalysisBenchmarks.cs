using System.Collections.Immutable;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Compiler.Configuration;
using AvroSourceGenerator.Compiler.Linking;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Benchmarks;

[MemoryDiagnoser]
public class ImportAnalysisBenchmarks
{
    private const int ManyReferences = 256;
    private ImmutableArray<BoundAvroFile> _files;
    private AvroCompilationOptions _options;

    [ParamsAllValues]
    public ImportWorkload Workload { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        SourceText[] sources = Workload switch
        {
            ImportWorkload.SmallStandalone => [CreateIdl("Consumer")],
            ImportWorkload.SmallImport => CreateChain(1, 0),
            ImportWorkload.SharedProtocol => CreateChain(ManyReferences, 0),
            ImportWorkload.ChainSingleReference => CreateChain(1, 32),
            ImportWorkload.ChainManyReferences => CreateChain(ManyReferences, 32),
            ImportWorkload.DiamondSingleReference => CreateDiamond(1),
            ImportWorkload.DiamondManyReferences => CreateDiamond(ManyReferences),
            ImportWorkload.DistinctOwners => CreateDistinctOwners(512),
            ImportWorkload.Unresolved => CreateUnresolved(),
            _ => throw new ArgumentOutOfRangeException(nameof(Workload)),
        };

        var parseOptions = new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.None);
        var parsed = sources.Select(source => AvroFile.Parse(source, parseOptions, CancellationToken.None)).ToImmutableArray();
        var symbols = SymbolTable.FromFiles(parsed, CancellationToken.None);
        _files = parsed.Select(file => BoundAvroFile.Bind(LinkedAvroFile.Link(file, symbols, CancellationToken.None), CancellationToken.None))
            .ToImmutableArray();
        _options = Workload == ImportWorkload.DistinctOwners
            ? new AvroCompilationOptions(ReferenceResolution.Deferred)
            : new AvroCompilationOptions();

        AvroDiagnosticCode[] expected = Workload switch
        {
            ImportWorkload.Unresolved => [AvroDiagnosticCode.ImportCycle, AvroDiagnosticCode.MissingImport, AvroDiagnosticCode.MissingReferences],
            ImportWorkload.DiamondSingleReference or ImportWorkload.DiamondManyReferences => [AvroDiagnosticCode.UnusedImport],
            _ => [],
        };
        var actual = Compile().Diagnostics.Select(diagnostic => diagnostic.Code).Order().ToArray();
        if (!actual.SequenceEqual(expected.Order()))
            throw new InvalidOperationException($"Unexpected {Workload} diagnostics: {string.Join(", ", actual)}.");
    }

    [Benchmark]
    public AvroCompilation Compile() => AvroCompilation.Create(_files, _options);

    private static SourceText[] CreateChain(int references, int links)
    {
        var sources = new List<SourceText> { CreateOwner(references) };
        var target = "protocol \"Owner.avpr\"";
        for (var index = links - 1; index >= 0; index--)
        {
            sources.Add(CreateIdl($"Link{index}", imports: $"import {target};"));
            target = $"idl \"Link{index}.avdl\"";
        }
        sources.Insert(0, CreateIdl("Consumer", CreateFields(references), $"import {target};"));
        return [.. sources];
    }

    internal static SourceText[] CreateDiamond(int references) =>
    [
        CreateIdl("Consumer", CreateFields(references), """
            import idl "Left.avdl";
            import idl "Right.avdl";
            import idl "Independent.avdl";
            """),
        CreateIdl("Left", imports: """import protocol "Owner.avpr";"""),
        CreateIdl("Right", imports: """import protocol "Owner.avpr";"""),
        CreateOwner(references),
        CreateIdl("Independent"),
    ];

    private static SourceText[] CreateDistinctOwners(int owners) =>
        [.. Enumerable.Range(0, owners).SelectMany(index => new[]
        {
            CreateIdl($"Consumer{index}", $"Owner{index} value;", $"import idl \"Owner{index}.avdl\";"),
            CreateIdl($"Owner{index}"),
        })];

    private static SourceText[] CreateUnresolved() =>
    [
        CreateIdl("Consumer", CreateFields(ManyReferences), """
            import idl "Left.avdl";
            import idl "Missing.avdl";
            """),
        CreateIdl("Left", imports: """import idl "Right.avdl";"""),
        CreateIdl("Right", imports: """import idl "Left.avdl";"""),
        CreateIdl("OtherConsumer", CreateFields(ManyReferences), """import idl "Independent.avdl";"""),
        CreateIdl("Independent"),
    ];

    private static string CreateFields(int references) => string.Join(' ', Enumerable.Range(0, references)
        .Select(index => $"Type{index} field{index};"));

    private static SourceText CreateOwner(int declarations)
    {
        var types = string.Join(',', Enumerable.Range(0, declarations).Select(index =>
            $$"""{"type":"record","name":"Type{{index}}","fields":[]}"""));
        return new SourceText("Owner.avpr", $$"""{"protocol":"Owner","types":[{{types}}],"messages":{} }""");
    }

    private static SourceText CreateIdl(string name, string fields = "", string imports = "") =>
        new SourceText($"{name}.avdl", $"{imports}\nschema {name};\nrecord {name} {{ {fields} }}");
}

[MemoryDiagnoser]
public class ImportAnalysisIncrementalBenchmarks
{
    private BenchmarkEnvironment _environment = null!;

    [Params(IncrementalChangeScenario.IndependentContent, IncrementalChangeScenario.ReferencedSchemaContent)]
    public IncrementalChangeScenario ChangeScenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _environment = BenchmarkEnvironment.CreateCurrentIncremental(BenchmarkScenario.CreateImportEdit(ChangeScenario));
        _environment.ValidateCurrentIncrementalRun();
    }

    [Benchmark]
    public GeneratorDriverRunResult ApplyEdit() => _environment.RunIncrementalCurrent();

    [GlobalCleanup]
    public void Cleanup() => _environment.Dispose();
}

public enum ImportWorkload
{
    SmallStandalone,
    SmallImport,
    SharedProtocol,
    ChainSingleReference,
    ChainManyReferences,
    DiamondSingleReference,
    DiamondManyReferences,
    DistinctOwners,
    Unresolved,
}
