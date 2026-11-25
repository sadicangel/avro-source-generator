using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas.Defaults;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Syntax.Annotations;
using AvroSourceGenerator.Syntax.Declarations;
using AvroSourceGenerator.Syntax.Json;
using AvroSourceGenerator.Syntax.Types;
using AvroSourceGenerator.Syntax;

namespace AvroSourceGenerator.Compiler.Parsing;

internal static class AvroSyntaxExtensions
{
    extension(INameSyntax syntax)
    {
        public Result<string> GetNamespace() => NameValidator.IsValidName(syntax.FullName)
            ? syntax.FullName : AvroDiagnostic.InvalidNamespace(syntax.GetSourceSpan());

        public Result<SchemaName> AsSchemaName(bool isProtocol = false) => NameValidator.IsValidName(syntax.FullName)
            ? SchemaName.FromFullName(syntax.FullName)
            : isProtocol ? AvroDiagnostic.InvalidProtocolName(syntax.GetSourceSpan()) : AvroDiagnostic.InvalidSchemaName(syntax.GetSourceSpan());
    }

    extension(NamedTypeSyntax syntax)
    {
        public Result<SchemaName> AsSchemaReference() =>
            NameValidator.IsValidName(syntax.Name.FullName)
                ? SchemaName.FromFullName(syntax.Name.FullName)
                : AvroDiagnostic.InvalidSchemaReference(syntax.Name.GetSourceSpan(), syntax.Name.FullName);
    }

    extension(IDeclarationSyntax syntax)
    {
        public Result<Option<string>> GetDocumentation() => syntax.Documentation switch
        {
            [] => Option.None<string>(),
            [var doc] => Option.Some(doc.DocumentationTrivia.ValueText),
            _ => syntax.Documentation.Aggregate(new StringBuilder(), (acc, doc) => acc.AppendLine(doc.DocumentationTrivia.ValueText), acc => acc.ToString()),
        };

        public Result<ImmutableSortedDictionary<string, JsonElement>> GetSchemaProperties() => syntax.Annotations.GetProperties(ReservedSchemaProperties.IsReserved);

        public Result<ImmutableSortedDictionary<string, JsonElement>> GetProtocolProperties() => syntax.Annotations.GetProperties(ReservedProtocolProperties.IsReserved);

        public Result<ImmutableArray<string>> GetAliases() =>
            (syntax.Annotations.OfType<AliasesAnnotationSyntax>().LastOrDefault()?.JsonValue).ToOption().AsAliases();

        public Result<Option<string>> GetNamespace() => syntax.Annotations.GetNamespace();

        public Result<Option<string>> GetOrder() => syntax.Annotations.GetOrder();

        public Result<Option<string>> GetLogicalTypeName() => syntax.Annotations.GetLogicalTypeName();
    }

    extension(FieldDeclarationSyntax syntax)
    {
        public Result<MemberName> GetFieldName() => NameValidator.IsValidMemberName(syntax.Name.FullName)
            ? new MemberName(syntax.Name.FullName) : AvroDiagnostic.InvalidFieldName(syntax.Name.GetSourceSpan());
    }

    extension(ParameterDeclarationSyntax syntax)
    {
        public Result<MemberName> GetParameterName() => NameValidator.IsValidMemberName(syntax.Name.FullName)
            ? new MemberName(syntax.Name.FullName) : AvroDiagnostic.InvalidRequestParameterName(syntax.Name.GetSourceSpan());
    }

    extension(EnumDeclarationSyntax syntax)
    {
        public Result<Option<string>> GetEnumDefault() =>
            syntax.DefaultValue?.Symbol switch
            {
                null or { SyntaxKind: SyntaxKind.NullKeyword } => Option.None<string>(),
                { SyntaxKind: SyntaxKind.IdentifierToken or SyntaxKind.StringLiteralToken, ValueText: var symbol } =>
                    string.IsNullOrWhiteSpace(symbol) ? Option.None<string>() : Option.Some(symbol),
                var symbol => AvroDiagnostic.InvalidEnumDefault(symbol.SourceSpan)
            };
    }

    extension(FixedDeclarationSyntax syntax)
    {
        public Result<int> GetFixedSize() => int.TryParse(syntax.SizeLiteralToken.SourceSpan.AsSpan(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var size) && size > 0
            ? size : AvroDiagnostic.InvalidFixedSize(syntax.SizeLiteralToken.GetSourceSpan());
    }

    extension(DecimalLogicalTypeSyntax syntax)
    {
        public Result<int> GetDecimalPrecision() => int.TryParse(syntax.PrecisionLiteralToken.SourceSpan.AsSpan(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var precision)
            ? precision : AvroDiagnostic.InvalidDecimalPrecision(syntax.PrecisionLiteralToken.GetSourceSpan());

        public Result<int> GetDecimalScale() => int.TryParse(syntax.ScaleLiteralToken.SourceSpan.AsSpan(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var scale)
            ? scale : AvroDiagnostic.InvalidDecimalScale(syntax.ScaleLiteralToken.GetSourceSpan());
    }

    extension(AnnotatedTypeSyntax syntax)
    {
        public Result<Option<string>> GetLogicalTypeName() => syntax.Annotations.GetLogicalTypeName();

        public Result<ImmutableSortedDictionary<string, JsonElement>> GetSchemaProperties() => syntax.Annotations.GetProperties(ReservedSchemaProperties.IsReserved);
    }

    extension(IEnumerable<IAnnotationSyntax> annotations)
    {
        public Result<Option<int>> GetDecimalPrecision() =>
            annotations.GetDecimalAnnotation(AvroJsonKeys.Precision).AsDecimalPrecision();

        public Result<Option<int>> GetDecimalScale() =>
            annotations.GetDecimalAnnotation(AvroJsonKeys.Scale).AsDecimalScale();

        private Option<JsonValueSyntax> GetDecimalAnnotation(string name) =>
            (annotations.OfType<CustomAnnotationSyntax>().LastOrDefault(a => a.AnnotationName.FullName == name)?.JsonValue).ToOption();

        private Result<ImmutableSortedDictionary<string, JsonElement>> GetProperties(Func<string, bool> isReserved)
        {
            // Preserve the existing dictionary value equality; JSON content equality is a separate contract.
            // ReSharper disable once UsageOfDefaultStructEquality
            var properties = ImmutableSortedDictionary.CreateBuilder<string, JsonElement>();
            foreach (var annotation in annotations.OfType<CustomAnnotationSyntax>())
            {
                if (!isReserved(annotation.AnnotationName.FullName))
                    properties[annotation.AnnotationName.FullName] = annotation.JsonValue.AsJsonElement();
            }
            return properties.ToImmutable();
        }

        private Result<Option<string>> GetNamespace() =>
            (annotations.OfType<NamespaceAnnotationSyntax>().LastOrDefault()?.JsonValue).ToOption().AsNamespace();

        private Result<Option<string>> GetOrder() =>
            (annotations.OfType<OrderAnnotationSyntax>().LastOrDefault()?.JsonValue).ToOption().AsOrder();

        private Result<Option<string>> GetLogicalTypeName() =>
            (annotations.OfType<LogicalTypeAnnotationSyntax>().LastOrDefault()?.JsonValue).ToOption().AsLogicalTypeName();
    }

    extension(DefaultValueClauseSyntax? syntax)
    {
        public bool HasNonNullDefault => syntax?.JsonValue is not (null or JsonNullSyntax);

        public Result<Option<DefaultValue>> GetDefaultValue() =>
            (syntax?.JsonValue).ToOption().Map(static json => json.ToDefaultValue());
    }
}
