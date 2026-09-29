using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests;

public sealed class AvroCompilationTests
{
    [Theory]
    [InlineData("schema.txt", "{}")]
    [InlineData("schema", "{}")]
    [InlineData("schema.bin", "")]
    public void Core_pipeline_reports_unsupported_extensions(string path, string text)
    {
        var compilation = AvroCompiler.Compile(
            [new SourceText(path, text)],
            new AvroParseOptions(GenerationTarget.Modern, true),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(AvroDiagnosticCode.UnsupportedSourceType, Assert.Single(compilation.Diagnostics).Code);
        Assert.False(compilation.IsValid);
    }

    [Theory]
    [InlineData(ReferenceResolution.Strict)]
    [InlineData(ReferenceResolution.Deferred)]
    public void Invalid_transitive_imports_preserve_independent_errors(ReferenceResolution resolution)
    {
        var compiled = Compile(
            resolution,
            DuplicateResolution.Error,
            ("root.avdl", """
                namespace GraphTests;
                import idl "middle.avdl";
                schema Root;
                record Root { Missing missing; }
                """),
            ("middle.avdl", """
                namespace GraphTests;
                import schema "broken.avsc";
                import schema "absent.avsc";
                schema Middle;
                record Middle { Missing missing; }
                """),
            ("broken.avsc", "{"),
            ("independent.avsc", Record("Independent", Field("Other", "Other"))));
        var diagnostics = compiled.Compilation.Diagnostics;
        Assert.Equal(3, diagnostics.Length);
        Assert.Single(diagnostics, diagnostic => diagnostic.Code == AvroDiagnosticCode.InvalidJson);
        Assert.Contains(
            diagnostics,
            diagnostic =>
                diagnostic.Code == AvroDiagnosticCode.MissingImport && diagnostic.GetMessage().Contains("absent.avsc"));
        Assert.Contains(
            diagnostics,
            diagnostic =>
                diagnostic.Code == AvroDiagnosticCode.MissingReferences && diagnostic.SourceSpan.SourceText.Path.OriginalPath == "independent.avsc");
    }

    [Fact]
    public void Repeated_diamond_imports_reuse_completed_closures()
    {
        var compiled = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("consumer.avdl", """
                namespace GraphTests;
                import idl "left.avdl";
                import idl "right.avdl";
                import idl "left.avdl";
                schema Consumer;
                record Consumer { Common common; Left left; Right right; }
                """),
            ("left.avdl", """
                namespace GraphTests;
                import schema "common.avsc";
                schema Left;
                record Left { Common common; }
                """),
            ("right.avdl", """
                namespace GraphTests;
                import schema "common.avsc";
                schema Right;
                record Right { Common common; }
                """),
            ("common.avsc", Record("Common")));

        Assert.Empty(compiled.Compilation.Diagnostics);
        Assert.True(compiled.Compilation.IsValid);
        Assert.Equal(4, compiled.RenderableFiles[0].ProjectSchemas.Count);
        Assert.All(compiled.RenderableFiles, file => Assert.Single(file.EmittedSchemas));
    }

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

