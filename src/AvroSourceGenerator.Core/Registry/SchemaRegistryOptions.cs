using AvroSourceGenerator.Configuration;

namespace AvroSourceGenerator.Registry;

public readonly record struct SchemaRegistryOptions(
    TargetProfile TargetProfile,
    ReferenceResolution ReferenceResolution,
    DuplicateResolution DuplicateResolution,
    bool UseNullableReferenceTypes)
{
    public static readonly SchemaRegistryOptions Default = new SchemaRegistryOptions(TargetProfile.Modern, ReferenceResolution.Strict, DuplicateResolution.Error, true);
}
