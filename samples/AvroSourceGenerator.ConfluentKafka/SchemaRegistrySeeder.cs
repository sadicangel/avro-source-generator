using Confluent.SchemaRegistry;
using Schema = Confluent.SchemaRegistry.Schema;

namespace AvroSourceGenerator.ConfluentKafka;

internal static class SchemaRegistrySeeder
{
    private const string RootSchemaFileName = "Order.avsc";

    private static readonly DependencySchemaFile[] s_dependencyFiles =
    [
        new("Customer.avsc", "AvroSourceGenerator.ConfluentKafka.Customer"),
        new("OrderItem.avsc", "AvroSourceGenerator.ConfluentKafka.OrderItem"),
        new("OrderStatus.avsc", "AvroSourceGenerator.ConfluentKafka.OrderStatus")
    ];

    public static async Task<int> SeedAsync(
        ISchemaRegistryClient schemaRegistryClient,
        string topicName)
    {
        var subjectVersions = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var dependency in s_dependencyFiles)
        {
            var schemaText = LoadSchemaText(dependency.FileName);
            var schema = new Schema(schemaText, references: [], SchemaType.Avro);

            await schemaRegistryClient.RegisterSchemaAsync(dependency.Subject, schema, normalize: false);

            var latest = await schemaRegistryClient.GetLatestSchemaAsync(dependency.Subject);
            subjectVersions[dependency.Subject] = latest.Version;
        }

        var rootSchemaText = LoadSchemaText(RootSchemaFileName);
        var rootReferences = s_dependencyFiles
            .Select(dependency => new SchemaReference(
                dependency.Subject,
                dependency.Subject,
                subjectVersions[dependency.Subject]))
            .ToList();

        var rootSubject = GetRootSubject(topicName);
        var rootSchema = new Schema(rootSchemaText, rootReferences, SchemaType.Avro);
        return await schemaRegistryClient.RegisterSchemaAsync(rootSubject, rootSchema, normalize: false);
    }

    public static async Task VerifyAsync(
        ISchemaRegistryClient schemaRegistryClient,
        string topicName)
    {
        var subjects = await schemaRegistryClient.GetAllSubjectsAsync();
        var expectedSubjects = s_dependencyFiles
            .Select(static dependency => dependency.Subject)
            .Append(GetRootSubject(topicName));

        foreach (var subject in expectedSubjects)
        {
            if (!subjects.Contains(subject, StringComparer.Ordinal))
            {
                throw new InvalidOperationException($"Expected schema subject '{subject}' was not found in Schema Registry.");
            }
        }

        var latestRootSchema = await schemaRegistryClient.GetLatestSchemaAsync(GetRootSubject(topicName));
        if (latestRootSchema.References is null || latestRootSchema.References.Count != s_dependencyFiles.Length)
        {
            throw new InvalidOperationException(
                $"Expected root schema to reference {s_dependencyFiles.Length} schemas, but found {latestRootSchema.References?.Count ?? 0}.");
        }
    }

    private static string LoadSchemaText(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, fileName);
        return File.ReadAllText(path);
    }

    private static string GetRootSubject(string topicName) => $"{topicName}-value";

    private sealed record DependencySchemaFile(string FileName, string Subject);

}
