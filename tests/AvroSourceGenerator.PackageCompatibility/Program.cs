using AvroSourceGenerator.IntegrationTests.Schemas;
#if APACHE
using Avro;
using Avro.Specific;
#endif

var nested = new NestedType { displayName = "Imported" };
var envelope = new ImportedEnvelope { Item = nested };
var collections = new Collections { stringList = ["one"], intMap = new Dictionary<string, int> { ["one"] = 1 } };
var request = new CreateUserRequest { username = "user", email = "test@example.com" };
var content = new EmailContent { subject = "Subject", body = "Body", recipientEmail = request.email };
var notification = new Notification { content = content };
var bytes = (BytesDefaults)Activator.CreateInstance(typeof(BytesDefaults))!;
Check(bytes.Plain is null && bytes.Base64Like is null && bytes.Empty is null, "Schema defaults must not generate C# initializers.");
var keyword = new KeywordEnumRecord { Value = KeywordEnum.@class };
var keywordIdl = new KeywordEnumIdlRecord { Value = KeywordEnumIdl.@class };
Check((int)keyword.Value == 0 && (int)KeywordEnum.normal == 1 &&
    (int)keywordIdl.Value == 0 && (int)KeywordEnumIdl.normal == 1, "Enum symbol order must preserve ordinal values.");

Check(ReferenceEquals(envelope.Item, nested), "IDL import must use the schema-generated type.");
Check(collections.intMap[collections.stringList[0]] == 1, "Collection model must be usable.");
var contentType = typeof(Notification).GetProperty(nameof(Notification.content))!.PropertyType;
var unionValue = contentType.GetProperty("Value");
#if UNIONS
Check(contentType.IsValueType && unionValue is not null, "Package props must expose the tagged union preview feature.");
#else
Check(contentType.IsInterface, "Union preview features must remain disabled unless explicitly enabled.");
#endif
Check(ReferenceEquals(unionValue is null ? notification.content : unionValue.GetValue(notification.content), content),
    "Generated union must accept its concrete variant.");
Check(!typeof(ImportedEnvelope).IsPublic, "Package props must expose the internal access modifier option.");
Check(typeof(NestedType).GetMethod("<Clone>$") is null, "Package props must expose the class declaration option.");

#if APACHE
foreach (var keywordRecord in new ISpecificRecord[] { keyword, keywordIdl })
{
    var keywordSchema = (RecordSchema)keywordRecord.Schema;
    var keywordField = keywordSchema.Fields.Single();
    var keywordEnum = (EnumSchema)keywordField.Schema;
    Check(keywordEnum.Symbols.SequenceEqual(new[] { "class", "normal" }), "Enum schema symbols must retain Avro names.");
    Check(keywordEnum.Default == "class" && keywordField.DefaultValue!.ToString() == "class", "Enum schema defaults must retain Avro names.");
}
var record = (ISpecificRecord)envelope;
var schema = (RecordSchema)record.Schema;
Check(schema.Fields.Single().Schema.Equals(((ISpecificRecord)nested).Schema), "Apache imported schema must match.");
Check(ReferenceEquals(record.Get(0), nested), "Apache record accessor must work.");
var protocol = (Protocol)typeof(UserDirectory).GetField("protocol",
    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!.GetValue(null)!;
Check(protocol.Messages.Count == 2, "Apache protocol must contain both messages.");
#endif

Console.WriteLine("Package consumer checks passed.");

static void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
