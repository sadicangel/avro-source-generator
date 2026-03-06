using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

public readonly partial struct SchemaRegistry
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
        var response = ProtocolResponse(property.Value.GetRequiredProperty(AvroJsonKeys.Response), containingNamespace);
        var errors = ProtocolErrors(property.Value.GetNullableArray(AvroJsonKeys.Errors), containingNamespace);
        return new ProtocolMessage(methodName, documentation, requestParameters, response, errors);
    }
}
