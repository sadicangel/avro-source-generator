using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AvroSourceGenerator.Avdl.Syntax;
using AvroSourceGenerator.Avdl.Syntax.Annotations;
using AvroSourceGenerator.Avdl.Syntax.Declarations;
using AvroSourceGenerator.Avsc;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Avdl;

internal static class SyntaxAvroExtensions
{
    extension(IDeclarationSyntax syntax)
    {
        public SchemaName GetRequiredSchemaName(string? containingNamespace)
        {
            var name = syntax.Name.FullName;
            if (!name.TrySplitQualifiedName(out name, out var @namespace))
                @namespace = syntax.Annotations.OfType<NamespaceAnnotationSyntax>().LastOrDefault() is { } annotation
                    ? annotation.JsonValue.GetRequiredString("Namespace annotation value")
                    : containingNamespace;
            return new SchemaName(name, @namespace);
        }

        public string? GetDocumentation() => syntax.Documentation switch
        {
            [] => null,
            [var doc] => doc.DocumentationTrivia.ValueText,
            _ => syntax.Documentation.Aggregate(new StringBuilder(), (acc, doc) => acc.AppendLine(doc.DocumentationTrivia.ValueText), acc => acc.ToString()),
        };

        public ImmutableArray<string> GetAliases() => syntax.Annotations.OfType<AliasesAnnotationSyntax>().LastOrDefault() is { } annotation
            ? annotation.JsonValue.GetRequiredStringArray("Aliases annotation value")
            : [];

        public ImmutableSortedDictionary<string, JsonElement> GetSchemaProperties() => syntax.Annotations.GetProperties(ReservedSchemaProperties.IsReserved);

        public ImmutableSortedDictionary<string, JsonElement> GetProtocolProperties() => syntax.Annotations.GetProperties(ReservedProtocolProperties.IsReserved);
    }

    extension(IEnumerable<IAnnotationSyntax> annotations)
    {
        public ImmutableSortedDictionary<string, JsonElement> GetProperties(Func<string, bool> isReserved)
        {
            var properties = ImmutableSortedDictionary.CreateBuilder<string, JsonElement>();
            foreach (var annotation in annotations.OfType<CustomAnnotationSyntax>())
            {
                if (!isReserved(annotation.AnnotationName.FullName))
                    properties[annotation.AnnotationName.FullName] = annotation.JsonValue.ToJsonElement();
            }
            return properties.ToImmutable();
        }
    }

    extension(JsonValueSyntax syntax)
    {
        public JsonElement ToJsonElement() => JsonSerializer.SerializeToElement(syntax.JsonNode);
        public JsonElement? ToOptionalJsonElement() => JsonSerializer.SerializeToElement(syntax.JsonNode);

        public string? ToOptionalString(string description)
        {
            if (syntax.JsonNode is null)
                return null;
            if (syntax.JsonNode is JsonValue value && value.TryGetValue<string>(out var result))
                return result;

            throw new InvalidSourceException($"{description} must be a string.", syntax.GetSourceSpan());
        }

        public string GetRequiredString(string description) =>
            syntax.ToOptionalString(description)
            ?? throw new InvalidSourceException($"{description} must be a string.", syntax.GetSourceSpan());

        public ImmutableArray<string> GetRequiredStringArray(string description)
        {
            if (syntax.JsonNode is not JsonArray array)
                throw new InvalidSourceException($"{description} must be an array of strings.", syntax.GetSourceSpan());

            var values = ImmutableArray.CreateBuilder<string>(array.Count);
            foreach (var node in array)
            {
                if (node is not JsonValue value || !value.TryGetValue<string>(out var result))
                    throw new InvalidSourceException($"{description} must be an array of strings.", syntax.GetSourceSpan());
                values.Add(result);
            }

            return values.MoveToImmutable();
        }
    }
}
