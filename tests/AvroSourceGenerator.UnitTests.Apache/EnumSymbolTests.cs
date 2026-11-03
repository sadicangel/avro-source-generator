using Avro;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.UnitTests.Apache;

public sealed class EnumSymbolTests
{
    [Theory]
    [InlineData("class")]
    [InlineData("normal")]
    public void Apache_json_and_idl_compile_with_keyword_members(string defaultSymbol) =>
        EnumSymbolAssertions.Verify("Apache", [MetadataReference.CreateFromFile(typeof(Schema).Assembly.Location)], defaultSymbol);
}
