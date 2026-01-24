using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Registry.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

internal readonly partial struct SchemaRegistry
{
    private ImmutableArray<ProtocolMessage> ProtocolMessages(JsonElement.ObjectEnumerator messages, string? containingNamespace)
    {
        var protocolMessages = ImmutableArray.CreateBuilder<ProtocolMessage>();
        foreach (var message in messages)
            protocolMessages.Add(Message(message, containingNamespace));
        return protocolMessages.ToImmutable();
    }

    private ProtocolMessage Message(JsonProperty property, string? containingNamespace)
    {
        var methodName = property.Name.ToValidName();
        var documentation = property.Value.GetDocumentation();
        var requestParameters = ProtocolRequestParameters(property.Value, containingNamespace);
        var response = ProtocolResponse(property.Value.GetRequiredProperty("response"), containingNamespace);
        var errors = ProtocolErrors(property.Value.GetNullableArray("errors"), containingNamespace);
        return new ProtocolMessage(methodName, documentation, requestParameters, response, errors);
    }
}
