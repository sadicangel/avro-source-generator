using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Schemas.Defaults;

namespace AvroSourceGenerator.Protocols;

public sealed record class ProtocolRequestParameter(
    MemberName Name,
    AvroSchema Type,
    AvroSchema UnderlyingType,
    string? Documentation,
    DefaultValue? Default);
