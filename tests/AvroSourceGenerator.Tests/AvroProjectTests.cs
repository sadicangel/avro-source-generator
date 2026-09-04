using System.Text.Json.Nodes;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Output;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Tests;

public sealed class AvroProjectTests
{
    [Fact]
    public void Reuses_the_project_schema_lookup_for_each_renderable_file()
    {
        var compiled = Compile(
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            ("address.avsc", Record("Address")),
            ("customer.avsc", Record("Customer", Field("Address", "Address"))),
            ("order.avsc", Record("Order", Field("Customer", "Customer"))),
            ("unrelated.avsc", Record("Unrelated")));

        Assert.Empty(compiled.Project.Diagnostics);
        Assert.Equal(
            [Name("Address"), Name("Customer"), Name("Order"), Name("Unrelated")],
            compiled.RenderableFiles[2].ProjectSchemas.Keys.OrderBy(static name => name.FullName));
        Assert.Same(
            compiled.RenderableFiles[2].ProjectSchemas,
            compiled.RenderableFiles[3].ProjectSchemas);
    }

    [Fact]
    public void Handles_recursive_dependencies()
    {
        var compiled = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("node.avsc", Record("Node", Field("Next", "Node"))));

        Assert.Empty(compiled.Project.Diagnostics);
        Assert.Equal([Name("Node")], compiled.RenderableFiles[0].ProjectSchemas.Keys);
    }

    [Theory]
    [InlineData(ReferenceResolution.Strict)]
    [InlineData(ReferenceResolution.Deferred)]
    public void Missing_references_disable_rendering(ReferenceResolution resolution)
    {
        var compiled = Compile(
            resolution,
            DuplicateResolution.Error,
            ("consumer.avsc", Record("Consumer", Field("Missing", "Missing"))));

        Assert.False(compiled.Project.CanRender);
        Assert.Equal(["AVROSG0006"], compiled.Project.Diagnostics.Select(static diagnostic => diagnostic.Descriptor.Id));
        Assert.Empty(compiled.RenderableFiles[0].EmittedSchemas);
    }

    [Theory]
    [InlineData(DuplicateResolution.Error, 1, false)]
    [InlineData(DuplicateResolution.Ignore, 0, true)]
    public void Keeps_the_first_cross_file_duplicate(
        DuplicateResolution resolution,
        int diagnosticCount,
        bool canRender)
    {
        var compiled = Compile(
            ReferenceResolution.Strict,
            resolution,
            ("first.avsc", Record("Shared")),
            ("second.avsc", Record("Shared")));

        Assert.Equal(diagnosticCount, compiled.Project.Diagnostics.Length);
        Assert.Equal(canRender, compiled.Project.CanRender);
        if (canRender)
        {
            Assert.Equal([Name("Shared")], compiled.RenderableFiles[0].EmittedSchemas.Select(static schema => schema.SchemaName));
            Assert.Empty(compiled.RenderableFiles[1].EmittedSchemas);
        }
    }

    [Fact]
    public void Same_file_duplicates_are_errors_even_when_cross_file_duplicates_are_ignored()
    {
        var compiled = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Ignore,
            ("duplicates.avsc", Record(
                "Container",
                Field("First", Record("Shared"), rawType: true),
                Field("Second", Record("Shared"), rawType: true))));

        Assert.False(compiled.Project.CanRender);
        Assert.Equal(["AVROSG0005"], compiled.Project.Diagnostics.Select(static diagnostic => diagnostic.Descriptor.Id));
    }

    [Fact]
    public void Includes_nested_protocol_and_variant_dependencies_in_render_closures()
    {
        var nested = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("nested.avsc", Record("Outer", Field("Inner", Record("Inner"), rawType: true))),
            ("rpc.avpr", Protocol()),
            ("choice.avsc", Record(
                "Envelope",
                Field("Choice", $"[{Record("First")},{Record("Second")}]", rawType: true))));

        Assert.Empty(nested.Project.Diagnostics);
        Assert.Contains(Name("Inner"), nested.RenderableFiles[0].ProjectSchemas.Keys);
        Assert.Contains(Name("Request"), nested.RenderableFiles[1].ProjectSchemas.Keys);
        Assert.Contains(Name("IEnvelopeChoiceVariant"), nested.RenderableFiles[2].ProjectSchemas.Keys);
        Assert.Contains(Name("First"), nested.RenderableFiles[2].ProjectSchemas.Keys);
        Assert.Contains(Name("Second"), nested.RenderableFiles[2].ProjectSchemas.Keys);
    }

    [Fact]
    public void Deferred_avdl_import_uses_the_project_schema_lookup()
    {
        var compiled = Compile(
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            ("common.avdl", """
                namespace GraphTests;
                schema Common;
                record Common { }
                """),
            ("consumer.avdl", """
                namespace GraphTests;
                import idl "common.avdl";
                schema Consumer;
                record Consumer { Common common; }
                """));

        Assert.Empty(compiled.Project.Diagnostics);
        Assert.Equal(
            [Name("Common"), Name("Consumer")],
            compiled.RenderableFiles[1].ProjectSchemas.Keys.OrderBy(static name => name.FullName));
    }

    [Fact]
    public void Strict_import_reports_one_unsupported_import_diagnostic_without_missing_reference_noise()
    {
        var compiled = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("consumer.avdl", """
                namespace GraphTests;
                import idl "common.avdl";
                schema Consumer;
                record Consumer { Common common; }
                """),
            ("unrelated.avsc", Record("Unrelated", Field("Missing", "Missing"))));

        Assert.Equal(["AVROSG1000"], compiled.Project.Diagnostics.Select(static diagnostic => diagnostic.Descriptor.Id));
        Assert.False(compiled.Project.CanRender);
    }

    [Fact]
    public void Deferred_supports_same_file_forward_references()
    {
        var compiled = Compile(
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            ("forward.avdl", """
                namespace GraphTests;
                schema First;
                record First { Second second; }
                record Second { }
                """));

        Assert.Empty(compiled.Project.Diagnostics);
        Assert.True(compiled.Project.CanRender);
        Assert.Equal(
            [Name("First"), Name("Second")],
            compiled.RenderableFiles[0].EmittedSchemas.Select(static schema => schema.SchemaName));
    }

    [Fact]
    public void Strict_rejects_same_file_forward_references()
    {
        var compiled = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("forward.avdl", """
                namespace GraphTests;
                schema First;
                record First { Second second; }
                record Second { }
                """));

        Assert.Equal(["AVROSG0006"], compiled.Project.Diagnostics.Select(static diagnostic => diagnostic.Descriptor.Id));
        Assert.False(compiled.Project.CanRender);
    }

    [Fact]
    public void Deferred_symbolic_root_can_resolve_from_another_file()
    {
        var compiled = Compile(
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            ("root.avsc", Record("Root")),
            ("reference.avsc", "\"GraphTests.Root\""));

        Assert.Empty(compiled.Project.Diagnostics);
        Assert.Single(compiled.RenderableFiles[0].EmittedSchemas);
        Assert.Empty(compiled.RenderableFiles[1].EmittedSchemas);
    }

    [Fact]
    public void Non_emitting_fixed_schemas_remain_available_in_the_schema_closure()
    {
        var compiled = SchemaCompilerTestHelpers.CompileProject(
            TargetProfile.Modern,
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            ("hash.avsc", """
                { "type": "fixed", "name": "Hash", "namespace": "GraphTests", "size": 16 }
                """),
            ("consumer.avsc", Record("Consumer", Field("Hash", "Hash"))));

        Assert.Empty(compiled.RenderableFiles[0].EmittedSchemas);
        Assert.Contains(Name("Hash"), compiled.RenderableFiles[1].ProjectSchemas.Keys);
    }

    [Fact]
    public void Has_value_equality_for_equivalent_projects()
    {
        var first = Compile(
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            ("one.avsc", Record("One")),
            ("two.avsc", Record("Two", Field("One", "One"))));
        var second = Compile(
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            ("one.avsc", Record("One")),
            ("two.avsc", Record("Two", Field("One", "One"))));

        Assert.Equal(first.Project, second.Project);
        Assert.Equal(first.Project.GetHashCode(), second.Project.GetHashCode());
    }

    private static CompiledAvroProject Compile(
        ReferenceResolution referenceResolution,
        DuplicateResolution duplicateResolution,
        params (string Path, string Text)[] sources) =>
        SchemaCompilerTestHelpers.CompileProject(
            TargetProfile.Modern,
            referenceResolution,
            duplicateResolution,
            sources);

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
