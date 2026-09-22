using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Avsc.Syntax;

internal ref struct JsonReader
{
    private static readonly Action<JsonSyntax, JsonSyntax> s_setParent = typeof(JsonSyntax).GetProperty(nameof(JsonSyntax.Parent))!
        .GetSetMethod().CreateDelegate<Action<JsonSyntax, JsonSyntax>>();

    private readonly SourceText _source;
    private readonly byte[] _utf8;
    private readonly CancellationToken _cancellationToken;
    private Utf8JsonReader _reader;
    private int _byteOffset;
    private int _characterOffset;

    public JsonReader(SourceText source, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _source = source;
        _cancellationToken = cancellationToken;
        _utf8 = Encoding.UTF8.GetBytes(source.Text);
        _reader = new Utf8JsonReader(_utf8);
        _byteOffset = 0;
        _characterOffset = 0;
        CurrentSpan = default;
    }

    public SourceSpan CurrentSpan { get; private set; }

    internal bool Read()
    {
        _cancellationToken.ThrowIfCancellationRequested();
        if (!_reader.Read()) return false;
        var start = AdvanceTo(_reader.TokenStartIndex);
        var end = _reader.TokenType == JsonTokenType.PropertyName
            ? _reader.TokenStartIndex + _reader.ValueSpan.Length + 2
            : _reader.BytesConsumed;
        CurrentSpan = _source.GetSourceSpan(start, AdvanceTo(end) - start);
        return true;
    }

    // Boundaries advance monotonically, including whitespace and punctuation between
    // tokens. Count every UTF-8 byte at most once when translating to UTF-16 offsets.
    private int AdvanceTo(long boundary)
    {
        var end = checked((int)boundary);
        _characterOffset += Encoding.UTF8.GetCharCount(_utf8.AsSpan(_byteOffset, end - _byteOffset));
        _byteOffset = end;
        return _characterOffset;
    }

    private static JsonArraySyntax SetParents(JsonArraySyntax array)
    {
        foreach (var item in array.Items)
            s_setParent(item, array);
        return array;
    }

    private static JsonObjectSyntax SetParents(JsonObjectSyntax @object)
    {
        foreach (var (_, value) in @object.Properties)
            s_setParent(value, @object);
        return @object;
    }

    public JsonSyntax Parse()
    {
        _cancellationToken.ThrowIfCancellationRequested();
        var start = CurrentSpan.Offset;
        switch (_reader.TokenType)
        {
            case JsonTokenType.StartArray:
                {
                    var items = ImmutableArray.CreateBuilder<JsonSyntax>();
                    while (Read() && _reader.TokenType != JsonTokenType.EndArray)
                    {
                        _cancellationToken.ThrowIfCancellationRequested();
                        items.Add(Parse());
                    }

                    return SetParents(new JsonArraySyntax(ContainerSpan(start), items.DrainToImmutable()));
                }
            case JsonTokenType.StartObject:
                {
                    var properties = ImmutableArray.CreateBuilder<JsonPropertySyntax>();
                    var nameLookup = new Dictionary<string, int>(StringComparer.Ordinal);
                    ImmutableArray<JsonPropertySyntax>.Builder? duplicates = null;
                    while (Read() && _reader.TokenType != JsonTokenType.EndObject)
                    {
                        _cancellationToken.ThrowIfCancellationRequested();
                        var propertyName = new JsonPropertyNameSyntax(_reader.GetString()!, CurrentSpan);
                        Read();
                        var propertyValue = Parse();
                        var property = new JsonPropertySyntax(propertyName, propertyValue);
                        if (!nameLookup.TryGetValue(property.Name.Value, out var index))
                        {
                            nameLookup[property.Name.Value] = properties.Count;
                            properties.Add(property);
                        }
                        else
                        {
                            var previous = properties[index];
                            duplicates ??= ImmutableArray.CreateBuilder<JsonPropertySyntax>();
                            duplicates.Add(previous);
                            properties[index] = property;
                        }
                    }
                    return SetParents(
                        new JsonObjectSyntax(
                            SourceSpan: ContainerSpan(start),
                            Properties: properties.DrainToImmutable(),
                            NameLookup: nameLookup.ToFrozenDictionary(),
                            Duplicates: duplicates?.DrainToImmutable() ?? []));
                }
            case JsonTokenType.String:
            case JsonTokenType.Number:
            case JsonTokenType.True:
            case JsonTokenType.False:
            case JsonTokenType.Null:
                return new JsonValueSyntax(CurrentSpan, _reader.TokenType);
            default:
                throw new InvalidOperationException("Expected a JSON value token.");
        }
    }

    private SourceSpan ContainerSpan(int start) =>
        _source.GetSourceSpan(start, CurrentSpan.Offset + CurrentSpan.Length - start);
}
