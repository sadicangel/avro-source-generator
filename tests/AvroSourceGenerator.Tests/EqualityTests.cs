using System.Text.Json;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Inputs;
using Microsoft.CodeAnalysis.Text;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Context;
using Soenneker.Utils.AutoBogus.Override;

namespace AvroSourceGenerator.Tests;

public class EqualityTests
{
    private static Type CompilationInfoType =>
        field ??= typeof(AvroSourceGenerator).Assembly.GetType("AvroSourceGenerator.Configuration.CompilationInfo", throwOnError: true)!;

    private static Type CSharpProjectOptionsType =>
        field ??= typeof(AvroSourceGenerator).Assembly.GetType("AvroSourceGenerator.Configuration.CSharpProjectOptions", throwOnError: true)!;

    private static Type AvroProjectOptionsType =>
        field ??= typeof(AvroSourceGenerator).Assembly.GetType("AvroSourceGenerator.Configuration.AvroProjectOptions", throwOnError: true)!;

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
    public void EnsureCSharpProjectOptionsHasValueSemantics()
    {
        var a = Generate(CSharpProjectOptionsType);
        var b = Generate(CSharpProjectOptionsType);

        Assert.Equal(a, b);
    }

    [Fact]
    public void EnsureAvroProjectOptionsHasValueSemantics()
    {
        var a = Generate(AvroProjectOptionsType);
        var b = Generate(AvroProjectOptionsType);

        Assert.Equal(a, b);
    }

    [Fact]
    public void EnsureAvroFileHasValueSemantics()
    {
        var parseOptions = new AvroParseOptions(
            TargetProfile.Modern,
            UseNullableReferenceTypes: true);
        var source = new global::AvroSourceGenerator.Text.SourceText(
            "schema.avsc",
            TestSchemas.Get("record").ToJsonString());
        var a = AvroFile.FromInput((source, parseOptions), TestContext.Current.CancellationToken);
        var b = AvroFile.FromInput((source, parseOptions), TestContext.Current.CancellationToken);

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
