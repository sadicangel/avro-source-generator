using System.Runtime.CompilerServices;

namespace AvroSourceGenerator.Compiler;

public static class LanguageFeaturesExtensions
{
    extension(LanguageFeatures features)
    {
        public bool HasNullableReferenceTypes => features.Has(LanguageFeatures.NullableReferenceTypes);
        public LanguageFeatures SetNullableReferenceTypes(bool value) => features.Set(LanguageFeatures.NullableReferenceTypes, value);
        public bool HasRecords => features.Has(LanguageFeatures.Records);
        public LanguageFeatures SetRecords(bool value) => features.Set(LanguageFeatures.Records, value);
        public bool HasInitOnlyProperties => features.Has(LanguageFeatures.InitOnlyProperties);
        public LanguageFeatures SetInitOnlyProperties(bool value) => features.Set(LanguageFeatures.InitOnlyProperties, value);
        public bool HasRequiredProperties => features.Has(LanguageFeatures.RequiredProperties);
        public LanguageFeatures SetRequiredProperties(bool value) => features.Set(LanguageFeatures.RequiredProperties, value);
        public bool HasRawStringLiterals => features.Has(LanguageFeatures.RawStringLiterals);
        public LanguageFeatures SetRawStringLiterals(bool value) => features.Set(LanguageFeatures.RawStringLiterals, value);
        public bool HasUnsafeAccessors => features.Has(LanguageFeatures.UnsafeAccessors);
        public LanguageFeatures SetUnsafeAccessors(bool value) => features.Set(LanguageFeatures.UnsafeAccessors, value);
        public bool HasUnions => features.Has(LanguageFeatures.Unions);
        public LanguageFeatures SetUnions(bool value) => features.Set(LanguageFeatures.Unions, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Has(LanguageFeatures flag) => (features & flag) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LanguageFeatures Set(LanguageFeatures flag, bool value) => value ? features | flag : features & ~flag;
    }
}
