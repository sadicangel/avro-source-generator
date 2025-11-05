namespace AvroSourceGenerator.Configuration;

[Flags]
internal enum LanguageFeatures
{
    None = 0,
    NullableReferenceTypes = 1 << 0,
    Records = 1 << 1,
    InitOnlyProperties = 1 << 2,
    RequiredProperties = 1 << 3,
    RawStringLiterals = 1 << 4,
    UnsafeAccessors = 1 << 5,

    CSharp7_3 = None,
    CSharp8 = NullableReferenceTypes,
    CSharp9 = NullableReferenceTypes | Records,
    CSharp10 = NullableReferenceTypes | Records | InitOnlyProperties,
    CSharp11 = NullableReferenceTypes | Records | InitOnlyProperties | RequiredProperties | RawStringLiterals,
    CSharp12 = NullableReferenceTypes | Records | InitOnlyProperties | RequiredProperties | RawStringLiterals | UnsafeAccessors,
    CSharp13 = NullableReferenceTypes | Records | InitOnlyProperties | RequiredProperties | RawStringLiterals | UnsafeAccessors,

    Latest = 2147483647,
}
