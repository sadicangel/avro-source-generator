using AvroSourceGenerator.Configuration;

namespace AvroSourceGenerator.Compiler;

public readonly record struct AvroParseOptions(
    TargetProfile TargetProfile,
    bool UseNullableReferenceTypes);