        Assert.Empty(compiled.Compilation.Diagnostics);
        Assert.Equal(
            [Name("Address"), Name("Customer"), Name("Order"), Name("Unrelated")],
            compiled.RenderableFiles[2].ProjectSchemas.Keys.OrderBy(static name => name.FullName));
        Assert.Same(
            compiled.RenderableFiles[2].ProjectSchemas,
            compiled.RenderableFiles[3].ProjectSchemas);
    }

    [Fact]
    public void Contributing_files_follow_direct_and_transitive_file_dependencies()
    {
        var compiled = Compile(
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            ("a.avsc", Record("A", Field("B", "B"), Field("C", "C"))),
            ("b.avsc", Record("B", Field("D", "D"))),
            ("c.avsc", Record("C")),
            ("d.avsc", Record("D")));

        Assert.Empty(compiled.Compilation.Diagnostics);
        Assert.Equal(
            ["a.avsc", "b.avsc", "c.avsc", "d.avsc"],
            compiled.Compilation.GetContributingFiles(compiled.BoundFiles[0], TestContext.Current.CancellationToken)
                .Select(file => file.Path.OriginalPath));
    }

    [Fact]
    public void Handles_recursive_dependencies()
    {
        var compiled = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("node.avsc", Record("Node", Field("Next", "Node"))));

        Assert.Empty(compiled.Compilation.Diagnostics);
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

        Assert.False(compiled.Compilation.IsValid);
        Assert.Equal(["AVROSG0005"], compiled.Compilation.Diagnostics.Select(static diagnostic => diagnostic.ToDiagnostic().Id));
        Assert.Empty(compiled.RenderableFiles[0].EmittedSchemas);
    }

    [Fact]
    public void Failed_schema_suppresses_transitive_references_but_not_independent_files()
    {
        var compiled = Compile(
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            ("broken.avsc", Record("Broken", Field("Missing", "Missing"))),
            ("middle.avsc", Record("Middle", Field("Broken", "Broken"))),
            ("consumer.avsc", Record("Consumer", Field("Middle", "Middle"))),
            ("independent.avsc", Record("Independent")));

        Assert.False(compiled.Compilation.IsValid);
        Assert.Equal(["AVROSG0005"], compiled.Compilation.Diagnostics.Select(static diagnostic => diagnostic.ToDiagnostic().Id));
        Assert.False(compiled.Compilation.IsFileValid(compiled.BoundFiles[0], TestContext.Current.CancellationToken));
        Assert.All(compiled.BoundFiles.Skip(1).Take(2), file => Assert.True(compiled.Compilation.IsFileValid(file, TestContext.Current.CancellationToken)));
        Assert.All(compiled.RenderableFiles.Take(3), file => Assert.Empty(file.EmittedSchemas));
        Assert.True(compiled.Compilation.IsFileValid(compiled.BoundFiles[3], TestContext.Current.CancellationToken));
        Assert.Equal([Name("Independent")], compiled.RenderableFiles[3].EmittedSchemas.Select(static schema => schema.SchemaName));
    }

    [Fact]
    public void Unused_import_does_not_make_the_importer_depend_on_its_target()
    {
        var compiled = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("broken.avsc", Record("Broken", Field("Missing", "Missing"))),
            ("importer.avdl", """
                namespace GraphTests;
                import schema "broken.avsc";
                schema Importer;
                record Importer { }
                """),
            ("independent.avsc", Record("Independent")));

        Assert.Equal(["AVROSG5004", "AVROSG0005"], compiled.Compilation.Diagnostics.Select(static diagnostic => diagnostic.ToDiagnostic().Id));
        Assert.True(compiled.Compilation.IsFileValid(compiled.BoundFiles[1], TestContext.Current.CancellationToken));
        Assert.Equal(
            ["importer.avdl"],
            compiled.Compilation.GetContributingFiles(compiled.BoundFiles[1], TestContext.Current.CancellationToken)
                .Select(file => file.Path.OriginalPath));
        Assert.Single(compiled.RenderableFiles[1].EmittedSchemas);
        Assert.True(compiled.Compilation.IsFileValid(compiled.BoundFiles[2], TestContext.Current.CancellationToken));
        Assert.Single(compiled.RenderableFiles[2].EmittedSchemas);
    }

    [Fact]
    public void Malformed_file_does_not_suppress_independent_generation()
    {
        var compiled = Compile(
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            ("broken.avsc", "{"),
            ("independent.avsc", Record("Independent")));

        Assert.Equal(["AVROSG1000"], compiled.Compilation.Diagnostics.Select(static diagnostic => diagnostic.ToDiagnostic().Id));
        Assert.False(compiled.Compilation.IsFileValid(compiled.BoundFiles[0], TestContext.Current.CancellationToken));
        Assert.Empty(compiled.RenderableFiles[0].EmittedSchemas);
        Assert.True(compiled.Compilation.IsFileValid(compiled.BoundFiles[1], TestContext.Current.CancellationToken));
        Assert.Single(compiled.RenderableFiles[1].EmittedSchemas);
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

        Assert.Equal(diagnosticCount, compiled.Compilation.Diagnostics.Length);
        Assert.Equal(canRender, compiled.Compilation.IsValid);
        if (canRender)
        {
            Assert.Equal([Name("Shared")], compiled.RenderableFiles[0].EmittedSchemas.Select(static schema => schema.SchemaName));
            Assert.Empty(compiled.RenderableFiles[1].EmittedSchemas);
        }
        else
        {
            Assert.True(compiled.Compilation.IsFileValid(compiled.BoundFiles[0], TestContext.Current.CancellationToken));
            Assert.False(compiled.Compilation.IsFileValid(compiled.BoundFiles[1], TestContext.Current.CancellationToken));
            Assert.Single(compiled.RenderableFiles[0].EmittedSchemas);
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

        Assert.False(compiled.Compilation.IsValid);
        Assert.Equal(["AVROSG0004"], compiled.Compilation.Diagnostics.Select(static diagnostic => diagnostic.ToDiagnostic().Id));
        Assert.False(compiled.Compilation.IsFileValid(compiled.BoundFiles[0], TestContext.Current.CancellationToken));
        Assert.Empty(compiled.RenderableFiles[0].EmittedSchemas);
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

        Assert.Empty(nested.Compilation.Diagnostics);
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

        Assert.Empty(compiled.Compilation.Diagnostics);
        Assert.Equal(
            [Name("Common"), Name("Consumer")],
            compiled.RenderableFiles[1].ProjectSchemas.Keys.OrderBy(static name => name.FullName));
    }

    [Fact]
    public void Strict_import_resolves_a_reference_from_the_imported_file()
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
            ("common.avdl", """
                namespace GraphTests;
                schema Common;
                record Common { }
                """));

        Assert.Empty(compiled.Compilation.Diagnostics);
        Assert.True(compiled.Compilation.IsValid);
    }

    [Fact]
    public void Strict_imports_avsc_and_avpr_types()
    {
        var avsc = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("consumer.avdl", """
                namespace GraphTests;
                import schema "common.avsc";
                schema Consumer;
                record Consumer { Common common; }
                """),
            ("common.avsc", Record("Common")));
        var avpr = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("consumer.avdl", """
                namespace GraphTests;
                protocol Consumer {
                    import protocol "common.avpr";
                    Request get();
                }
                """),
            ("common.avpr", Protocol()));

        Assert.Empty(avsc.Compilation.Diagnostics);
        Assert.Empty(avpr.Compilation.Diagnostics);
    }

    [Fact]
    public void Strict_uses_the_transitive_relative_import_closure()
    {
        var compiled = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("schemas/consumer.avdl", """
                namespace GraphTests;
                import idl "imports/middle.avdl";
                schema Consumer;
                record Consumer { Common common; }
                """),
            ("schemas/imports/middle.avdl", """
                namespace GraphTests;
                import schema "../shared/common.avsc";
                schema Common;
                """),
            ("schemas/shared/common.avsc", Record("Common")));

        Assert.Empty(compiled.Compilation.Diagnostics);
        Assert.True(compiled.Compilation.IsValid);
    }

    [Fact]
    public void Missing_import_reports_one_import_diagnostic_without_reference_noise()
    {
        var compiled = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("consumer.avdl", """
                namespace GraphTests;
                import idl "missing.avdl";
                schema Consumer;
                record Consumer { Missing missing; }
                """),
            ("independent.avsc", Record("Independent")));

        Assert.Equal(["AVROSG5002"], compiled.Compilation.Diagnostics.Select(static diagnostic => diagnostic.ToDiagnostic().Id));
        Assert.False(compiled.Compilation.IsValid);
        Assert.False(compiled.Compilation.IsFileValid(compiled.BoundFiles[0], TestContext.Current.CancellationToken));
        Assert.True(compiled.Compilation.IsFileValid(compiled.BoundFiles[1], TestContext.Current.CancellationToken));
        Assert.Single(compiled.RenderableFiles[1].EmittedSchemas);
    }

    [Theory]
    [InlineData(ReferenceResolution.Strict)]
    [InlineData(ReferenceResolution.Deferred)]
    public void Unused_import_kind_mismatch_warns_and_renders(ReferenceResolution resolution)
    {
        var compiled = Compile(
            resolution,
            DuplicateResolution.Error,
            ("consumer.avdl", """
                import schema "common.avpr";
                schema Standalone;
                record Standalone { }
                """),
            ("common.avpr", Protocol()));

        Assert.Equal(["AVROSG5004"], compiled.Compilation.Diagnostics.Select(static diagnostic => diagnostic.ToDiagnostic().Id));
        Assert.True(compiled.Compilation.IsValid);
        Assert.Single(compiled.RenderableFiles[0].EmittedSchemas);
    }

    [Fact]
    public void Protocol_import_rejects_non_protocol_json()
    {
        var compiled = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("consumer.avdl", """
                import protocol "common.avpr";
                schema Standalone;
                record Standalone { }
                """),
            ("common.avpr", Record("Common")));

        Assert.Equal(["AVROSG2001", "AVROSG5004"], compiled.Compilation.Diagnostics.Select(static diagnostic => diagnostic.ToDiagnostic().Id));
        Assert.False(compiled.Compilation.IsValid);
    }

    [Fact]
    public void Malformed_imported_file_uses_its_parser_diagnostic_without_reference_noise()
    {
        var compiled = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("consumer.avdl", """
                namespace GraphTests;
                import schema "common.avsc";
                schema Consumer;
                record Consumer { Common common; }
                """),
            ("common.avsc", "not json"));

        Assert.Equal(["AVROSG1000"], compiled.Compilation.Diagnostics.Select(static diagnostic => diagnostic.ToDiagnostic().Id));
        Assert.False(compiled.Compilation.IsValid);
    }

    [Fact]
    public void Unused_import_cycle_warns_and_renders()
    {
        var compiled = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("a.avdl", """
                import idl "b.avdl";
                schema A;
                record A { }
                """),
            ("b.avdl", """
                import idl "a.avdl";
                schema B;
                record B { }
                """));

        Assert.Equal(["AVROSG5004", "AVROSG5004"], compiled.Compilation.Diagnostics.Select(static diagnostic => diagnostic.ToDiagnostic().Id));
        Assert.True(compiled.Compilation.IsValid);
        Assert.All(compiled.RenderableFiles, file => Assert.Single(file.EmittedSchemas));
    }

    [Fact]
    public void Import_cycle_is_reported_once_for_multiple_importers()
    {
        var compiled = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("first.avdl", """
                import idl "a.avdl";
                schema First;
                record First { Missing missing; }
                """),
            ("second.avdl", """
                import idl "b.avdl";
                schema Second;
                record Second { Missing missing; }
                """),
            ("a.avdl", """
                import idl "b.avdl";
                schema A;
                record A { }
                """),
            ("b.avdl", """
                import idl "a.avdl";
                schema B;
                record B { }
                """));

        Assert.Equal(["AVROSG5000"], compiled.Compilation.Diagnostics.Select(static diagnostic => diagnostic.ToDiagnostic().Id));
        Assert.False(compiled.Compilation.IsValid);
    }

    [Fact]
    public void Strict_import_uses_canonical_windows_path_identity()
    {
        var compiled = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            (@"C:\Project\idl\consumer.avdl", """
                namespace GraphTests;
                import schema "../schemas/./common.avsc";
                schema Consumer;
                record Consumer { Common common; }
                """),
            ("C:/Project/schemas/common.avsc", Record("Common")));

        Assert.Empty(compiled.Compilation.Diagnostics);
        Assert.True(compiled.Compilation.IsValid);
    }

    [Fact]
    public void Strict_import_uses_canonical_unix_path_identity()
    {
        var compiled = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("/project/idl/./consumer.avdl", """
                namespace GraphTests;
                import schema "../schemas\\common.avsc";
                schema Consumer;
                record Consumer { Common common; }
                """),
            ("/project/schemas/common.avsc", Record("Common")));

        Assert.Empty(compiled.Compilation.Diagnostics);
        Assert.True(compiled.Compilation.IsValid);
    }

    [Fact]
    public void Import_lookup_uses_host_platform_case_behavior()
    {
        var compiled = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("/project/consumer.avdl", """
                namespace GraphTests;
                import schema "common.avsc";
                schema Consumer;
                record Consumer { Common common; }
                """),
            ("/project/Common.avsc", Record("Common")));

        if (OperatingSystem.IsWindows())
        {
            Assert.Empty(compiled.Compilation.Diagnostics);
            Assert.True(compiled.Compilation.IsValid);
        }
        else
        {
            var diagnostic = Assert.Single(compiled.Compilation.Diagnostics);
            Assert.Equal(AvroDiagnosticCode.MissingImport, diagnostic.Code);
            Assert.Contains("common.avsc", diagnostic.GetMessage());
        }
    }

    [Fact]
    public void Canonical_duplicate_source_paths_are_invalid_and_preserve_display_paths()
    {
        var compiled = Compile(
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            (@"schemas\.\common.avsc", Record("First")),
            ("schemas/common.avsc", Record("Second")));

        var diagnostic = Assert.Single(compiled.Compilation.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.DuplicateSourcePath, diagnostic.Code);
        Assert.Equal("schemas/common.avsc", diagnostic.SourceSpan.SourceText.Path.OriginalPath);
        Assert.Contains(@"schemas\.\common.avsc", diagnostic.GetMessage());
        Assert.Contains("schemas/common.avsc", diagnostic.GetMessage());
    }

    [Fact]
    public void Duplicate_source_paths_use_host_platform_case_behavior()
    {
        var compiled = Compile(
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            (@"C:\Project\common.avsc", Record("First")),
            ("c:/project/COMMON.avsc", Record("Second")));

        if (OperatingSystem.IsWindows())
            Assert.Equal(AvroDiagnosticCode.DuplicateSourcePath, Assert.Single(compiled.Compilation.Diagnostics).Code);
        else
            Assert.Empty(compiled.Compilation.Diagnostics);
    }

    [Fact]
    public void Unix_rooted_source_paths_use_host_platform_case_behavior()
    {
        var compiled = Compile(
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            ("/project/common.avsc", Record("First")),
            ("/project/Common.avsc", Record("Second")));

        if (OperatingSystem.IsWindows())
            Assert.Equal(AvroDiagnosticCode.DuplicateSourcePath, Assert.Single(compiled.Compilation.Diagnostics).Code);
        else
            Assert.Empty(compiled.Compilation.Diagnostics);
    }

    [Fact]
    public void Cycle_detection_uses_canonical_windows_path_identity()
    {
        var compiled = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            (@"C:\Project\a.avdl", """
                import idl ".\\sub\\..\\B.avdl";
                schema A;
                record A { B b; }
                """),
            ("C:/Project/B.avdl", """
                import idl "./a.avdl";
                schema B;
                record B { A a; }
                """));

        var diagnostic = Assert.Single(compiled.Compilation.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.ImportCycle, diagnostic.Code);
        Assert.Contains(@"C:\Project\a.avdl", diagnostic.GetMessage());
        Assert.Contains("C:/Project/B.avdl", diagnostic.GetMessage());
    }

    [Fact]
    public void Strict_rejects_an_unimported_cross_file_reference()
    {
        var compiled = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Error,
            ("common.avsc", Record("Common")),
            ("consumer.avdl", """
                namespace GraphTests;
                schema Consumer;
                record Consumer { Common common; }
                """));

        Assert.Equal(["AVROSG0005"], compiled.Compilation.Diagnostics.Select(static diagnostic => diagnostic.ToDiagnostic().Id));
        Assert.False(compiled.Compilation.IsValid);
    }

    [Fact]
    public void Strict_checks_visibility_of_the_accepted_duplicate_owner()
    {
        var compiled = Compile(
            ReferenceResolution.Strict,
            DuplicateResolution.Ignore,
            ("unimported/common.avsc", Record("Common")),
            ("schemas/common.avsc", Record("Common")),
            ("schemas/consumer.avdl", """
                namespace GraphTests;
                import schema "common.avsc";
                schema Consumer;
                record Consumer { Common common; }
                """));

        Assert.Equal(["AVROSG5004", "AVROSG0005"], compiled.Compilation.Diagnostics.Select(static diagnostic => diagnostic.ToDiagnostic().Id));
        Assert.False(compiled.Compilation.IsValid);
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

        Assert.Empty(compiled.Compilation.Diagnostics);
        Assert.True(compiled.Compilation.IsValid);
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

        Assert.Equal(["AVROSG0005"], compiled.Compilation.Diagnostics.Select(static diagnostic => diagnostic.ToDiagnostic().Id));
        Assert.False(compiled.Compilation.IsValid);
    }

    [Fact]
    public void Deferred_symbolic_root_can_resolve_from_another_file()
    {
        var compiled = Compile(
            ReferenceResolution.Deferred,
            DuplicateResolution.Error,
            ("root.avsc", Record("Root")),
            ("reference.avsc", "\"GraphTests.Root\""));

        Assert.Empty(compiled.Compilation.Diagnostics);
        Assert.Single(compiled.RenderableFiles[0].EmittedSchemas);
        Assert.Empty(compiled.RenderableFiles[1].EmittedSchemas);
    }

    [Fact]
    public void Non_emitting_fixed_schemas_remain_available_in_the_schema_closure()
    {
        var compiled = SchemaCompilerTestHelpers.Compile(
            GenerationTarget.Modern,
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

        Assert.Equal(first.Compilation, second.Compilation);
        Assert.Equal(first.Compilation.GetHashCode(), second.Compilation.GetHashCode());
    }

    private static CompiledAvroSources Compile(
        ReferenceResolution referenceResolution,
        DuplicateResolution duplicateResolution,
        params (string Path, string Text)[] sources) =>
        SchemaCompilerTestHelpers.Compile(
            GenerationTarget.Modern,
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
        {"name":"{{name}}","type":{{(rawType ? type : JsonValue.Create(type).ToJsonString())}}}
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
