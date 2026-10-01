using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using AvroSourceGenerator.Avdl.Annotations;
using AvroSourceGenerator.Avdl.Declarations;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Avdl;

internal static class SyntaxAvroExtensions
{
    extension(IDeclarationSyntax syntax)
    {
        public string? GetDocumentation() => syntax.Documentation switch
        {
            [] => null,
            [var doc] => doc.DocumentationTrivia.ValueText,
            _ => syntax.Documentation.Aggregate(new StringBuilder(), (acc, doc) => acc.AppendLine(doc.DocumentationTrivia.ValueText), acc => acc.ToString()),
        };

        public ImmutableSortedDictionary<string, JsonElement> GetSchemaProperties() => syntax.Annotations.GetProperties(ReservedSchemaProperties.IsReserved);

        public ImmutableSortedDictionary<string, JsonElement> GetProtocolProperties() => syntax.Annotations.GetProperties(ReservedProtocolProperties.IsReserved);
    }

    extension(IEnumerable<IAnnotationSyntax> annotations)
    {
        public ImmutableSortedDictionary<string, JsonElement> GetProperties(Func<string, bool> isReserved)
        {
            // Preserve the existing dictionary value equality; JSON content equality is a separate contract.
            // ReSharper disable once UsageOfDefaultStructEquality
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
    }
}
