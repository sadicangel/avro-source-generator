namespace AvroSourceGenerator;

[Flags]
public enum LanguageFeatures
{
    None = 0,
    NullableReferenceTypes = 1 << 0,
    InitOnlyProperties = 1 << 1,
    RequiredProperties = 1 << 2,
    UnsafeAccessors = 1 << 3,

    CSharp7_3 = None,
    CSharp8 = NullableReferenceTypes,
    CSharp9 = NullableReferenceTypes,
    CSharp10 = NullableReferenceTypes | InitOnlyProperties,
    CSharp11 = NullableReferenceTypes | InitOnlyProperties | RequiredProperties,
    CSharp12 = NullableReferenceTypes | InitOnlyProperties | RequiredProperties | UnsafeAccessors,
    CSharp13 = NullableReferenceTypes | InitOnlyProperties | RequiredProperties | UnsafeAccessors,

    Latest = 2147483647,
}

