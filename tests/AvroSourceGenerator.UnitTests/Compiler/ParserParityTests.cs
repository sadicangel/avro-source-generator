using AvroSourceGenerator.Avdl;
using AvroSourceGenerator.Avdl.Declarations;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Compiler.Configuration;
using AvroSourceGenerator.Compiler.Parsing;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.UnitTests.Compiler;

public sealed class ParserParityTests
{
    [Theory]
    [InlineData(GenerationTarget.Modern, false)]
    [InlineData(GenerationTarget.Modern, true)]
    [InlineData(GenerationTarget.Apache, false)]
    [InlineData(GenerationTarget.Apache, true)]
    public void Failed_throws_unions_discard_state_and_suppress_dependent_validation(GenerationTarget target, bool oneWay)
    {
        var idl = $"protocol P {{ error E {{}} {(oneWay ? "string" : "void")} m(Missing p){(oneWay ? " oneway" : "")} throws E, E; }}";
        var json = $$$$"""{"protocol":"P","types":[{"type":"error","name":"E","fields":[]}],"messages":{"m":{"request":[{"name":"p","type":"Missing"}],"response":"{{{{(oneWay ? "string" : "null")}}}}","errors":["E","E"],"one-way":{{{{(oneWay ? "true" : "false")}}}}}}}""";

        foreach (var source in new[] { new SourceText("test.avdl", idl), new SourceText("test.avpr", json) })
        {
            var file = Parse(source, target);
            AssertInvalidState(file);
            var diagnostic = Assert.Single(file.GetDiagnostics());
            Assert.Equal(AvroDiagnosticCode.DuplicateUnionBranch, diagnostic.Code);
            var branch = source.Path.OriginalPath.EndsWith(".avdl", StringComparison.Ordinal) ? "E" : "\"E\"";
            Assert.Equal(source.GetSourceSpan(source.Text.LastIndexOf(branch, StringComparison.Ordinal), branch.Length), diagnostic.SourceSpan);
            Assert.Same(source, diagnostic.SourceSpan.SourceText);

            var bound = SchemaCompilerTestHelpers.Bind(file);
            Assert.False(bound.IsValid);
            Assert.Null(bound.RootSchema);
            Assert.Empty(bound.Declarations);
            Assert.Equal(file.GetDiagnostics(), bound.GetDiagnostics());
            var compilation = AvroCompiler.Compile([source], Options(target), cancellationToken: TestContext.Current.CancellationToken);
            Assert.False(compilation.IsValid);
            Assert.Empty(compilation.Schemas);
            Assert.Equal(diagnostic, Assert.Single(compilation.Diagnostics));
        }
    }

    [Theory]
    [InlineData(GenerationTarget.Modern, false)]
    [InlineData(GenerationTarget.Modern, true)]
    [InlineData(GenerationTarget.Apache, false)]
    [InlineData(GenerationTarget.Apache, true)]
    public void Absent_and_valid_throws_clauses_preserve_message_semantics(GenerationTarget target, bool declaredErrors)
    {
        var idl = $"protocol P {{ error E {{}} void m(){(declaredErrors ? " throws E" : " oneway")}; }}";
        var json = $$$$"""{"protocol":"P","types":[{"type":"error","name":"E","fields":[]}],"messages":{"m":{"request":[],"response":"null",{{{{(declaredErrors ? "\"errors\":[\"E\"]" : "\"one-way\":true")}}}}}}}""";
        var idlFile = Parse(new SourceText("test.avdl", idl), target);
        var jsonFile = Parse(new SourceText("test.avpr", json), target);

        AssertValid(idlFile);
        AssertValid(jsonFile);
        var message = Assert.Single(Assert.IsType<ProtocolSchema>(idlFile.RootSchema).Messages);
        if (declaredErrors)
            Assert.Equal([SchemaType.String, SchemaType.Reference], Assert.IsType<UnionSchema>(message.Errors).Schemas.Select(schema => schema.SchemaType));
        else
            Assert.Null(message.Errors);
        Assert.Equal(Serialize(jsonFile), Serialize(idlFile));
    }

