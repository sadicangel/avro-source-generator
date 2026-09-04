using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Templating;

namespace AvroSourceGenerator.Tests;

public sealed class RenderOptionsTests
{
    public static TheoryData<LanguageFeatures, string, string, string> ConfigurationDerivedExpressions => new()
    {
        { LanguageFeatures.None, "object", "fieldValue", "set" },
        { LanguageFeatures.NullableReferenceTypes, "object?", "fieldValue!", "set" },
        { LanguageFeatures.InitOnlyProperties, "object", "fieldValue", "init" },
        { LanguageFeatures.NullableReferenceTypes | LanguageFeatures.InitOnlyProperties, "object?", "fieldValue!", "init" },
    };

    [Theory]
    [MemberData(nameof(ConfigurationDerivedExpressions))]
    public void ConfigurationDerivedExpressions_MatchLanguageFeatures(
        LanguageFeatures languageFeatures,
        string objectType,
        string fieldValueExpression,
        string setter)
    {
        var options = new RenderOptions(TargetProfile.Modern, languageFeatures, AccessModifier.Public);

        Assert.Equal(objectType, options.ObjectType);
        Assert.Equal(fieldValueExpression, options.FieldValueExpression);
        Assert.Equal(setter, options.Setter);
    }
}
