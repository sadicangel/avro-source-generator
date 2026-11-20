using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas.Defaults;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Syntax.Json;
using AvroSourceGenerator.Syntax;

namespace AvroSourceGenerator.Compiler.Parsing;

internal static class JsonSyntaxExtensions
{
    extension(JsonValueSyntax syntax)
    {
        public Option<string> GetStringValue() => syntax is JsonStringSyntax text ? text.Value.ToOption() : Option.None<string>();
    }

    extension(Option<JsonValueSyntax> optional)
    {
        public Result<Option<string>> AsNamespace()
        {
            if (optional is not JsonValueSyntax value || value is JsonNullSyntax) return Option.None<string>();
            return value.GetStringValue().GetValueOrDefault() switch
            {
                "" => Option.None<string>(),
                string ns when NameValidator.IsValidName(ns) => Option.Some(ns),
                _ => AvroDiagnostic.InvalidNamespace(value.GetSourceSpan())
            };
        }

        public Result<Option<string>> AsOrder()
        {
            if (optional is not JsonValueSyntax value || value is JsonNullSyntax) return Option.None<string>();
            return value.GetStringValue() is string text && !string.IsNullOrWhiteSpace(text)
                ? Option.Some(text) : AvroDiagnostic.InvalidOrder(value.GetSourceSpan());
        }

        public Result<Option<string>> AsLogicalTypeName()
        {
            if (optional is not JsonValueSyntax value) return Option.None<string>();
            return value.GetStringValue() is string text && !string.IsNullOrWhiteSpace(text)
                ? Option.Some(text) : AvroDiagnostic.InvalidLogicalType(value.GetSourceSpan());
        }

        public Result<Option<string>> AsEnumDefault()
        {
            if (optional is not JsonValueSyntax value || value is JsonNullSyntax) return Option.None<string>();
            if (value.GetStringValue() is not string text) return AvroDiagnostic.InvalidEnumDefault(value.GetSourceSpan());
            return string.IsNullOrWhiteSpace(text) ? Option.None<string>() : Option.Some(text);
        }

        public Result<ImmutableArray<string>> AsAliases()
        {
            if (optional is not JsonValueSyntax value || value is JsonNullSyntax) return ImmutableArray<string>.Empty;
            if (value is not JsonArraySyntax array) return AvroDiagnostic.InvalidAliases(value.GetSourceSpan());
            var builder = ImmutableArray.CreateBuilder<string>(array.Elements.Length);
            foreach (var item in array.Elements)
            {
                if (item.GetStringValue() is not string alias || string.IsNullOrWhiteSpace(alias))
                    return AvroDiagnostic.InvalidAliases(item.GetSourceSpan());
                builder.Add(alias);
            }
            return builder.MoveToImmutable();
        }

        public Result<Option<int>> AsDecimalPrecision()
        {
            if (optional is not JsonValueSyntax value) return Option.None<int>();
            return value is JsonNumberSyntax { Int32Value: int number } ? Option.Some(number) : AvroDiagnostic.InvalidDecimalPrecision(value.GetSourceSpan());
        }

        public Result<Option<int>> AsDecimalScale()
        {
            if (optional is not JsonValueSyntax value) return Option.None<int>();
            return value is JsonNumberSyntax { Int32Value: int number } ? Option.Some(number) : AvroDiagnostic.InvalidDecimalScale(value.GetSourceSpan());
        }
    }

    extension(JsonPropertySyntax property)
    {
        public Result<SchemaName> GetValueAsSchemaName() => property.Value.GetStringValue() is string name && NameValidator.IsValidName(name)
            ? SchemaName.FromFullName(name)
            : property.Name.ValueText == AvroJsonKeys.Protocol ? AvroDiagnostic.InvalidProtocolName(property.Value.GetSourceSpan()) : AvroDiagnostic.InvalidSchemaName(property.Value.GetSourceSpan());
    }