    [Theory]
    [InlineData(GenerationTarget.Modern)]
    [InlineData(GenerationTarget.Apache)]
    public void Invalid_fixed_logical_annotations_report_format_specific_value_spans(GenerationTarget target)
    {
        const string Fixed = """{"type":"fixed","name":"F","size":16,"logicalType":1}""";
        var inputs = new[]
        {
            (new SourceText("test.avdl", "protocol P { @logicalType(1) fixed F(16); }"), AvroDiagnosticCode.InvalidIdlLogicalTypeAnnotation),
            (new SourceText("test.avdl", "schema F; @logicalType(1) fixed F(16);"), AvroDiagnosticCode.InvalidIdlLogicalTypeAnnotation),
            (new SourceText("test.avpr", $$$$"""{"protocol":"P","types":[{{{{Fixed}}}}],"messages":{}}"""), AvroDiagnosticCode.InvalidLogicalType),
            (new SourceText("test.avsc", Fixed), AvroDiagnosticCode.InvalidLogicalType)
        };
        foreach (var (source, code) in inputs)
        {
            var file = Parse(source, target);
            AssertInvalidState(file);
            var diagnostic = Assert.Single(file.GetDiagnostics());
            Assert.Equal(code, diagnostic.Code);
            var offset = source.Text.IndexOf("1", StringComparison.Ordinal);
            if (source.Path.OriginalPath.EndsWith(".avsc", StringComparison.Ordinal) || source.Path.OriginalPath.EndsWith(".avpr", StringComparison.Ordinal))
                offset = source.Text.LastIndexOf("1", StringComparison.Ordinal);
            Assert.Equal(source.GetSourceSpan(offset, 1), diagnostic.SourceSpan);
            Assert.Equal($"AVROSG{(int)code:D4}", diagnostic.ToDiagnostic().Id);
            Assert.Same(source, diagnostic.SourceSpan.SourceText);
            Assert.Equal(file.GetDiagnostics(), SchemaCompilerTestHelpers.Bind(file).GetDiagnostics());
        }
    }

    [Theory]
    [InlineData(GenerationTarget.Modern, "uuid")]
    [InlineData(GenerationTarget.Modern, "decimal")]
    [InlineData(GenerationTarget.Apache, "uuid")]
    [InlineData(GenerationTarget.Apache, "decimal")]
    public void Fixed_logical_declarations_preserve_storage_references_and_serialization(GenerationTarget target, string logicalType)
    {
        var decimalIdl = logicalType == "decimal" ? " @precision(4) @scale(2)" : "";
        var decimalJson = logicalType == "decimal" ? ",\"precision\":4,\"scale\":2" : "";
        var declaration = $$$$"""{"type":"fixed","name":"F","size":16,"logicalType":"{{{{logicalType}}}}","custom":1{{{{decimalJson}}}}}""";
        var idlDeclaration = $"@custom(1) @logicalType(\"{logicalType}\"){decimalIdl} fixed F(16);";
        var idlProtocol = Parse(new SourceText("test.avdl", $"@namespace(\"example\") protocol P {{ {idlDeclaration} record R {{ F f; }} F m(F p); }}"), target);
        var jsonProtocol = Parse(new SourceText("test.avpr", $$$$"""{"protocol":"P","namespace":"example","types":[{{{{declaration}}}},{"type":"record","name":"R","fields":[{"name":"f","type":"F"}]}],"messages":{"m":{"request":[{"name":"p","type":"F"}],"response":"F"}}}"""), target);
        var idlSchema = Parse(new SourceText("test.avdl", $"namespace example; schema F; {idlDeclaration}"), target);
        var jsonSchema = Parse(new SourceText("test.avsc", declaration.Replace("\"name\":\"F\"", "\"name\":\"F\",\"namespace\":\"example\"", StringComparison.Ordinal)), target);
        var files = new[] { idlProtocol, jsonProtocol, idlSchema, jsonSchema };
        var retainsLogicalType = target != GenerationTarget.Apache || logicalType != "uuid";

        foreach (var file in files)
        {
            AssertValid(file);
            var fixedSchema = Assert.IsType<FixedSchema>(file.Declarations[0]);
            Assert.Equal(new SchemaName("F", "example"), fixedSchema.SchemaName);
            Assert.Equal(1, fixedSchema.Properties["custom"].GetInt32());
            Assert.Equal(retainsLogicalType, fixedSchema.Properties.ContainsKey("logicalType"));
            if (retainsLogicalType) Assert.Equal(logicalType, fixedSchema.Properties["logicalType"].GetString());
            Assert.Same(file.Text, file.DeclarationSpans[0].SourceText);
            Assert.Contains(file.Text.Path.OriginalPath.EndsWith(".avdl", StringComparison.Ordinal) ? "fixed F(16);" : $"\"logicalType\":\"{logicalType}\"", file.DeclarationSpans[0].ToString(), StringComparison.Ordinal);
            if (file.RootSchema is ProtocolSchema protocol)
            {
                Assert.Same(fixedSchema, protocol.Types[0]);
                var spans = file.ReferenceSpans[fixedSchema.SchemaName];
                Assert.Equal(3, spans.Length);
                Assert.True(spans[0].Offset < spans[1].Offset && spans[1].Offset < spans[2].Offset);
                Assert.All(spans, span => Assert.Same(file.Text, span.SourceText));
                Assert.Equal([fixedSchema.SchemaName], file.Dependencies[new SchemaName("R", "example")]);
                Assert.Contains(fixedSchema.SchemaName, file.Dependencies[protocol.SchemaName]);
            }

            var bound = SchemaCompilerTestHelpers.Bind(file);
            Assert.True(bound.IsValid);
            Assert.Empty(bound.GetDiagnostics());
            Assert.Equal(retainsLogicalType, bound.Declarations[0].Properties.ContainsKey("logicalType"));
        }

        Assert.Equal(Serialize(jsonProtocol), Serialize(idlProtocol));
        var expectedFixed = jsonSchema.Declarations[0].ToJsonString(jsonSchema.Declarations.ToDictionary(schema => schema.SchemaName));
        Assert.All(files, file => Assert.Equal(expectedFixed, file.Declarations[0].ToJsonString(file.Declarations.ToDictionary(schema => schema.SchemaName))));
        Assert.Equal(SerializeBound(jsonProtocol), SerializeBound(idlProtocol));
    }

