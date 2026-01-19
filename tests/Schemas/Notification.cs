using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Xunit.Sdk;

#pragma warning disable IDE0130
namespace AvroSourceGenerator.IntegrationTests.Schemas;

partial record Notification { }

[SuppressMessage(
    "Extensibility",
    "xUnit3001:Classes that are marked as serializable (or created by the test framework at runtime) must have a public parameterless constructor",
    Justification = "<Pending>")]
partial interface INotificationContentVariant : IXunitSerializable;

partial record EmailContent
{
    public void Deserialize(IXunitSerializationInfo info)
    {
        SetSubject(this, (string)info.GetValue(nameof(subject))!);
        SetBody(this, (string)info.GetValue(nameof(body))!);
        SetRecipientEmail(this, (string)info.GetValue(nameof(recipientEmail))!);

        return;

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_subject")]
        static extern void SetSubject(EmailContent obj, string value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_body")]
        static extern void SetBody(EmailContent obj, string value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_recipientEmail")]
        static extern void SetRecipientEmail(EmailContent obj, string value);
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(subject), subject);
        info.AddValue(nameof(body), body);
        info.AddValue(nameof(recipientEmail), recipientEmail);
    }
}

partial record PushContent : IXunitSerializable
{
    public void Deserialize(IXunitSerializationInfo info)
    {
        SetTitle(this, (string)info.GetValue(nameof(title))!);
        SetMessage(this, (string)info.GetValue(nameof(message))!);
        SetDeviceToken(this, (string)info.GetValue(nameof(deviceToken))!);

        return;

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_title")]
        static extern void SetTitle(PushContent obj, string value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_message")]
        static extern void SetMessage(PushContent obj, string value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_deviceToken")]
        static extern void SetDeviceToken(PushContent obj, string value);
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(title), title);
        info.AddValue(nameof(message), message);
        info.AddValue(nameof(deviceToken), deviceToken);
    }
}

partial record SmsContent : IXunitSerializable
{
    public void Deserialize(IXunitSerializationInfo info)
    {
        SetMessage(this, (string)info.GetValue(nameof(message))!);
        SetPhoneNumber(this, (string)info.GetValue(nameof(phoneNumber))!);

        return;

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_message")]
        static extern void SetMessage(SmsContent obj, string value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_phoneNumber")]
        static extern void SetPhoneNumber(SmsContent obj, string value);
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(message), message);
        info.AddValue(nameof(phoneNumber), phoneNumber);
    }
}
