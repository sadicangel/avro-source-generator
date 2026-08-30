using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Templating;

namespace AvroSourceGenerator.Tests;

public sealed class TemplateSettingsTests
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
        var settings = new TemplateSettings(TargetProfile.Modern, languageFeatures, AccessModifier.Public);

        Assert.Equal(objectType, settings.ObjectType);
        Assert.Equal(fieldValueExpression, settings.FieldValueExpression);
        Assert.Equal(setter, settings.Setter);
    }
}