    [Theory]
    [InlineData(GenerationTarget.Modern)]
    [InlineData(GenerationTarget.Apache)]
    public void Logical_declaration_replacement_preserves_duplicate_occurrences(GenerationTarget target)
    {
        const string First = """{"type":"fixed","name":"F","size":16,"logicalType":"uuid","custom":1}""";
        const string Second = """{"type":"fixed","name":"F","size":16,"logicalType":"uuid","custom":2}""";
        var source = new SourceText("test.avpr", $$$$"""{"protocol":"P","types":[{{{{First}}}},{{{{Second}}}}],"messages":{}}""");
        var file = Parse(source, target);
        AssertValid(file);
        var protocol = Assert.IsType<ProtocolSchema>(file.RootSchema);
        Assert.Equal(2, protocol.Types.Length);
        for (var i = 0; i < protocol.Types.Length; i++)
        {
            Assert.Same(file.Declarations[i], protocol.Types[i]);
            Assert.Equal(i + 1, protocol.Types[i].Properties["custom"].GetInt32());
            Assert.Equal(target == GenerationTarget.Modern, protocol.Types[i].Properties.ContainsKey("logicalType"));
        }
        Assert.Equal([First, Second], file.DeclarationSpans.Take(2).Select(span => span.ToString()));
        var compilation = AvroCompiler.Compile([source], Options(target), cancellationToken: TestContext.Current.CancellationToken);
        var diagnostic = Assert.Single(compilation.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.DuplicateSchema, diagnostic.Code);
        Assert.Equal(file.DeclarationSpans[1], diagnostic.SourceSpan);
    }

