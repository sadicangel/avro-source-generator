using System.Text.Json;
using AvroSourceGenerator.Avjs;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Diagnostics;

internal static class AvjsDiagnosticExtensions
{
    extension(AvroDiagnostic)
    {
        public static AvroDiagnostic InvalidProperty(JsonPropertySyntax property, JsonSyntax? invalidValue = null) =>
            new AvroDiagnostic(GetPropertyCode(property), (invalidValue ?? property.Value).SourceSpan);

        public static AvroDiagnostic InvalidProperty(JsonSyntax syntax)
        {
            if (GetContainingProperty(syntax) is { } property)
                return AvroDiagnostic.InvalidProperty(property, syntax);

            return syntax.TokenType is JsonTokenType.String
                ? AvroDiagnostic.InvalidSchemaReference(syntax.SourceSpan, syntax.GetDisplayText())
                : AvroDiagnostic.SchemaExpected(syntax.SourceSpan);
        }
    }

    private static AvroDiagnosticCode GetPropertyCode(JsonPropertySyntax property)
    {
        if (property.Parent is { } parent && GetContainingProperty(parent)?.Name.Value is AvroJsonKeys.Messages)
            return AvroDiagnosticCode.InvalidMessages;

        return property.Name.Value switch
        {
            AvroJsonKeys.Name => GetDeclarationCollection(property) switch
            {
                AvroJsonKeys.Fields => AvroDiagnosticCode.InvalidFieldName,
                AvroJsonKeys.Request => AvroDiagnosticCode.InvalidRequestParameterName,
                _ => AvroDiagnosticCode.InvalidSchemaName,
            },
            AvroJsonKeys.Type => GetDeclarationCollection(property) switch
            {
                AvroJsonKeys.Fields => AvroDiagnosticCode.InvalidFieldType,
                AvroJsonKeys.Request => AvroDiagnosticCode.InvalidParameterType,
                AvroJsonKeys.Types => AvroDiagnosticCode.InvalidProtocolDeclarationType,
                _ => AvroDiagnosticCode.InvalidSchemaType,
            },
            AvroJsonKeys.Protocol => AvroDiagnosticCode.InvalidProtocolName,
            AvroJsonKeys.Namespace => AvroDiagnosticCode.InvalidNamespace,
            AvroJsonKeys.Fields => AvroDiagnosticCode.InvalidFields,
            AvroJsonKeys.Items => AvroDiagnosticCode.InvalidItems,
            AvroJsonKeys.Values => AvroDiagnosticCode.InvalidValues,
            AvroJsonKeys.Symbols => AvroDiagnosticCode.InvalidSymbols,
            AvroJsonKeys.Aliases => AvroDiagnosticCode.InvalidAliases,
            AvroJsonKeys.Doc => AvroDiagnosticCode.InvalidDoc,
            AvroJsonKeys.LogicalType => AvroDiagnosticCode.InvalidLogicalType,
            AvroJsonKeys.Types => AvroDiagnosticCode.InvalidTypes,
            AvroJsonKeys.Messages => AvroDiagnosticCode.InvalidMessages,
            AvroJsonKeys.Request => AvroDiagnosticCode.InvalidRequest,
            AvroJsonKeys.Response => AvroDiagnosticCode.InvalidResponse,
            AvroJsonKeys.Errors => AvroDiagnosticCode.InvalidErrors,
            AvroJsonKeys.OneWay => AvroDiagnosticCode.InvalidOneWay,
            AvroJsonKeys.Default => AvroDiagnosticCode.InvalidEnumDefault,
            AvroJsonKeys.Size => AvroDiagnosticCode.InvalidFixedSize,
            _ => throw new ArgumentOutOfRangeException(nameof(property), property.Name.Value, "Property has no validation diagnostic."),
        };
    }

    private static string? GetDeclarationCollection(JsonPropertySyntax property) =>
        property.Parent?.Parent is JsonArraySyntax array ? GetContainingProperty(array)?.Name.Value : null;

    private static JsonPropertySyntax? GetContainingProperty(JsonSyntax syntax)
    {
        // Union elements inherit their containing property's diagnostic. An object
        // starts a new definition, so errors in its properties keep their own codes.
        while (syntax.Parent is JsonArraySyntax array)
            syntax = array;

        if (syntax.Parent is not JsonObjectSyntax parent)
            return null;

        foreach (var property in parent.Properties)
        {
            if (ReferenceEquals(property.Value, syntax))
                return property;
        }

        return null;
    }
}
