using System.Collections.Immutable;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Inputs;
using AvroSourceGenerator.Output;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Templating;

namespace AvroSourceGenerator.Tests;

public sealed class SchemaProjectTests
{
    [Fact]
    public void Records_ownership_and_direct_and_transitive_dependencies()
    {
        var output = Generate(
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            ("address.avsc", Record("Address")),
            ("customer.avsc", Record("Customer", Field("Address", "Address"))),
            ("order.avsc", Record("Order", Field("Customer", "Customer"))),
            ("unrelated.avsc", Record("Unrelated")));

        Assert.Empty(output.Diagnostics);
        Assert.Equal(4, output.Schemas.Length);
        Assert.Equal(
            [
                new OwnedSchema(Name("Address"), "address.avsc", true),
                new OwnedSchema(Name("Customer"), "customer.avsc", true),
                new OwnedSchema(Name("Order"), "order.avsc", true),
                new OwnedSchema(Name("Unrelated"), "unrelated.avsc", true),
            ],
            output.Project.Schemas);
        Assert.Equal(
            [
                new SchemaDependency(Name("Customer"), Name("Address")),
                new SchemaDependency(Name("Order"), Name("Customer")),
            ],
            output.Project.Dependencies);
        Assert.Equal([Name("Address")], output.Project.Files[0].Exports);
        Assert.Equal([Name("Address")], output.Project.ForwardDependencies[Name("Customer")]);
        Assert.Equal([Name("Customer")], output.Project.ReverseDependencies[Name("Address")]);
        Assert.False(output.Project.ForwardDependencies.ContainsKey(Name("Unrelated")));
    }

    [Fact]
    public void Records_self_dependencies_for_recursive_schemas()
    {
        var output = Generate(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("node.avsc", Record("Node", Field("Next", "Node"))));

        Assert.Empty(output.Diagnostics);
        Assert.Equal([new SchemaDependency(Name("Node"), Name("Node"))], output.Project.Dependencies);
        Assert.Equal([Name("Node")], output.Project.ForwardDependencies[Name("Node")]);
        Assert.Equal([Name("Node")], output.Project.ReverseDependencies[Name("Node")]);
    }

    [Fact]
    public void Records_deferred_missing_references_with_the_consuming_schema()
    {
        var output = Generate(
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            ("consumer.avsc", Record("Consumer", Field("Missing", "Missing"))));

        Assert.Empty(output.Schemas);
        Assert.Equal(["AVROSG0006"], output.Diagnostics.Select(static diagnostic => diagnostic.Descriptor.Id));
        Assert.Equal(
            [new SchemaDependency(Name("Consumer"), Name("Missing"))],
            output.Project.Dependencies);
        Assert.Equal(
            [new MissingSchemaReference("consumer.avsc", Name("Consumer"), Name("Missing"))],
            output.Project.MissingReferences);
    }

    [Fact]
    public void Records_strict_missing_references_at_the_source_file()
    {
        var output = Generate(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("consumer.avsc", Record("Consumer", Field("Missing", "Missing"))));

        Assert.Empty(output.Schemas);
        Assert.Equal(["AVROSG0006"], output.Diagnostics.Select(static diagnostic => diagnostic.Descriptor.Id));
        Assert.Empty(output.Project.Dependencies);
        Assert.Equal(
            [new MissingSchemaReference("consumer.avsc", Schema: null, Name("Missing"))],
            output.Project.MissingReferences);
    }

    [Theory]
    [InlineData(DuplicateResolution.Error, false, 1)]
    [InlineData(DuplicateResolution.Ignore, true, 0)]
    public void Keeps_first_owner_and_records_duplicate_attempts(
        DuplicateResolution duplicateResolution,
        bool isIgnored,
        int diagnosticCount)
    {
        var output = Generate(
            ReferenceResolution.Strict,
            duplicateResolution,
            ("first.avsc", Record("Shared")),
            ("second.avsc", Record("Shared")));

        Assert.Single(output.Schemas);
        Assert.Equal(diagnosticCount, output.Diagnostics.Length);
        Assert.All(output.Diagnostics, diagnostic => Assert.Equal("AVROSG0005", diagnostic.Descriptor.Id));
        Assert.Equal([new OwnedSchema(Name("Shared"), "first.avsc", true)], output.Project.Schemas);
        Assert.Equal([Name("Shared")], output.Project.Files[0].Exports);
        Assert.Empty(output.Project.Files[1].Exports);
        Assert.Equal(
            [new DuplicateSchemaDefinition(Name("Shared"), "first.avsc", "second.avsc", isIgnored)],
            output.Project.Duplicates);
    }

