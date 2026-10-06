using Avro;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.UnitTests.Apache;

public sealed class VariantGenerationTests
{
    [Theory]
    [InlineData("record", "error", false, "class")]
    [InlineData("record", "error", true, "record")]
    [InlineData("error", "fixed", false, "class")]
    [InlineData("error", "fixed", true, "record")]
    [InlineData("fixed", "fixed", false, "class")]
    [InlineData("fixed", "fixed", true, "record")]
    public void Error_and_fixed_variants_compile_and_refresh_after_member_edits(
        string firstType, string secondType, bool nullable, string declaration) =>
        VariantGenerationAssertions.Verify(
            "Apache", [MetadataReference.CreateFromFile(typeof(Schema).Assembly.Location)],
            firstType, secondType, nullable, true, declaration);
}
