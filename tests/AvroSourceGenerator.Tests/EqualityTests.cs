using System.Text.Json;
using Microsoft.CodeAnalysis.Text;
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

    private static Type RenderSettingsType =>
        field ??= typeof(AvroSourceGenerator).Assembly.GetType("AvroSourceGenerator.Parsing.RenderSettings", throwOnError: true)!;

    private static Type AvroFileType =>
        field ??= typeof(AvroSourceGenerator).Assembly.GetType("AvroSourceGenerator.Parsing.AvroFile", throwOnError: true)!;

    private readonly AutoFaker _faker;
    private readonly int _seed;

    public EqualityTests()
    {
        _faker = new AutoFaker(opts => opts
            .WithOverride(new JsonElementOverride())
            .WithOverride(new TextSpanOverride())
            .WithOverride(new LinePositionSpanOverride())
            .WithOverride(new ObjectArrayOverride()));
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
    public void EnsureRenderSettingsHasValueSemantics()
    {
        var a = Generate(RenderSettingsType);
        var b = Generate(RenderSettingsType);

        Assert.Equal(a, b);
    }

    [Fact]
    public void EnsureAvroFileHasValueSemantics()
    {
        var a = Generate(AvroFileType);
        var b = Generate(AvroFileType);

        Assert.Equal(a, b);
    }
}

file sealed class JsonElementOverride : AutoFakerOverride<JsonElement>
{
    public override bool Preinitialize => false;

    public override void Generate(AutoFakerOverrideContext context)
    {
        using var doc = JsonDocument.Parse(TestSchemas.Get("record").ToJsonString());
        context.Instance = doc.RootElement.Clone();
    }
}

file sealed class TextSpanOverride : AutoFakerOverride<TextSpan>
{
    public override bool Preinitialize => false;

    public override void Generate(AutoFakerOverrideContext context) =>
        context.Instance = new TextSpan(0, 10);
}

file sealed class LinePositionSpanOverride : AutoFakerOverride<LinePositionSpan>
{
    public override bool Preinitialize => false;

    public override void Generate(AutoFakerOverrideContext context) =>
        context.Instance = new LinePositionSpan(LinePosition.Zero, LinePosition.Zero);
}

file sealed class ObjectArrayOverride : AutoFakerOverride<object?[]?>
{
    public override bool Preinitialize => false;

    public override void Generate(AutoFakerOverrideContext context) =>
        context.Instance = (object?[])[context.Faker.Hacker.Noun()];
}
