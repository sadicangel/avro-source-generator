using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Protocols;

public sealed record class ProtocolResponse(
    AvroSchema Type,
    AvroSchema UnderlyingType);