    [Theory]
    [InlineData(GenerationTarget.Modern)]
    [InlineData(GenerationTarget.Apache)]
    public void Invalid_namespace_contents_report_the_annotation_value(GenerationTarget target)
    {
        foreach (var ns in new[] { "bad-name", "bad..name", "1bad", " " })
        {
            foreach (var protocolAnnotation in new[] { false, true })
            {
                var idl = protocolAnnotation
                    ? $"@namespace(\"{ns}\") protocol P {{ record R {{}} }}"
                    : $"protocol P {{ @namespace(\"{ns}\") record R {{}} }}";
                var json = protocolAnnotation
                    ? $$$$"""{"protocol":"P","namespace":"{{{{ns}}}}","types":[{"type":"record","name":"R","fields":[]}],"messages":{}}"""
                    : $$$$"""{"protocol":"P","types":[{"type":"record","name":"R","namespace":"{{{{ns}}}}","fields":[]}],"messages":{}}""";
                foreach (var source in new[] { new SourceText("test.avdl", idl), new SourceText("test.avpr", json) })
                {
                    var file = Parse(source, target);
                    AssertInvalidState(file);
                    var diagnostic = Assert.Single(file.GetDiagnostics());
                    Assert.Equal(source.Path.OriginalPath.EndsWith(".avdl", StringComparison.Ordinal) ? AvroDiagnosticCode.InvalidIdlNamespace : AvroDiagnosticCode.InvalidNamespace, diagnostic.Code);
                    var value = $"\"{ns}\"";
                    Assert.Equal(source.GetSourceSpan(source.Text.IndexOf(value, StringComparison.Ordinal), value.Length), diagnostic.SourceSpan);
                    Assert.Same(source, diagnostic.SourceSpan.SourceText);
                    Assert.Equal(file.GetDiagnostics(), SchemaCompilerTestHelpers.Bind(file).GetDiagnostics());
                }
            }
        }
    }

    [Theory]
    [InlineData(GenerationTarget.Modern)]
    [InlineData(GenerationTarget.Apache)]
    public void Namespace_validation_preserves_empty_strings_and_qualified_name_precedence(GenerationTarget target)
    {
        foreach (var ns in new[] { "\"\"", "\"bad-name\"", "null" })
        {
            var source = new SourceText("test.avdl", $"@namespace({ns}) protocol P {{ @namespace({ns}) record R {{}} }}");
            var syntax = Parser.Parse(source, TestContext.Current.CancellationToken);
            Assert.Empty(syntax.Diagnostics);
            var protocol = Assert.IsType<ProtocolDeclarationSyntax>(Assert.Single(syntax.Document.Declarations));
            var record = Assert.IsType<RecordDeclarationSyntax>(Assert.Single(protocol.Types));
            // The syntax parser accepts simple declaration names. Exercise qualified-name
            // precedence directly at the shared semantic name conversion boundary.
            protocol = protocol with { Name = new SimpleNameSyntax(protocol.Name.Identifier with { Value = "qualified.P" }) };
            record = record with { Name = new SimpleNameSyntax(record.Name.Identifier with { Value = "qualified.R" }) };
            Assert.Equal(new SchemaName("P", "qualified"), protocol.GetSchemaName("enclosing").GetValue());
            Assert.Equal(new SchemaName("R", "qualified"), record.GetSchemaName("enclosing").GetValue());
            var json = Parse(new SourceText("test.avpr", $$$$"""{"protocol":"qualified.P","namespace":{{{{ns}}}},"types":[{"type":"record","name":"qualified.R","namespace":{{{{ns}}}},"fields":[]}],"messages":{}}"""), target);
            AssertValid(json);
            Assert.Equal(new SchemaName("P", "qualified"), json.RootSchema.SchemaName);
            Assert.Equal(new SchemaName("R", "qualified"), json.Declarations[0].SchemaName);
        }

        var emptyNamespace = Parse(new SourceText("test.avdl", "namespace enclosing; @namespace(\"\") protocol P { @namespace(\"\") record R {} }"), target);
        AssertValid(emptyNamespace);
        Assert.Equal(string.Empty, emptyNamespace.RootSchema.SchemaName.Namespace);
        Assert.Equal(string.Empty, Assert.IsType<ProtocolSchema>(emptyNamespace.RootSchema).Types[0].SchemaName.Namespace);
    }