    extension(JsonObjectSyntax syntax)
    {
        public Result<JsonPropertySyntax> GetRequiredProperty(string propertyName) =>
            syntax.Property(propertyName) is JsonPropertySyntax property ? property : AvroDiagnostic.MissingProperty(syntax.GetSourceSpan(), propertyName);

        public Result<Option<JsonArraySyntax>> GetErrors() => syntax.GetValue(AvroJsonKeys.Errors) switch
        {
            None or JsonNullSyntax => Option.None<JsonArraySyntax>(),
            JsonArraySyntax errors => Option.Some(errors),
            JsonValueSyntax value => AvroDiagnostic.InvalidErrors(value.GetSourceSpan())
        };

        private Option<JsonValueSyntax> GetValue(string key) => syntax.Property(key).ToOption().Map(static property => property.Value);

        private Result<JsonValueSyntax> GetRequiredValue(string key) =>
            syntax.GetRequiredProperty(key).Map(static property => property.Value);

        public Result<JsonValueSyntax> GetFieldType() => syntax.GetRequiredValue(AvroJsonKeys.Type);

        public Result<JsonValueSyntax> GetParameterType() => syntax.GetRequiredValue(AvroJsonKeys.Type);

        public Result<JsonValueSyntax> GetItems() => syntax.GetRequiredValue(AvroJsonKeys.Items);

        public Result<JsonValueSyntax> GetValues() => syntax.GetRequiredValue(AvroJsonKeys.Values);

        public Result<JsonValueSyntax> GetResponse() => syntax.GetRequiredValue(AvroJsonKeys.Response);

        public Result<JsonArraySyntax> GetFields()
        {
            var property = syntax.GetRequiredProperty(AvroJsonKeys.Fields);
            if (property.IsError) return property.GetError();
            var value = property.GetValue().Value;
            return value is JsonArraySyntax collection ? collection : AvroDiagnostic.InvalidFields(value.GetSourceSpan());
        }

        public Result<JsonArraySyntax> GetTypes()
        {
            var property = syntax.GetRequiredProperty(AvroJsonKeys.Types);
            if (property.IsError) return property.GetError();
            var value = property.GetValue().Value;
            return value is JsonArraySyntax collection ? collection : AvroDiagnostic.InvalidTypes(value.GetSourceSpan());
        }

        public Result<JsonObjectSyntax> GetMessages()
        {
            var property = syntax.GetRequiredProperty(AvroJsonKeys.Messages);
            if (property.IsError) return property.GetError();
            var value = property.GetValue().Value;
            return value is JsonObjectSyntax collection ? collection : AvroDiagnostic.InvalidMessages(value.GetSourceSpan());
        }

        public Result<JsonArraySyntax> GetRequest()
        {
            var property = syntax.GetRequiredProperty(AvroJsonKeys.Request);
            if (property.IsError) return property.GetError();
            var value = property.GetValue().Value;
            return value is JsonArraySyntax collection ? collection : AvroDiagnostic.InvalidRequest(value.GetSourceSpan());
        }

        public Result<JsonArraySyntax> GetSymbols()
        {
            var property = syntax.GetRequiredProperty(AvroJsonKeys.Symbols);
            if (property.IsError) return property.GetError();
            var value = property.GetValue().Value;
            return value is JsonArraySyntax collection ? collection : AvroDiagnostic.InvalidSymbols(value.GetSourceSpan());
        }

        public Result<Option<bool>> GetOneWay() => syntax.GetValue(AvroJsonKeys.OneWay) switch
        {
            None or JsonNullSyntax => Option.None<bool>(),
            JsonBooleanSyntax boolean => Option.Some(boolean.Value),
            JsonValueSyntax value => AvroDiagnostic.InvalidOneWay(value.GetSourceSpan())
        };

        public Result<Option<string>> GetNamespace() => syntax.GetValue(AvroJsonKeys.Namespace).AsNamespace();

        public Result<MemberName> GetFieldName()
        {
            var property = syntax.GetRequiredProperty(AvroJsonKeys.Name);
            if (property.IsError) return property.GetError();
            var value = property.GetValue().Value;
            return value.GetStringValue() is string name && NameValidator.IsValidMemberName(name)
                ? new MemberName(name)
                : AvroDiagnostic.InvalidFieldName(value.GetSourceSpan());
        }

