using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class AttributeLanguageFeaturesTests
{
    [Theory]
    [MemberData(nameof(LanguageFeaturesSchemaPairs))]
    public Task Verify(string languageFeatures, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With("fields", [new { type = "string", name = "Field" }]).ToString();

        var source = s_sources[schemaType].Replace("$languageFeatures$", languageFeatures);

        return TestHelper.VerifySourceCode(schema, source);
    }

    public static MatrixTheoryData<string, string> LanguageFeaturesSchemaPairs() => new(
        [.. Enum.GetNames<LanguageFeatures>().Where(n => n.StartsWith("CSharp"))],
        ["error", "fixed", "record", "protocol"]);

    private static readonly Dictionary<string, string> s_sources = new()
    {
        ["error"] = """
        using System;
        using AvroSourceGenerator;

        namespace SchemaNamespace;
        
        [Avro(LanguageFeatures = LanguageFeatures.$languageFeatures$)]
        public partial class Error;
        """,

        ["fixed"] = """
        using System;
        using AvroSourceGenerator;

        namespace SchemaNamespace;

        [Avro(LanguageFeatures = LanguageFeatures.$languageFeatures$)]
        public partial class Fixed;
        """,

        ["record"] = """
        using System;
        using AvroSourceGenerator;

        namespace SchemaNamespace;

        [Avro(LanguageFeatures = LanguageFeatures.$languageFeatures$)]
        public partial class Record;
        """,

        ["protocol"] = """
        using System;
        using AvroSourceGenerator;

        namespace SchemaNamespace;

        [Avro(LanguageFeatures = LanguageFeatures.$languageFeatures$)]
        public partial class RpcProtocol;
        """,
    };
}
