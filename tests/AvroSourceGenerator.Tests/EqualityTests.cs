using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis.Text;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Context;
using Soenneker.Utils.AutoBogus.Generators;
using Soenneker.Utils.AutoBogus.Override;

namespace AvroSourceGenerator.Tests;

public class EqualityTests
{
    private static Type CompilationInfoType =>
        field ??= typeof(AvroSourceGenerator).Assembly.GetType("AvroSourceGenerator.Configuration.CompilationInfo", throwOnError: true)!;

    private static Type ProjectSettingsType =>
        field ??= typeof(AvroSourceGenerator).Assembly.GetType("AvroSourceGenerator.Configuration.ProjectSettings", throwOnError: true)!;

    private static Type GeneratorConfigType =>
        field ??= typeof(AvroSourceGenerator).Assembly.GetType("AvroSourceGenerator.Configuration.GeneratorConfig", throwOnError: true)!;

    private static Type IAvroFileType =>
        field ??= typeof(AvroSourceGenerator).Assembly.GetType("AvroSourceGenerator.Inputs.IAvroFile", throwOnError: true)!;

    private static Type[] AvroFileTypes =>
        field ??= [.. typeof(AvroSourceGenerator).Assembly.GetTypes().Where(t => IAvroFileType.IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })];

    private readonly AutoFaker _faker;
    private readonly int _seed;

    public EqualityTests()
    {
        _faker = new AutoFaker(opts => opts
            .WithOverride(new JsonElementOverride())
            .WithOverride(new TextSpanOverride())
            .WithOverride(new LinePositionSpanOverride())
            .WithOverride(new AvroFileOverride(IAvroFileType, AvroFileTypes))
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
    public void EnsureProjectSettingsHasValueSemantics()
    {
        var a = Generate(ProjectSettingsType);
        var b = Generate(ProjectSettingsType);

        Assert.Equal(a, b);
    }

    [Fact]
    public void EnsureGeneratorConfigHasValueSemantics()
    {
        var a = Generate(GeneratorConfigType);
        var b = Generate(GeneratorConfigType);

        Assert.Equal(a, b);
    }

    public static TheoryData<Type> AvroFileTypesData => new(AvroFileTypes);

    [Theory]
    [MemberData(nameof(AvroFileTypesData))]
    public void EnsureAvroFileHasValueSemantics(Type avroFileType)
    {
        var a = Generate(avroFileType);
        var b = Generate(avroFileType);

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

file sealed class AvroFileOverride(Type avroFileType, IReadOnlyList<Type> avroFileTypes) : AutoFakerGeneratorOverride
{
    private static MethodInfo CreateFile => field ??= typeof(AvroSourceGenerator).Assembly.GetType("AvroSourceGenerator.Inputs.AvroFile", throwOnError: true)!.GetMethod("FromFileText")!;

    public override bool Preinitialize => false;

    public override bool CanOverride(AutoFakerContext context) => context.GenerateType.IsAssignableTo(avroFileType);

    public override void Generate(AutoFakerOverrideContext context)
    {
        var (path, text) = context.GenerateType.Type?.Name switch
        {
            "AvroSchemaFile" => (Path.ChangeExtension(context.Faker.System.FileName(), "avsc"), TestSchemas.Get("record").ToJsonString()),
            "AvroSubjectFile" => (Path.ChangeExtension(context.Faker.System.FileName(), "subject.json"), new JsonObject().With(
                "schema",
                TestSchemas.Get("record").With(
                    "fields",
                    [
                        new
                        {
                            name = "ExternalEnum",
                            type = "external.namespace.Enum"
                        },
                        new
                        {
                            name = "ExternalRecord",
                            type = "external.namespace.Record"
                        }
                    ]).ToJsonString()).ToJsonString()),
            _ => (Path.ChangeExtension(context.Faker.System.FileName(), "avsc"), context.Faker.Lorem.Paragraph())
        };
        context.Instance = CreateFile.Invoke(null, [path, text])!;
    }
}
