using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AvroSourceGenerator.Avdl;
using AvroSourceGenerator.Avdl.Annotations;
using AvroSourceGenerator.Avdl.Declarations;
using AvroSourceGenerator.Avdl.Types;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Schemas.Defaults;

namespace AvroSourceGenerator.Compiler.Parsing;

internal static class AvroSyntaxExtensions
{
    extension(INameSyntax syntax)
    {
        public Result<SchemaName> GetSchemaName(string? containingNamespace)
        {
            var isQualified = syntax.FullName.TrySplitQualifiedName(out var localName, out var @namespace);
            if (!NameValidator.IsValidName(localName))
                return AvroDiagnostic.InvalidIdlProperty(AvroDiagnosticCode.InvalidSchemaName, syntax.GetSourceSpan());
            if (!isQualified)
                return new SchemaName(localName, containingNamespace);
            if (!NameValidator.IsValidName(@namespace))
                return AvroDiagnostic.InvalidIdlProperty(AvroDiagnosticCode.InvalidSchemaName, syntax.GetSourceSpan());
            return new SchemaName(localName, @namespace);
        }
    }

    extension(IDeclarationSyntax syntax)
    {
        public Result<SchemaName> GetSchemaName(string? containingNamespace)
        {
            var name = syntax.Name;
            if (name.FullName.Contains('.') is true)
                return name.GetSchemaName(containingNamespace: null);
            var @namespace = syntax.GetNamespace();
            if (@namespace.IsError) return @namespace.GetError();
            return name.GetSchemaName(@namespace.GetValue().GetValueOrDefault() ?? containingNamespace);
        }

        public Result<MemberName> GetMemberName() => new MemberName(syntax.Name.FullName);

        public Result<Option<string>> GetDocumentation() => syntax.Documentation switch
        {
            [] => null,
            [var doc] => Option.Some(doc.DocumentationTrivia.ValueText),
            _ => syntax.Documentation.Aggregate(new StringBuilder(), (acc, doc) => acc.AppendLine(doc.DocumentationTrivia.ValueText), acc => acc.ToString()),
        };

        public Result<ImmutableSortedDictionary<string, JsonElement>> GetSchemaProperties() => syntax.Annotations.GetProperties(ReservedSchemaProperties.IsReserved);

        public Result<ImmutableSortedDictionary<string, JsonElement>> GetProtocolProperties() => syntax.Annotations.GetProperties(ReservedProtocolProperties.IsReserved);

        public Result<ImmutableArray<string>> GetAliases()
        {
            if (syntax.Annotations.OfType<AliasesAnnotationSyntax>().LastOrDefault() is not { } annotation)
                return ImmutableArray<string>.Empty;
            if (annotation.JsonValue.JsonNode is not JsonArray array)
                return AvroDiagnostic.InvalidIdlProperty(AvroDiagnosticCode.InvalidIdlAliases, annotation.JsonValue.GetSourceSpan());

            var builder = ImmutableArray.CreateBuilder<string>(array.Count);
            foreach (var node in array)
            {
                if (node is not JsonValue value || !value.TryGetValue<string>(out var result))
                    return AvroDiagnostic.InvalidIdlProperty(AvroDiagnosticCode.InvalidIdlAliases, annotation.JsonValue.GetSourceSpan());
                builder.Add(result);
            }
            return builder.MoveToImmutable();
        }

        public Result<Option<string>> GetNamespace() => syntax.Annotations.GetNamespace();

        public Result<Option<string>> GetOrder() => syntax.Annotations.GetOrder();

        public Result<Option<string>> GetLogicalTypeName() => syntax.Annotations.GetLogicalTypeName();
    }

    extension(EnumDeclarationSyntax syntax)
    {
        public Result<Option<string>> GetEnumDefault() => syntax.DefaultValue?.JsonValue.JsonNode switch
        {
            JsonValue value when value.TryGetValue<string>(out var result) => Option.Some(result),
            JsonValue value when value.GetValueKind() is JsonValueKind.Null or JsonValueKind.Undefined => Option.None<string>(),
            null => Option.None<string>(),
            _ => AvroDiagnostic.InvalidIdlProperty(AvroDiagnosticCode.InvalidIdlEnumDefault, syntax.DefaultValue!.JsonValue.GetSourceSpan())
        };
    }

