using System.Text.Json;
using Confluent.SchemaRegistry;
using Schema = Confluent.SchemaRegistry.Schema;

namespace AvroSourceGenerator.ConfluentKafka;

internal static class SchemaRegistrySeeder
{
    private const string RootSchemaFileName = "Order.subject.json";

    private static readonly DependencySchemaFile[] s_dependencyFiles =
    [
        new("Customer.avsc", "AvroSourceGenerator.ConfluentKafka.Customer"),
        new("OrderItem.subject.json", "AvroSourceGenerator.ConfluentKafka.OrderItem"),
        new("OrderStatus.avsc", "AvroSourceGenerator.ConfluentKafka.OrderStatus")
    ];

    public static async Task<int> SeedAsync(
        ISchemaRegistryClient schemaRegistryClient,
        string topicName)
    {
        var subjectVersions = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var dependency in s_dependencyFiles)
        {
            var definition = LoadSchemaDefinition(dependency.FileName);
            var schema = new Schema(definition.SchemaText, definition.References, SchemaType.Avro);

            await schemaRegistryClient.RegisterSchemaAsync(dependency.Subject, schema, normalize: false);

            var latest = await schemaRegistryClient.GetLatestSchemaAsync(dependency.Subject);
            subjectVersions[dependency.Subject] = latest.Version;
        }

        var rootDefinition = LoadSchemaDefinition(RootSchemaFileName);
        var rootReferences = rootDefinition.References
            .Select(reference => new SchemaReference(
                reference.Name,
                reference.Subject,
                subjectVersions.TryGetValue(reference.Subject, out var version) ? version : reference.Version))
            .ToList();

        var rootSubject = GetRootSubject(topicName);
        var rootSchema = new Schema(rootDefinition.SchemaText, rootReferences, SchemaType.Avro);
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

    private static SchemaDefinition LoadSchemaDefinition(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, fileName);
        var text = File.ReadAllText(path);

        if (fileName.EndsWith(".subject.json", StringComparison.OrdinalIgnoreCase))
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            var schemaType = root.GetProperty("schemaType").GetString();
            if (!string.Equals(schemaType, "AVRO", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Schema file '{fileName}' must use schemaType 'AVRO'.");
            }

            var references = new List<SchemaReference>();
            if (root.TryGetProperty("references", out var referencesElement))
            {
                foreach (var reference in referencesElement.EnumerateArray())
                {
                    references.Add(
                        new SchemaReference(
                            reference.GetProperty("name").GetString()
                            ?? throw new InvalidOperationException($"Reference in '{fileName}' is missing a name."),
                            reference.GetProperty("subject").GetString()
                            ?? throw new InvalidOperationException($"Reference in '{fileName}' is missing a subject."),
                            reference.GetProperty("version").GetInt32()));
                }
            }

            return new SchemaDefinition(
                root.GetProperty("schema").GetString()
                ?? throw new InvalidOperationException($"Schema file '{fileName}' is missing a schema payload."),
                references);
        }

        return new SchemaDefinition(text, []);
    }

    private static string GetRootSubject(string topicName) => $"{topicName}-value";

    private sealed record DependencySchemaFile(string FileName, string Subject);

    private sealed record SchemaDefinition(string SchemaText, List<SchemaReference> References);
}
