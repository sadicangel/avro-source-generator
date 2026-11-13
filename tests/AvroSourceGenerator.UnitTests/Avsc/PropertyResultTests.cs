using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.UnitTests.Avsc;

public sealed class PropertyResultTests
{
    [Theory]
    [InlineData("""{"type":"enum","name":"E"}""", "symbols")]
    [InlineData("""{"type":"fixed","name":"F"}""", "size")]
    [InlineData("""{"type":"record","fields":[]}""", "name")]
    [InlineData("""{"type":"record","name":"R"}""", "fields")]
    [InlineData("""{"protocol":"P","messages":{}}""", "types", ".avpr")]
    [InlineData("""{"protocol":"P","types":[]}""", "messages", ".avpr")]
    public void Required_property_errors_bubble_without_replacement_or_duplicate_reporting(string text, string property, string extension = ".avsc")
    {
        var file = Parse(text, extension);

        AssertInvalid(file);
        var diagnostic = Assert.Single(file.GetDiagnostics());
        Assert.Equal(AvroDiagnosticCode.MissingSchemaProperty, diagnostic.Code);
        Assert.Equal(property, Assert.Single(diagnostic.Arguments));
        Assert.Equal(file.Text.GetSourceSpan(), diagnostic.SourceSpan);
    }

    [Fact]
    public void Record_gate_reports_metadata_and_child_errors_in_traversal_order()
    {
        var file = Parse("""{"type":"record","name":"R","doc":42,"aliases":false,"fields":[{"name":"f","type":0}]}""");

        AssertInvalid(file);
        Assert.Equal(
            [AvroDiagnosticCode.InvalidDoc, AvroDiagnosticCode.InvalidAliases, AvroDiagnosticCode.InvalidFieldType],
            file.GetDiagnostics().Select(diagnostic => diagnostic.Code));
        Assert.Equal(["42", "false", "0"], file.GetDiagnostics().Select(diagnostic => diagnostic.SourceSpan.ToString()));
    }

    [Fact]
    public void Enum_gate_checks_all_properties_after_independent_failures()
    {
        var file = Parse("""{"type":"enum","name":"E","doc":42,"aliases":false,"symbols":0,"default":true}""");

        AssertInvalid(file);
        Assert.Equal(
            [AvroDiagnosticCode.InvalidDoc, AvroDiagnosticCode.InvalidAliases, AvroDiagnosticCode.InvalidSymbols, AvroDiagnosticCode.InvalidEnumDefault],
            file.GetDiagnostics().Select(diagnostic => diagnostic.Code));
        Assert.Equal(["42", "false", "0", "true"], file.GetDiagnostics().Select(diagnostic => diagnostic.SourceSpan.ToString()));
    }

    [Fact]
    public void Field_gate_checks_metadata_and_type_even_when_the_name_is_invalid()
    {
        var file = Parse("""{"type":"record","name":"R","fields":[{"name":false,"type":true,"doc":0,"aliases":42}]}""");

        AssertInvalid(file);
        Assert.Equal(
            [AvroDiagnosticCode.InvalidFieldName, AvroDiagnosticCode.InvalidFieldType, AvroDiagnosticCode.InvalidDoc, AvroDiagnosticCode.InvalidAliases],
            file.GetDiagnostics().Select(diagnostic => diagnostic.Code));
        Assert.Equal(["false", "true", "0", "42"], file.GetDiagnostics().Select(diagnostic => diagnostic.SourceSpan.ToString()));
    }

    [Fact]
    public void Field_gate_reports_a_missing_type_once_after_an_invalid_name()
    {
        var file = Parse("""{"type":"record","name":"R","fields":[{"name":false},false]}""");

        AssertInvalid(file);
        Assert.Equal(
            [AvroDiagnosticCode.InvalidFieldName, AvroDiagnosticCode.MissingSchemaProperty],
            file.GetDiagnostics().Select(diagnostic => diagnostic.Code));
        Assert.Equal(["false", """{"name":false}"""], file.GetDiagnostics().Select(diagnostic => diagnostic.SourceSpan.ToString()));
        Assert.Equal("type", Assert.Single(Assert.Single(file.GetDiagnostics(), diagnostic => diagnostic.Code == AvroDiagnosticCode.MissingSchemaProperty).Arguments));
    }

    [Fact]
    public void Message_gate_reports_all_independent_property_errors_once()
    {
        var file = Parse(
            """{"protocol":"P","types":[],"messages":{"m":{"request":{},"response":false,"errors":0,"one-way":42,"doc":true}}}""",
            ".avpr");

        AssertInvalid(file);
        Assert.Equal(
            [AvroDiagnosticCode.InvalidRequest, AvroDiagnosticCode.InvalidResponse, AvroDiagnosticCode.InvalidErrors, AvroDiagnosticCode.InvalidOneWay, AvroDiagnosticCode.InvalidDoc],
            file.GetDiagnostics().Select(diagnostic => diagnostic.Code));
        Assert.Equal(["{}", "false", "0", "42", "true"], file.GetDiagnostics().Select(diagnostic => diagnostic.SourceSpan.ToString()));
    }

