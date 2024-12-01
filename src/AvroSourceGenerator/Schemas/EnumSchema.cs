using System.Text.Json;


namespace AvroSourceGenerator.Schemas;

internal readonly record struct EnumSchema(JsonElement Json) : IAvroSchema
{
    public JsonElement Name { get => Json.GetProperty("name"); }
    public JsonElement Type { get => Json.GetProperty("type"); }
    public int SymbolsLength { get => Json.GetProperty("symbols").GetArrayLength(); }
    public IEnumerable<JsonElement> Symbols
    {
        get
        {
            var symbols = Json.GetProperty("symbols").EnumerateArray();
            while (symbols.MoveNext())
                yield return symbols.Current;
        }
    }
    public JsonElement? Namespace { get => Json.TryGetProperty("namespace", out var v) ? v : null; }
    public JsonElement? Documentation { get => Json.TryGetProperty("doc", out var v) ? v : null; }
    public JsonElement? Default { get => Json.TryGetProperty("default", out var v) ? v : null; }
    public int AliasesLength { get => Json.TryGetProperty("aliases", out var aliases) ? aliases.GetArrayLength() : 0; }
    public IEnumerable<JsonElement> Aliases
    {
        get
        {
            if (Json.TryGetProperty("aliases", out var aliases))
            {
                var array = aliases.EnumerateArray();
                while (array.MoveNext())
                    yield return array.Current;
            }
        }
    }
}
