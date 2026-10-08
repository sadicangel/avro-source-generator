using AvroSourceGenerator.IntegrationTests.Schemas;
#if APACHE
using Avro;
using Avro.Specific;
#elif CHR
using Chr.Avro.Abstract;
using Chr.Avro.Representation;
using Chr.Avro.Serialization;
#endif

var nested = new NestedType { displayName = "Imported" };
var envelope = new ImportedEnvelope { Item = nested };
var collections = new Collections { stringList = ["one"], intMap = new Dictionary<string, int> { ["one"] = 1 } };
var request = new CreateUserRequest { username = "user", email = "test@example.com" };
var content = new EmailContent { subject = "Subject", body = "Body", recipientEmail = request.email };
var notification = new Notification { content = content };

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
var record = (ISpecificRecord)envelope;
var schema = (RecordSchema)record.Schema;
Check(schema.Fields.Single().Schema.Equals(((ISpecificRecord)nested).Schema), "Apache imported schema must match.");
Check(ReferenceEquals(record.Get(0), nested), "Apache record accessor must work.");
var protocol = (Protocol)typeof(UserDirectory).GetField("protocol",
    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!.GetValue(null)!;
Check(protocol.Messages.Count == 2, "Apache protocol must contain both messages.");
#elif CHR
var builder = new SchemaBuilder();
var schema = (RecordSchema)builder.BuildSchema<ImportedEnvelope>();
Check(((RecordSchema)schema.Fields.Single().Type).Name == nameof(NestedType), "Chr imported schema must match.");
Check(((RecordSchema)builder.BuildSchema<Collections>()).Fields.Count == 2, "Chr collection schema must build.");
Check(((RecordSchema)builder.BuildSchema<CreateUserRequest>()).Name == nameof(CreateUserRequest), "Chr protocol model must build.");
var unionSchema = new JsonSchemaReader().Read(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Schemas", "Notification.avsc")));
var casePrefix = $"{typeof(Notification).Namespace}.NotificationContent{(unionValue is null ? "Variant" : "Union")}";
var serializer = new BinarySerializerBuilder(BinarySerializerBuilder.CreateDefaultCaseBuilders()
    .Prepend(b => (IBinarySerializerBuilderCase)Activator.CreateInstance(
        typeof(Notification).Assembly.GetType(casePrefix + "SerializerBuilderCase", throwOnError: true)!, b)!));
var deserializer = new BinaryDeserializerBuilder(BinaryDeserializerBuilder.CreateDefaultCaseBuilders()
    .Prepend(b => (IBinaryDeserializerBuilderCase)Activator.CreateInstance(
        typeof(Notification).Assembly.GetType(casePrefix + "DeserializerBuilderCase", throwOnError: true)!, b)!));
Check(serializer.BuildDelegate<Notification>(unionSchema) is not null, "Chr union serializer must build.");
Check(deserializer.BuildDelegate<Notification>(unionSchema) is not null, "Chr union deserializer must build.");
#endif

Console.WriteLine("Package consumer checks passed.");

static void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
