
namespace AvroNet;

internal sealed record class AvroModelOptions(
    string Name,
    string Schema,
    string Namespace,
    string AccessModifier,
    string DeclarationType,
    int DotnetVersion)
{
    public bool UseRequiredProperties { get => DotnetVersion >= 7; }
    public bool UseInitOnlyProperties { get => DotnetVersion >= 8; }
}
