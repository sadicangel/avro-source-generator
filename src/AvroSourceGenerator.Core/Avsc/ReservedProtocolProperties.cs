using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Avsc;

internal static class ReservedProtocolProperties
{
    private static readonly HashSet<string> s_reservedProperties =
    [
        AvroJsonKeys.Protocol,
        AvroJsonKeys.Namespace,
        AvroJsonKeys.Types,
        AvroJsonKeys.Messages,
        AvroJsonKeys.Doc,
    ];

    public static bool IsReserved(string propertyName) => s_reservedProperties.Contains(propertyName);
}
