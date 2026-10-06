namespace AvroSourceGenerator.UnitTests;

public sealed class VariantGenerationTests
{
    [Theory]
    [InlineData("record", "error", false, true, "class")]
    [InlineData("record", "error", true, true, "record")]
    [InlineData("error", "error", false, true, "record")]
    [InlineData("error", "fixed", false, false, "class")]
    public void Error_variants_compile_and_substituted_fixed_members_use_object(
        string firstType, string secondType, bool nullable, bool supportsVariant, string declaration) =>
        VariantGenerationAssertions.Verify("None", [], firstType, secondType, nullable, supportsVariant, declaration);
}
