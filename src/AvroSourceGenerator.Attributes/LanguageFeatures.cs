namespace AvroSourceGenerator;

[Flags]
public enum LanguageFeatures
{
    None = 0,
    NullableReferenceTypes = 1 << 0,
    FileScopedNamespaces = 1 << 1,
    InitOnlyProperties = 1 << 2,
    RequiredProperties = 1 << 3,
    UnsafeAccessors = 1 << 4,

    CSharp7_3 = None,
    CSharp8 = NullableReferenceTypes,
    CSharp9 = NullableReferenceTypes | FileScopedNamespaces,
    CSharp10 = NullableReferenceTypes | FileScopedNamespaces | InitOnlyProperties,
    CSharp11 = NullableReferenceTypes | FileScopedNamespaces | InitOnlyProperties | RequiredProperties,
    CSharp12 = NullableReferenceTypes | FileScopedNamespaces | InitOnlyProperties | RequiredProperties | UnsafeAccessors,

    Latest = 2147483647,
}