    [Fact]
    public void Rejects_same_file_duplicates_even_when_duplicates_are_ignored()
    {
        var output = Generate(
            ReferenceResolution.Strict,
            DuplicateResolution.Ignore,
            ("duplicates.avsc", Record(
                "Container",
                Field("First", Record("Shared"), rawType: true),
                Field("Second", Record("Shared"), rawType: true))));

        Assert.Equal(["AVROSG0005"], output.Diagnostics.Select(static diagnostic => diagnostic.Descriptor.Id));
        Assert.Equal([Name("Shared")], output.Project.Files[0].Exports);
        Assert.Equal(
            [new DuplicateSchemaDefinition(Name("Shared"), "duplicates.avsc", "duplicates.avsc", IsIgnored: false)],
            output.Project.Duplicates);
    }

    [Fact]
    public void Records_nested_and_protocol_exports_and_dependencies()
    {
        var output = Generate(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("nested.avsc", Record("Outer", Field("Inner", Record("Inner"), rawType: true))),
            ("rpc.avpr", Protocol()));

        Assert.Empty(output.Diagnostics);
        Assert.Equal(
            [Name("Inner"), Name("Outer")],
            output.Project.Files[0].Exports);
        Assert.Equal(
            [Name("Request"), Name("Rpc")],
            output.Project.Files[1].Exports);
        Assert.Contains(new SchemaDependency(Name("Outer"), Name("Inner")), output.Project.Dependencies);
        Assert.Contains(new SchemaDependency(Name("Rpc"), Name("Request")), output.Project.Dependencies);
    }

    [Fact]
    public void Records_variant_dependencies_used_by_generated_property_types()
    {
        var output = Generate(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("choice.avsc", Record(
                "Envelope",
                Field("Choice", $"[{Record("First")},{Record("Second")}]", rawType: true))));

        Assert.Empty(output.Diagnostics);
        var variant = Name("IEnvelopeChoiceVariant");
        Assert.Contains(new SchemaDependency(Name("Envelope"), variant), output.Project.Dependencies);
        Assert.Contains(new SchemaDependency(variant, Name("First")), output.Project.Dependencies);
        Assert.Contains(new SchemaDependency(variant, Name("Second")), output.Project.Dependencies);
        Assert.Contains(new SchemaDependency(Name("First"), variant), output.Project.Dependencies);
        Assert.Contains(new SchemaDependency(Name("Second"), variant), output.Project.Dependencies);
    }

    [Fact]
    public void Records_avdl_ownership_and_deferred_import_dependencies()
    {
        const string Common = """
            namespace GraphTests;
            schema Common;

            record Common { }
            """;
        const string Consumer = """
            namespace GraphTests;
            import idl "common.avdl";
            schema Consumer;

            record Consumer {
                Common common;
            }
            """;

        var output = Generate(
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            ("common.avdl", Common),
            ("consumer.avdl", Consumer));

        Assert.Empty(output.Diagnostics);
        Assert.Equal([Name("Common")], output.Project.Files[0].Exports);
        Assert.Equal([Name("Consumer")], output.Project.Files[1].Exports);
        Assert.Equal(
            [new SchemaDependency(Name("Consumer"), Name("Common"))],
            output.Project.Dependencies);
    }

    [Fact]
    public void Has_value_equality_for_equivalent_projects()
    {
        var first = Generate(
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            ("one.avsc", Record("One")),
            ("two.avsc", Record("Two", Field("One", "One"))));
        var second = Generate(
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            ("one.avsc", Record("One")),
            ("two.avsc", Record("Two", Field("One", "One"))));

        Assert.Equal(first.Project, second.Project);
        Assert.Equal(first.Project.GetHashCode(), second.Project.GetHashCode());
    }

    private static global::AvroSourceGenerator.Output.GeneratorOutput Generate(
        ReferenceResolution referenceResolution,
        DuplicateResolution duplicateResolution,
        params (string Path, string Text)[] sources)
    {
        ImmutableArray<IAvroFile> files = [.. sources.Select(source => AvroFile.FromFileText(source.Path, source.Text))];
        var config = new GeneratorConfig(
            TargetProfile.Modern,
            LanguageFeatures.Latest,
            AccessModifier.Public,
            referenceResolution,
            duplicateResolution,
            Diagnostics: []);
        return global::AvroSourceGenerator.Output.GeneratorOutput.FromInput((files, config), TestContext.Current.CancellationToken);
    }

    private static SchemaName Name(string name) => new(name, "GraphTests");

    private static string Record(string name, params string[] fields) => $$"""
        {
          "type": "record",
          "namespace": "GraphTests",
          "name": "{{name}}",
          "fields": [{{string.Join(",", fields)}}]
        }
        """;

    private static string Field(string name, string type, bool rawType = false) => $$"""
        {"name":"{{name}}","type":{{(rawType ? type : JsonValue.Create(type)!.ToJsonString())}}}
        """;

    private static string Protocol() => """
        {
          "protocol": "Rpc",
          "namespace": "GraphTests",
          "types": [
            {"type":"record","name":"Request","fields":[]}
          ],
          "messages": {
            "Send": {"request":[],"response":"Request"}
          }
        }
        """;
}
