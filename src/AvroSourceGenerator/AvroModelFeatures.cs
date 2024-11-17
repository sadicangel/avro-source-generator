namespace AvroSourceGenerator;

[Flags]
internal enum AvroModelFeatures
{
    None = 0,
    NullableReferenceTypes = 1 << 0,
    FileScopedNamespaces = 1 << 1,
    InitOnlyProperties = 1 << 2,
    RequiredProperties = 1 << 3,
    UnsafeAccessors = 1 << 4,

    NetStandard2_0 = None,
    Net472 = None,
    Net48 = None,
    NetStandard2_1 = NullableReferenceTypes,
    Net5 = NullableReferenceTypes | FileScopedNamespaces,
    Net6 = NullableReferenceTypes | FileScopedNamespaces | InitOnlyProperties,
    Net7 = NullableReferenceTypes | FileScopedNamespaces | InitOnlyProperties | RequiredProperties,
    Net8 = NullableReferenceTypes | FileScopedNamespaces | InitOnlyProperties | RequiredProperties | UnsafeAccessors,
}

