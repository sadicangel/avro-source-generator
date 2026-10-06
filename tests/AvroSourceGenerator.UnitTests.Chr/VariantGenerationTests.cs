using Chr.Avro.Abstract;
using Chr.Avro.Serialization;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.UnitTests.Chr;

public sealed class VariantGenerationTests
{
    [Theory]
    [InlineData("record", "error", false, true, "class")]
    [InlineData("record", "error", true, true, "record")]
    [InlineData("error", "error", false, true, "record")]
    [InlineData("error", "fixed", false, false, "class")]
    public void Error_variants_compile_and_substituted_fixed_members_use_object(
        string firstType, string secondType, bool nullable, bool supportsVariant, string declaration) =>
        VariantGenerationAssertions.Verify(
            "Chr",
            [MetadataReference.CreateFromFile(typeof(Schema).Assembly.Location), MetadataReference.CreateFromFile(typeof(IBinarySerializerBuilder).Assembly.Location)],
            firstType, secondType, nullable, supportsVariant, declaration);
}
