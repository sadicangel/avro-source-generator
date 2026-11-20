using System.Text.Json;
using AvroSourceGenerator.Syntax;
using AvroSourceGenerator.Syntax.Json;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.UnitTests.Avsc;

public sealed class JsonCompatibilityTests
{
    [Theory]
    [InlineData("""{"a":1,"\u0061":{"items":[null,false,true,[],{}]},"A":-0}""")]
    [InlineData("""[0,-0,2147483648,-2147483649,1.0000,1e-3,-1.25E+12,1e99999]""")]
    [InlineData("""{"é😀":"\"\\\/\b\f\n\r\t\u0000","\uD83D\uDE00":"😀","":""}""")]
    [InlineData(" \t\r\n [true, false, null] \t\r\n ")]
    public void Valid_json_preserves_the_same_values_and_raw_text(string json)
    {
        using var reference = JsonDocument.Parse(json);
        var tree = Parser.ParseJson(new SourceText("test.avsc", json), TestContext.Current.CancellationToken);

        Assert.Empty(tree.Diagnostics);
        AssertSameValue(reference.RootElement, tree.Root);
    }

    private static void AssertSameValue(JsonElement expected, JsonValueSyntax actual)
    {
        Assert.Equal(expected.GetRawText(), actual.GetRawText().ToString());
        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = Assert.IsType<JsonObjectSyntax>(actual);
                var members = expected.EnumerateObject().ToArray();
                Assert.Equal(members.Select(member => member.Name), obj.Members.Select(member => member.Name.ValueText));
                for (var index = 0; index < members.Length; index++)
                    AssertSameValue(members[index].Value, obj.Members[index].Value);
                foreach (var name in members.Select(member => member.Name).Distinct(StringComparer.Ordinal))
                    Assert.Equal(expected.GetProperty(name).GetRawText(), obj.Property(name)!.Value.GetRawText().ToString());
                break;
            case JsonValueKind.Array:
                var array = Assert.IsType<JsonArraySyntax>(actual);
                Assert.Equal(expected.GetArrayLength(), array.Elements.Length);
                for (var index = 0; index < array.Elements.Length; index++)
                    AssertSameValue(expected[index], array.Elements[index]);
                break;
            case JsonValueKind.String:
                Assert.Equal(expected.GetString(), Assert.IsType<JsonStringSyntax>(actual).Value);
                break;
            case JsonValueKind.Number:
                Assert.Equal(expected.TryGetInt32(out var integer) ? integer : (int?)null, Assert.IsType<JsonNumberSyntax>(actual).Int32Value);
                break;
            case JsonValueKind.True or JsonValueKind.False:
                Assert.Equal(expected.GetBoolean(), Assert.IsType<JsonBooleanSyntax>(actual).Value);
                break;
            case JsonValueKind.Null:
                Assert.IsType<JsonNullSyntax>(actual);
                break;
        }
    }

    [Theory]
    [InlineData("""{"record":{"name":"Value","items":[-12.5e-2,true,null,{"ok":false}]}}""")]
    [InlineData("""["letter\u0041","backslash\\","quote\"",0,-0,0.01,1e+3,{},[]]""")]
    public void Single_character_edits_match_system_text_json_acceptance(string json)
    {
        const string Replacements = "{}[],:0.-+e\"\\ !$";
        for (var index = 0; index < json.Length; index++)
        {
            var withoutCharacter = json.Remove(index, 1);
            AssertSameAcceptance(withoutCharacter);
            foreach (var replacement in Replacements)
            {
                AssertSameAcceptance(json.Insert(index, replacement.ToString()));
                if (replacement != json[index])
                    AssertSameAcceptance(withoutCharacter.Insert(index, replacement.ToString()));
            }
        }
    }

    private static void AssertSameAcceptance(string json)
    {
        var expected = AcceptsJson(json);
        var source = new SourceText("test.avsc", json);
        var tree = Parser.ParseJson(source, TestContext.Current.CancellationToken);
        Assert.True(expected == tree.Diagnostics.IsEmpty,
            $"System.Text.Json accepted: {expected}. Input: {json}{Environment.NewLine}{string.Join(Environment.NewLine, tree.Diagnostics.Select(diagnostic => diagnostic.GetMessage()))}");
    }

    private static bool AcceptsJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    [Theory]
    [InlineData("""{"record":{"name": """, "")]
    [InlineData("""{"type" "record", "fields": []}""", "\"record\"")]
    [InlineData("""{"fields":[1,2}""", "}")]
    [InlineData("""{"name":"unfinished""", "\"unfinished")]
    [InlineData("""{"name":"invalid\q","fields":[]}""", "\\q")]
    public void A_single_editing_mistake_reports_one_diagnostic_at_the_problem(string json, string problem)
    {
        var source = new SourceText("test.avsc", json);
        var tree = Parser.ParseJson(source, TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(tree.Diagnostics);
        var offset = problem.Length == 0 ? json.Length : json.IndexOf(problem, StringComparison.Ordinal);
        Assert.Equal(source.GetSourceSpan(offset, problem.Length), diagnostic.SourceSpan);
    }
}
