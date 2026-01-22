using System.Diagnostics.CodeAnalysis;
using Xunit.Sdk;

namespace AvroSourceGenerator.IntegrationTests;

public sealed class XUnitSerializer : IXunitSerializer
{
    public bool IsSerializable(Type type, object? value, [NotNullWhen(false)] out string? failureReason)
    {
        var isSerializable = type == typeof(FileInfo);
        failureReason = isSerializable ? null : $"Type {type} is not serializable.";
        return isSerializable;
    }

    public object Deserialize(Type type, string serializedValue) => type switch
    {
        _ when type == typeof(FileInfo) => new FileInfo(serializedValue),
        _ => throw new NotSupportedException($"Type {type} is not supported for deserialization.")
    };

    public string Serialize(object value) => value switch
    {
        FileInfo fileInfo => fileInfo.FullName,
        _ => throw new NotSupportedException($"Type {value.GetType()} is not supported for serialization.")
    };
}