    extension(FixedDeclarationSyntax syntax)
    {
        public Result<int> GetFixedSize() => syntax.SizeLiteralToken.Value is int value and > 0
            ? value
            : AvroDiagnostic.InvalidIdlFixedSize(syntax.SizeLiteralToken.SourceSpan);
    }

    extension(AnnotatedTypeSyntax syntax)
    {
        public Result<Option<string>> GetLogicalTypeName() => syntax.Annotations.GetLogicalTypeName();

        public Result<ImmutableSortedDictionary<string, JsonElement>> GetSchemaProperties() => syntax.Annotations.GetProperties(ReservedSchemaProperties.IsReserved);
    }

    extension(IEnumerable<IAnnotationSyntax> annotations)
    {
        private Result<ImmutableSortedDictionary<string, JsonElement>> GetProperties(Func<string, bool> isReserved)
        {
            // Preserve the existing dictionary value equality; JSON content equality is a separate contract.
            // ReSharper disable once UsageOfDefaultStructEquality
            var properties = ImmutableSortedDictionary.CreateBuilder<string, JsonElement>();
            foreach (var annotation in annotations.OfType<CustomAnnotationSyntax>())
            {
                if (!isReserved(annotation.AnnotationName.FullName))
                    properties[annotation.AnnotationName.FullName] = JsonSerializer.SerializeToElement(annotation.JsonValue.JsonNode);
            }
            return properties.ToImmutable();
        }

        private Result<Option<string>> GetNamespace() => annotations.OfType<NamespaceAnnotationSyntax>().LastOrDefault() switch
        {
            null => Option.None<string>(),
            NamespaceAnnotationSyntax { JsonValue.JsonNode: JsonValue value } when value.TryGetValue<string>(out var result) && (result.Length == 0 || NameValidator.IsValidName(result)) => Option.Some(result),
            NamespaceAnnotationSyntax syntax => AvroDiagnostic.InvalidIdlProperty(AvroDiagnosticCode.InvalidIdlNamespace, syntax.JsonValue.GetSourceSpan())
        };

        private Result<Option<string>> GetOrder() => annotations.OfType<OrderAnnotationSyntax>().LastOrDefault() switch
        {
            null => Option.None<string>(),
            OrderAnnotationSyntax { JsonValue.JsonNode: JsonValue value } when value.TryGetValue<string>(out var result) => Option.Some(result),
            OrderAnnotationSyntax syntax => AvroDiagnostic.InvalidIdlProperty(AvroDiagnosticCode.InvalidIdlOrder, syntax.JsonValue.GetSourceSpan())
        };

        private Result<Option<string>> GetLogicalTypeName() => annotations.OfType<LogicalTypeAnnotationSyntax>().LastOrDefault() switch
        {
            null => Option.None<string>(),
            LogicalTypeAnnotationSyntax { JsonValue.JsonNode: JsonValue value } when value.TryGetValue<string>(out var result) => Option.Some(result),
            LogicalTypeAnnotationSyntax syntax => AvroDiagnostic.InvalidIdlProperty(AvroDiagnosticCode.InvalidIdlLogicalTypeAnnotation, syntax.JsonValue.GetSourceSpan())
        };
    }

    extension(DefaultValueClauseSyntax? syntax)
    {
        public Result<Option<DefaultValue>> GetDefaultValue() =>
            (syntax?.JsonValue).ToOption().Map<DefaultValue>(static json => new UnboundDefaultValue(JsonSerializer.SerializeToElement(json.JsonNode)));
    }

    extension(Option<Option<DefaultValue>> defaultValue)
    {
        public bool IsNotNullOrNullLiteral => defaultValue
            .Map(static option => option.Map(static value => value is UnboundDefaultValue { Json.ValueKind: not (JsonValueKind.Null or JsonValueKind.Undefined) }))
            .GetValueOrDefault()
            .GetValueOrDefault();
    }
}