        public Result<MemberName> GetParameterName()
        {
            var property = syntax.GetRequiredProperty(AvroJsonKeys.Name);
            if (property.IsError) return property.GetError();
            var value = property.GetValue().Value;
            return value.GetStringValue() is string name && NameValidator.IsValidMemberName(name)
                ? new MemberName(name)
                : AvroDiagnostic.InvalidRequestParameterName(value.GetSourceSpan());
        }

        public Result<Option<string>> GetLogicalTypeName() => syntax.GetValue(AvroJsonKeys.LogicalType).AsLogicalTypeName();

        public Result<Option<string>> GetOrder() => syntax.GetValue(AvroJsonKeys.Order).AsOrder();

        public Result<Option<string>> GetDocumentation()
        {
            if (syntax.GetValue(AvroJsonKeys.Doc) is not JsonValueSyntax value || value is JsonNullSyntax) return Option.None<string>();
            if (value.GetStringValue() is not string text) return AvroDiagnostic.InvalidDoc(value.GetSourceSpan());
            return string.IsNullOrWhiteSpace(text) ? Option.None<string>() : Option.Some(text);
        }

        public Result<Option<string>> GetEnumDefault() => syntax.GetValue(AvroJsonKeys.Default).AsEnumDefault();

        public Result<Option<int>> GetDecimalPrecision() => syntax.GetValue(AvroJsonKeys.Precision).AsDecimalPrecision();

        public Result<Option<int>> GetDecimalScale() => syntax.GetValue(AvroJsonKeys.Scale).AsDecimalScale();

        public Result<ImmutableSortedDictionary<string, JsonElement>> GetSchemaProperties() =>
            syntax.GetProperties(ReservedSchemaProperties.IsReserved);

        public Result<ImmutableSortedDictionary<string, JsonElement>> GetProtocolProperties() =>
            syntax.GetProperties(ReservedProtocolProperties.IsReserved);

        private Result<ImmutableSortedDictionary<string, JsonElement>> GetProperties(Func<string, bool> isReserved)
        {
            ImmutableSortedDictionary<string, JsonElement>.Builder? properties = null;
            foreach (var property in syntax.Properties)
            {
                var name = property.Name.ValueText;
                if (isReserved(name)) continue;
                // Preserve the existing dictionary value equality; JSON content equality is a separate contract.
                // ReSharper disable once UsageOfDefaultStructEquality
                properties ??= ImmutableSortedDictionary.CreateBuilder<string, JsonElement>(StringComparer.Ordinal);
                properties[name] = property.Value.AsJsonElement();
            }
            return properties?.ToImmutable() ?? ImmutableSortedDictionary<string, JsonElement>.Empty;
        }

        public Result<ImmutableArray<string>> GetAliases() => syntax.GetValue(AvroJsonKeys.Aliases).AsAliases();

        public Result<int> GetFixedSize()
        {
            var property = syntax.GetRequiredProperty(AvroJsonKeys.Size);
            if (property.IsError) return property.GetError();
            var value = property.GetValue().Value;
            return value is JsonNumberSyntax { Int32Value: int size and > 0 } ? size : AvroDiagnostic.InvalidFixedSize(value.GetSourceSpan());
        }

        public Result<Option<DefaultValue>> GetDefaultValue() => syntax.Property(AvroJsonKeys.Default) is JsonPropertySyntax property
            ? Option.Some<DefaultValue>(new UnboundDefaultValue(property.Value.AsJsonElement()))
            : Option.None<DefaultValue>();
    }

    extension(JsonStringSyntax syntax)
    {
        public Result<SchemaName> AsSchemaReference() => syntax.Value is string name && NameValidator.IsValidName(name)
            ? SchemaName.FromFullName(name)
            : AvroDiagnostic.InvalidSchemaReference(syntax.GetSourceSpan(), syntax.Value ?? syntax.GetRawText().Trim('"').ToString());
    }
}
