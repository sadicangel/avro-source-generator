using System.Runtime.CompilerServices;

namespace AvroSourceGenerator.Compiler.Configuration;

public static class LanguageFeaturesExtensions
{
    extension(LanguageFeatures features)
    {
        public bool HasNullableReferenceTypes => features.Has(LanguageFeatures.NullableReferenceTypes);

        public bool HasRecords => features.Has(LanguageFeatures.Records);

        public bool HasInitOnlyProperties => features.Has(LanguageFeatures.InitOnlyProperties);

        public bool HasRequiredProperties => features.Has(LanguageFeatures.RequiredProperties);

        public bool HasRawStringLiterals => features.Has(LanguageFeatures.RawStringLiterals);

        public bool HasUnsafeAccessors => features.Has(LanguageFeatures.UnsafeAccessors);

        public bool HasUnions => features.Has(LanguageFeatures.Unions);

        public LanguageFeatures SetNullableReferenceTypes(bool value) => features.Set(LanguageFeatures.NullableReferenceTypes, value);

        public LanguageFeatures SetRecords(bool value) => features.Set(LanguageFeatures.Records, value);

        public LanguageFeatures SetInitOnlyProperties(bool value) => features.Set(LanguageFeatures.InitOnlyProperties, value);

        public LanguageFeatures SetRequiredProperties(bool value) => features.Set(LanguageFeatures.RequiredProperties, value);

        public LanguageFeatures SetRawStringLiterals(bool value) => features.Set(LanguageFeatures.RawStringLiterals, value);

        public LanguageFeatures SetUnsafeAccessors(bool value) => features.Set(LanguageFeatures.UnsafeAccessors, value);

        public LanguageFeatures SetUnions(bool value) => features.Set(LanguageFeatures.Unions, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Has(LanguageFeatures flag) => (features & flag) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LanguageFeatures Set(LanguageFeatures flag, bool value) => value ? features | flag : features & ~flag;
    }
}
