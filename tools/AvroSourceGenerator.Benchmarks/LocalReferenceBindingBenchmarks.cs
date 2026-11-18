using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Compiler.Configuration;
using AvroSourceGenerator.Compiler.Linking;
using AvroSourceGenerator.Text;
using BenchmarkDotNet.Attributes;

namespace AvroSourceGenerator.Benchmarks;

[MemoryDiagnoser]
public class LocalReferenceBindingBenchmarks
{
    private LinkedAvroFile _linkedFile = null!;

    [Params(10, 1_000)]
    public int EnumCount { get; set; }

    [Params(0, 1, 2_000)]
    public int ReferenceCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var enums = string.Join(",", Enumerable.Range(0, EnumCount).Select(index =>
            $$"""{"type":"enum","name":"E{{index}}","symbols":["A"]}"""));
        var enumIndex = ReferenceCount == 1 ? 0 : EnumCount - 1;
        var fields = string.Join(",", Enumerable.Range(0, ReferenceCount).Select(index =>
            $$"""{"name":"f{{index}}","type":"E{{enumIndex}}"}"""));
        var source = new SourceText("binding.avpr", $$"""
            {
              "protocol": "Binding",
              "types": [{{enums}}, {"type":"record","name":"R","fields":[{{fields}}]}],
              "messages": {}
            }
            """);
        var file = AvroFile.Parse(source, new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.None), CancellationToken.None);
        var symbols = SymbolTable.FromFiles([file], CancellationToken.None);
        _linkedFile = LinkedAvroFile.Link(file, symbols, CancellationToken.None);
        if (!_linkedFile.IsValid || _linkedFile.References.Count != 0)
            throw new InvalidOperationException("Expected a valid protocol with only local references.");
    }

    [Benchmark]
    public BoundAvroFile Bind() => BoundAvroFile.Bind(_linkedFile, CancellationToken.None);
}
