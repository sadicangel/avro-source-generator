using AvroSourceGenerator.Configuration;

namespace AvroSourceGenerator.Registry;

public readonly record struct SchemaRegistryOptions(
    TargetProfile TargetProfile,
    DuplicateResolution DuplicateResolution,
    bool UseNullableReferenceTypes)
{
    public static readonly SchemaRegistryOptions Default = new SchemaRegistryOptions(TargetProfile.Modern, DuplicateResolution.Error, true);
}
