using System.Collections.Immutable;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Protocols;

public sealed record class ProtocolMessage(
    MemberName MethodName,
    string? Documentation,
    ImmutableArray<ProtocolRequestParameter> RequestParameters,
    ProtocolResponse Response,
    AvroSchema? Errors,
    bool? OneWay);
