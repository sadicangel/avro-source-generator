using System.Diagnostics;
using System.Text.Json;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Templates;
using Scriban;
using Scriban.Functions;
using Scriban.Parsing;
using Scriban.Runtime;
using Xunit;

namespace AvroSourceGenerator.Tests;
public class ScribanTest
{
    private static TemplateContext CreateContext(
        AvroSchema rootSchema,
        string rootNamespace = "Tests",
        bool isRecordDeclaration = true,
        AccessModifier accessModifier = AccessModifier.Public,
        LanguageFeatures languageFeatures = LanguageFeatures.CSharp12)
    {
        var schemaRegistry = new SchemaRegistry2(rootSchema, languageFeatures.HasFlag(LanguageFeatures.NullableReferenceTypes));

        var avro = new ScriptObject();
        avro.Import("escape", TypeSymbol2.Escape);
        avro.Import("type", schemaRegistry.Type);
        avro.Import("array", static (AvroSchema schema) => schema.AsArraySchema());
        avro.Import("enum", static (AvroSchema schema) => schema.AsEnumSchema());
        avro.Import("error", static (AvroSchema schema) => schema.AsErrorSchema());
        avro.Import("fixed", static (AvroSchema schema) => schema.AsFixedSchema());
        avro.Import("logical", static (AvroSchema schema) => schema.AsLogicalSchema());
        avro.Import("map", static (AvroSchema schema) => schema.AsMapSchema());
        avro.Import("record", static (AvroSchema schema) => schema.AsRecordSchema());
        avro.Import("union", static (AvroSchema schema) => schema.AsUnionSchema());

        var builtin = new BuiltinFunctions();
        builtin.Import(new
        {
            avro,
            schemas = schemaRegistry,
            RootNamespace = rootNamespace,
            IsRecordDeclaration = isRecordDeclaration,
            AccessModifier = accessModifier switch
            {
                AccessModifier.Public => "public",
                AccessModifier.Private => "private",
                AccessModifier.Internal => "internal",
                AccessModifier.Protected => "protected",
                AccessModifier.ProtectedInternal => "protected internal",
                AccessModifier.PrivateProtected => "private protected",
                _ => throw new ArgumentOutOfRangeException(nameof(accessModifier)),
            },
            LanguageFeatures = languageFeatures,
            UseNullableReferenceTypes = (languageFeatures & LanguageFeatures.NullableReferenceTypes) != 0,
            UseFileScopedNamespaces = (languageFeatures & LanguageFeatures.FileScopedNamespaces) != 0,
            UseRequiredProperties = (languageFeatures & LanguageFeatures.RequiredProperties) != 0,
            UseInitOnlyProperties = (languageFeatures & LanguageFeatures.InitOnlyProperties) != 0,
            UseUnsafeAccessors = (languageFeatures & LanguageFeatures.UnsafeAccessors) != 0,
        },
        null,
        static member => member.Name);
        var context = new TemplateContext(builtin)
        {
            MemberRenamer = static member => member.Name,
            TemplateLoader = new TemplateLoader(@"D:\Development\avro-source-generator\src\AvroSourceGenerator\Templates\"),
        };
        return context;
    }

    [Fact]
    public void RecordTest()
    {
        using var jsonDocument = JsonDocument.Parse("""
        {
            "type": "record",
            "namespace": "Tests",
            "name": "User",
            "doc": "A user record",
            "aliases": ["Person", "Individual"],
            "fields": [
                { "name": "Name", "type": "string" },
                { "name": "Age", "type": "int", "default": 18 },
                { "name": "Description", "type": [ "string", "null" ] },
                { "name": "Suit", "type": { "type": "enum", "name": "Suit", "symbols": [ "SPADES", "HEARTS", "DIAMONDS", "CLUBS" ], "doc": "Suit enum", "aliases": ["CardSuit"], "default": "SPADES" }}
            ]
        }
        """);

        var context = CreateContext(rootSchema: new RecordSchema(jsonDocument.RootElement));

        var template = Template.Parse(File.ReadAllText(@"D:\Development\avro-source-generator\src\AvroSourceGenerator\Templates\main.sbncs"));
        var result = template.Render(context);
        Debug.WriteLine(result);
    }
}
public enum AccessModifier
{
    Public,
    Private,
    Internal,
    Protected,
    ProtectedInternal,
    PrivateProtected,
}

internal sealed class TemplateLoader(string directory) : ITemplateLoader
{
    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) =>
        Path.Combine(directory, $"{templateName}.sbncs");

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath) =>
        File.ReadAllText(templatePath);

    public async ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath) =>
#if NETFRAMEWORK
        await Task.FromResult(Load(context, callerSpan, templatePath));
#else
        await File.ReadAllTextAsync(templatePath);
#endif
}
