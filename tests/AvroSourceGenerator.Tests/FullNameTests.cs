namespace AvroSourceGenerator.Tests;

public sealed class FullNameTests
{
    [Fact]
    public Task Verify() => VerifySourceCode("""
    {
      "type": "record",
      "name": "Example",
      "doc": "A simple name (attribute) and no namespace attribute: use the null namespace (\"\"); the full name is 'Example'.",
      "fields": [
        {
          "name": "inheritNull",
          "type": {
            "type": "enum",
            "name": "Simple",
            "doc": "A simple name (attribute) and no namespace attribute: inherit the null namespace of the enclosing type 'Example'. The full name is 'Simple'.",
            "symbols": ["a", "b"]
          }
        }, {
          "name": "explicitNamespace",
          "type": {
            "type": "fixed",
            "name": "Simple",
            "namespace": "explicit",
            "doc": "A simple name (attribute) and a namespace (attribute); the full name is 'explicit.Simple' (this is a different type than of the 'inheritNull' field).",
            "size": 12
          }
        }, {
          "name": "fullName",
          "type": {
            "type": "record",
            "name": "a.full.Name",
            "namespace": "ignored",
            "doc": "A name attribute with a full name, so the namespace attribute is ignored. The full name is 'a.full.Name', and the namespace is 'a.full'.",
            "fields": [
              {
                "name": "inheritNamespace",
                "type": {
                  "type": "enum",
                  "name": "Understanding",
                  "doc": "A simple name (attribute) and no namespace attribute: inherit the namespace of the enclosing type 'a.full.Name'. The full name is 'a.full.Understanding'.",
                  "symbols": ["d", "e"]
                }
              }
            ]
          }
        }
      ]
    }
    """);

    [Theory]
    [InlineData("Example.")]
    [InlineData(".Name")]
    [InlineData("Example..Name")]
    public Task Diagnostic(string name) => VerifyDiagnostic($$"""
    {
      "type": "record",
      "name": "{{name}}",
      "fields": []
    }
    """);
}
