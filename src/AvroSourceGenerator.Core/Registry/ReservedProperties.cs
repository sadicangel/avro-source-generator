using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

internal static class ReservedProperties
{
    private static readonly HashSet<string> s_reservedProperties =
    [
        AvroJsonKeys.Type,
        AvroJsonKeys.Name,
        AvroJsonKeys.Namespace,
        AvroJsonKeys.Fields,
        AvroJsonKeys.Items,
        AvroJsonKeys.Size,
        AvroJsonKeys.Symbols,
        AvroJsonKeys.Values,
        AvroJsonKeys.Aliases,
        AvroJsonKeys.Order,
        AvroJsonKeys.Doc,
        AvroJsonKeys.Default,
        AvroJsonKeys.LogicalType,
    ];

    public static bool IsReserved(string propertyName) => s_reservedProperties.Contains(propertyName);
}