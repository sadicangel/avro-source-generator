using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Xunit.Sdk;

namespace AvroSourceGenerator.IntegrationTests.Schemas;

partial record Notification { }

[SuppressMessage("Extensibility", "xUnit3001:Classes that are marked as serializable (or created by the test framework at runtime) must have a public parameterless constructor", Justification = "<Pending>")]
partial record OneOfEmailContentSmsContentPushContent : IXunitSerializable
{
    public abstract void Deserialize(IXunitSerializationInfo info);

    public abstract void Serialize(IXunitSerializationInfo info);
}

partial record EmailContent : IXunitSerializable
{
    public override void Deserialize(IXunitSerializationInfo info)
    {
        Set_subject(this, (string)(info.GetValue(nameof(subject))!));
        Set_body(this, (string)(info.GetValue(nameof(body))!));
        Set_recipientEmail(this, (string)(info.GetValue(nameof(recipientEmail))!));

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_subject")]
        extern static void Set_subject(EmailContent obj, string value);
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_body")]
        extern static void Set_body(EmailContent obj, string value);
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_recipientEmail")]
        extern static void Set_recipientEmail(EmailContent obj, string value);
    }

    public override void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(subject), subject);
        info.AddValue(nameof(body), body);
        info.AddValue(nameof(recipientEmail), recipientEmail);
    }
}

partial record PushContent : IXunitSerializable
{
    public override void Deserialize(IXunitSerializationInfo info)
    {
        Set_title(this, (string)(info.GetValue(nameof(title))!));
        Set_message(this, (string)(info.GetValue(nameof(message))!));
        Set_deviceToken(this, (string)(info.GetValue(nameof(deviceToken))!));

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_title")]
        extern static void Set_title(PushContent obj, string value);
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_message")]
        extern static void Set_message(PushContent obj, string value);
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_deviceToken")]
        extern static void Set_deviceToken(PushContent obj, string value);
    }

    public override void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(title), title);
        info.AddValue(nameof(message), message);
        info.AddValue(nameof(deviceToken), deviceToken);
    }
}

partial record SmsContent : IXunitSerializable
{
    public override void Deserialize(IXunitSerializationInfo info)
    {
        Set_message(this, (string)(info.GetValue(nameof(message))!));
        Set_phoneNumber(this, (string)(info.GetValue(nameof(phoneNumber))!));

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_message")]
        extern static void Set_message(SmsContent obj, string value);
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_phoneNumber")]
        extern static void Set_phoneNumber(SmsContent obj, string value);
    }

    public override void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(message), message);
        info.AddValue(nameof(phoneNumber), phoneNumber);
    }
}