    [Fact]
    public void Union_gate_retains_all_duplicate_and_nested_branch_diagnostics()
    {
        var file = Parse("""["int","int",["null","string"],"string","string","int"]""");

        AssertInvalid(file);
        Assert.Equal(
            [AvroDiagnosticCode.DuplicateUnionBranch, AvroDiagnosticCode.NestedUnion, AvroDiagnosticCode.DuplicateUnionBranch, AvroDiagnosticCode.DuplicateUnionBranch],
            file.GetDiagnostics().Select(diagnostic => diagnostic.Code));
        Assert.Equal(
            ["\"int\"", """["null","string"]""", "\"string\"", "\"int\""],
            file.GetDiagnostics().Select(diagnostic => diagnostic.SourceSpan.ToString()));
        Assert.Equal(["int", "string", "int"], file.GetDiagnostics()
            .Where(diagnostic => diagnostic.Code == AvroDiagnosticCode.DuplicateUnionBranch)
            .Select(diagnostic => Assert.Single(diagnostic.Arguments)));
        Assert.True(file.GetDiagnostics().Select(diagnostic => diagnostic.SourceSpan.Offset).SequenceEqual(
            file.GetDiagnostics().Select(diagnostic => diagnostic.SourceSpan.Offset).Order()));
    }

    [Fact]
    public void Earlier_union_validation_errors_survive_a_later_invalid_branch()
    {
        var file = Parse("""["int","int",false]""");

        AssertInvalid(file);
        Assert.Equal([AvroDiagnosticCode.DuplicateUnionBranch, AvroDiagnosticCode.SchemaExpected], file.GetDiagnostics().Select(diagnostic => diagnostic.Code));
        Assert.Equal(["\"int\"", "false"], file.GetDiagnostics().Select(diagnostic => diagnostic.SourceSpan.ToString()));
    }

    [Fact]
    public void Invalid_union_branches_do_not_hide_later_duplicates()
    {
        var file = Parse("""["int","int",false,"int",true,["null","string"]]""");

        AssertInvalid(file);
        Assert.Equal(
            [AvroDiagnosticCode.DuplicateUnionBranch, AvroDiagnosticCode.SchemaExpected, AvroDiagnosticCode.DuplicateUnionBranch, AvroDiagnosticCode.SchemaExpected, AvroDiagnosticCode.NestedUnion],
            file.GetDiagnostics().Select(diagnostic => diagnostic.Code));
        Assert.Equal(
            ["\"int\"", "false", "\"int\"", "true", """["null","string"]"""],
            file.GetDiagnostics().Select(diagnostic => diagnostic.SourceSpan.ToString()));
    }

    [Fact]
    public void Enum_gate_reports_each_duplicate_before_its_invalid_default()
    {
        var file = Parse("""{"type":"enum","name":"E","symbols":["A","A","B","B","A"],"default":false}""");

        AssertInvalid(file);
        Assert.Equal(
            [AvroDiagnosticCode.DuplicateEnumSymbol, AvroDiagnosticCode.DuplicateEnumSymbol, AvroDiagnosticCode.DuplicateEnumSymbol, AvroDiagnosticCode.InvalidSymbols, AvroDiagnosticCode.InvalidEnumDefault],
            file.GetDiagnostics().Select(diagnostic => diagnostic.Code));
        Assert.Equal(["\"A\"", "\"B\"", "\"A\"", """["A","A","B","B","A"]""", "false"], file.GetDiagnostics().Select(diagnostic => diagnostic.SourceSpan.ToString()));
    }

    [Theory]
    [InlineData("false")]
    [InlineData("0")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("\"\"")]
    [InlineData("\" \"")]
    public void Invalid_field_order_reports_a_property_error_instead_of_throwing(string order)
    {
        var file = Parse($$"""{"type":"record","name":"R","fields":[{"name":"f","type":"string","order":{{order}}}]}""");

        AssertInvalid(file);
        Assert.Equal([AvroDiagnosticCode.InvalidOrder], file.GetDiagnostics().Select(diagnostic => diagnostic.Code));
        var diagnostic = file.GetDiagnostics().First();
        Assert.Equal(order, diagnostic.SourceSpan.ToString());
        Assert.Equal("Field property 'order' must be a non-empty string or null", diagnostic.GetMessage());
        Assert.Equal("AVROSG2036", diagnostic.ToDiagnostic().Id);
    }

    private static AvroFile Parse(string text, string extension = ".avsc") => AvxxParser.Parse(
        new SourceText("test" + extension, text),
        new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.NullableReferenceTypes),
        TestContext.Current.CancellationToken);

    private static void AssertInvalid(AvroFile file)
    {
        Assert.False(file.IsValid);
        Assert.Same(AvroSchema.Null, file.RootSchema);
        Assert.Empty(file.Declarations);
        Assert.Empty(file.DeclarationSpans);
        Assert.Empty(file.References);
        Assert.Empty(file.ReferenceSpans);
        Assert.Empty(file.Dependencies);
        Assert.All(file.GetDiagnostics(), diagnostic => Assert.Same(file.Text, diagnostic.SourceSpan.SourceText));
    }
}
