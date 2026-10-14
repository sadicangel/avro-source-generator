using System.Reflection;
using System.Text.Json;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

var options = new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.None);
var inputs = new[]
{
    new SourceText("string.avsc", "\"string\""),
    new SourceText("array.avsc", """{"type":"array","items":"string"}"""),
    new SourceText("map.avsc", """{"type":"map","values":"long"}"""),
    new SourceText("union.avsc", """["null","string"]"""),
    new SourceText("logical.avsc", """{"type":"int","logicalType":"date"}"""),
    new SourceText("key.avdl", "schema string;"),
};
foreach (var source in inputs)
{
    var file = AvroFile.Parse(source, options, CancellationToken.None);
    if (!file.IsValid || file.Diagnostics.Length != 0 || file.Declarations.Length != 0)
        throw new InvalidOperationException($"Could not parse {source.Path.OriginalPath}.");
    using var json = JsonDocument.Parse(file.RootSchema.ToJsonString(new Dictionary<SchemaName, TopLevelSchema>()));
    var compilation = AvroCompiler.Compile(new[] { source }, options);
    if (!compilation.IsValid || compilation.Schemas.Count != 0 || compilation.Files[0].RootSchema is null)
        throw new InvalidOperationException($"Could not compile {source.Path.OriginalPath}.");
}
var strict = AvroFile.Parse(inputs[0], options with { RequireTopLevelSchema = true }, CancellationToken.None);
if (strict.IsValid || strict.Diagnostics.Length != 1 || (int)strict.Diagnostics[0].Code != 2002)
    throw new InvalidOperationException("Named-root validation was not applied.");

var assembly = typeof(AvroFile).Assembly;
var expectedVersion = args.Single();
if (assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion != expectedVersion)
    throw new InvalidOperationException("Core assembly version differs from its package.");
if (assembly.GetExportedTypes().Any(type =>
    type.Namespace?.StartsWith("AvroSourceGenerator.Avdl", StringComparison.Ordinal) == true ||
    type.Namespace == "AvroSourceGenerator.Avjs" || type.Name.EndsWith("Parser", StringComparison.Ordinal)))
    throw new InvalidOperationException("Parser machinery was exported.");
Console.WriteLine($"Core {expectedVersion}: parsing and compilation passed.");

#if CHECK_EXTERNAL_INHERITANCE
public sealed record class ExternalSchema : AvroSchema
{
    public ExternalSchema() : base(default, default, default, null, System.Collections.Immutable.ImmutableSortedDictionary<string, JsonElement>.Empty) { }

    public override void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace) { }
}
#endif
