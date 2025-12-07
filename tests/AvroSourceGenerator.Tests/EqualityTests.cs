using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Context;
using Soenneker.Utils.AutoBogus.Override;

namespace AvroSourceGenerator.Tests;

public class EqualityTests
{
    private static Type CompilationInfoType =>
        field ??= typeof(AvroSourceGenerator).Assembly.GetType("AvroSourceGenerator.Configuration.CompilationInfo", throwOnError: true)!;

    private static Type GeneratorSettingsType =>
        field ??= typeof(AvroSourceGenerator).Assembly.GetType("AvroSourceGenerator.Configuration.GeneratorSettings", throwOnError: true)!;

    private static Type SchemaNameType =>
        field ??= typeof(AvroSourceGenerator).Assembly.GetType("AvroSourceGenerator.Schemas.SchemaName", throwOnError: true)!;

    private static ConstructorInfo AvroFileConstructor =>
        field ??= typeof(AvroSourceGenerator).Assembly.GetType("AvroSourceGenerator.Parsing.AvroFile", throwOnError: true)!
            .GetConstructor([typeof(string), typeof(string), typeof(JsonElement), SchemaNameType, typeof(ImmutableArray<Diagnostic>)])!;

    private readonly AutoFaker _faker;
    private readonly int _seed;

    public EqualityTests()
    {
        _faker = new AutoFaker(opts => opts.WithOverride(new JsonElementOverride()));
        _seed = _faker.Generate<int>();
    }

    private object Generate(Type type)
    {
        _faker.UseSeed(_seed);
        return _faker.Generate(type);
    }

    [Fact]
    public void EnsureCompilationInfoHasValueSemantics()
    {
        var a = Generate(CompilationInfoType);
        var b = Generate(CompilationInfoType);

        Assert.Equal(a, b);
    }

    [Fact]
    public void EnsureGeneratorSettingsHasValueSemantics()
    {
        var a = Generate(GeneratorSettingsType);
        var b = Generate(GeneratorSettingsType);

        Assert.Equal(a, b);
    }

    [Fact]
    public void EnsureAvroFileHasValueSemantics()
    {
        var path = _faker.Faker.System.FilePath();
        var text = TestSchemas.Get("record").ToJsonString();
        using var doc = JsonDocument.Parse(text);
        var json = doc.RootElement.Clone();
        var schemaName = Generate(SchemaNameType);
        var diagnostics = _faker.Generate<ImmutableArray<Diagnostic>>();

        var a = AvroFileConstructor.Invoke([path, text, json, schemaName, diagnostics]);
        var b = AvroFileConstructor.Invoke([path, text, json, schemaName, diagnostics]);

        Assert.Equal(a, b);
    }
}

file sealed class JsonElementOverride : AutoFakerOverride<JsonElement>
{
    public override void Generate(AutoFakerOverrideContext context)
    {
        using var jsonDocument = JsonDocument.Parse(
            """
            {
                "type": "record",
                "name": "Record",
                "namespace": "RecordNamespace",
                "fields": [],
                "doc": "A empty record"
            }
            """);
        context.Instance = jsonDocument.RootElement.Clone();
    }
}