    [Theory]
    [InlineData(GenerationTarget.Modern)]
    [InlineData(GenerationTarget.Apache)]
    public void Annotated_logical_keywords_match_explicit_annotations_and_json(GenerationTarget target)
    {
        var cases = new[]
        {
            ("date", "date", "int", target == GenerationTarget.Modern ? CSharpName.DateOnly : CSharpName.DateTime),
            ("time_ms", "time-millis", "int", target == GenerationTarget.Modern ? CSharpName.TimeOnly : CSharpName.TimeSpan),
            ("timestamp_ms", "timestamp-millis", "long", target == GenerationTarget.Modern ? CSharpName.DateTimeOffset : CSharpName.DateTime),
            ("local_timestamp_ms", "local-timestamp-millis", "long", target == GenerationTarget.Modern ? CSharpName.DateTimeOffset : CSharpName.DateTime),
            ("uuid", "uuid", "string", CSharpName.Guid),
            ("decimal(9, 2)", "decimal", "bytes", target == GenerationTarget.Modern ? CSharpName.Decimal : CSharpName.AvroDecimal)
        };
        foreach (var (keyword, logicalType, underlying, csharpName) in cases)
        {
            var isDecimal = logicalType == "decimal";
            var annotations = isDecimal ? " @precision(9) @scale(2)" : "";
            var properties = isDecimal ? ",\"precision\":9,\"scale\":2" : "";
            var keywordFile = Parse(new SourceText("keyword.avdl", $"schema R; record R {{ @custom(1){(isDecimal ? " @precision(1) @scale(0)" : "")} {keyword} f; }}"), target);
            var annotatedFile = Parse(new SourceText("annotated.avdl", $"schema R; record R {{ @custom(1) @logicalType(\"{logicalType}\"){annotations} {underlying} f; }}"), target);
            var jsonFile = Parse(new SourceText("test.avsc", $$$$"""{"type":"record","name":"R","fields":[{"name":"f","type":{"type":"{{{{underlying}}}}","logicalType":"{{{{logicalType}}}}","custom":1{{{{properties}}}}}}]}"""), target);
            foreach (var file in new[] { keywordFile, annotatedFile, jsonFile })
            {
                AssertValid(file);
                var record = Assert.IsType<RecordSchema>(Assert.Single(file.Declarations));
                var type = Assert.IsType<LogicalSchema>(Assert.Single(record.Fields).Type);
                Assert.Equal(csharpName, type.CSharpName);
                Assert.Equal(logicalType, type.SchemaName.Name);
                Assert.Equal(1, type.Properties["custom"].GetInt32());
                Assert.Equal(1, type.UnderlyingSchema.Properties["custom"].GetInt32());
                if (isDecimal)
                {
                    Assert.Equal(9, type.Properties["precision"].GetInt32());
                    Assert.Equal(2, type.Properties["scale"].GetInt32());
                }
            }
            Assert.Equal(SerializeBound(jsonFile), SerializeBound(keywordFile));
            Assert.Equal(SerializeBound(jsonFile), SerializeBound(annotatedFile));
        }
    }

    private static AvroParseOptions Options(GenerationTarget target) => new AvroParseOptions(target, LanguageFeatures.NullableReferenceTypes);

    private static AvroFile Parse(SourceText source, GenerationTarget target) => AvxxParser.Parse(source, Options(target), TestContext.Current.CancellationToken);

    private static void AssertValid(AvroFile file)
    {
        Assert.True(file.IsValid, string.Join("; ", file.GetDiagnostics()));
        Assert.Empty(file.GetDiagnostics());
    }

    private static void AssertInvalidState(AvroFile file)
    {
        Assert.False(file.IsValid);
        Assert.Same(AvroSchema.Null, file.RootSchema);
        Assert.Empty(file.Declarations);
        Assert.Empty(file.DeclarationSpans);
        Assert.Empty(file.References);
        Assert.Empty(file.ReferenceSpans);
        Assert.Empty(file.Dependencies);
        Assert.Empty(file.Imports);
    }

    private static string Serialize(AvroFile file) => file.RootSchema.ToJsonString(file.Declarations.ToDictionary(schema => schema.SchemaName));

    private static string SerializeBound(AvroFile file)
    {
        var bound = SchemaCompilerTestHelpers.Bind(file);
        Assert.True(bound.IsValid, string.Join("; ", bound.GetDiagnostics()));
        Assert.Empty(bound.GetDiagnostics());
        Assert.NotNull(bound.RootSchema);
        return bound.RootSchema.ToJsonString(bound.Declarations.ToDictionary(schema => schema.SchemaName));
    }
}
