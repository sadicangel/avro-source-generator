using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using AvroSourceGenerator.Avdl.Syntax;
using AvroSourceGenerator.Avdl.Syntax.Annotations;
using AvroSourceGenerator.Avdl.Syntax.Declarations;
using AvroSourceGenerator.Avsc;
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
                @namespace = syntax.Annotations.OfType<NamespaceAnnotationSyntax>().LastOrDefault()?.Namespace ?? containingNamespace;
            return new SchemaName(name, @namespace);
        }

        public string? GetDocumentation() => syntax.Documentation switch
        {
            [] => null,
            [var doc] => doc.DocumentationTrivia.ValueText,
            _ => syntax.Documentation.Aggregate(new StringBuilder(), (acc, doc) => acc.AppendLine(doc.DocumentationTrivia.ValueText), acc => acc.ToString()),
        };

        public ImmutableArray<string> GetAliases() => syntax.Annotations.OfType<AliasesAnnotationSyntax>().LastOrDefault()?.Aliases ?? [];

        public ImmutableSortedDictionary<string, JsonElement> GetSchemaProperties() => syntax.GetProperties(ReservedSchemaProperties.IsReserved);

        public ImmutableSortedDictionary<string, JsonElement> GetProtocolProperties() => syntax.GetProperties(ReservedProtocolProperties.IsReserved);

        private ImmutableSortedDictionary<string, JsonElement> GetProperties(Func<string, bool> isReserved) => syntax.Annotations.OfType<CustomAnnotationSyntax>()
            .Where(a => !isReserved(a.AnnotationName.FullName))
            .ToImmutableSortedDictionary(a => a.AnnotationName.FullName, a => a.JsonValue.ToJsonElement());
    }

    extension(JsonValueSyntax syntax)
    {
        public JsonElement ToJsonElement() => JsonSerializer.SerializeToElement(syntax.JsonNode);
        public JsonElement? ToOptionalJsonElement() => JsonSerializer.SerializeToElement(syntax.JsonNode);
        public string? ToOptionalString() => syntax.JsonNode?.GetValue<string>();
    }
}
