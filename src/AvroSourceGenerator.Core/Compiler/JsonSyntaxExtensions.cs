using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Avjs;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Schemas.Defaults;

namespace AvroSourceGenerator.Compiler;

internal static class JsonSyntaxExtensions
{
    extension(JsonSyntax syntax)
    {
        public Result<JsonObjectSyntax> AsObject() =>
            syntax is JsonObjectSyntax obj ? obj : AvroDiagnostic.InvalidProperty(syntax);

        public Result<JsonArraySyntax> AsArray() =>
            syntax is JsonArraySyntax array ? array : AvroDiagnostic.InvalidProperty(syntax);

        public Result<JsonValueSyntax> AsValue() =>
            syntax is JsonValueSyntax value ? value : AvroDiagnostic.InvalidProperty(syntax);
    }

    extension(JsonObjectSyntax syntax)
    {
        public Result<JsonPropertySyntax> GetRequiredProperty(string propertyName) =>
            syntax.Property(propertyName) is { } property ? property : AvroDiagnostic.MissingProperty(syntax, propertyName);

        public Result<Option<JsonPropertySyntax>> GetOptionalProperty(string propertyName) =>
            syntax.Property(propertyName) is { Value.TokenType: not JsonTokenType.Null } property ? property : Option.None<JsonPropertySyntax>();

        public Result<string> GetRequiredString(string propertyName) =>
            syntax.GetRequiredProperty(propertyName).Bind(GetRequiredString);

        public Result<Option<string>> GetOptionalNullableString(string propertyName) =>
            syntax.GetOptionalProperty(propertyName).TraverseBind(GetNullableString);

        public Result<Option<bool>> GetOptionalBoolean(string propertyName) =>
            syntax.GetOptionalProperty(propertyName).Traverse(GetBoolean);

        public Result<JsonObjectSyntax> GetRequiredObject(string propertyName) =>
            syntax.GetRequiredProperty(propertyName).Bind(GetValueAsObject);

        public Result<JsonArraySyntax> GetRequiredArray(string propertyName) =>
            syntax.GetRequiredProperty(propertyName).Bind(GetValueAsArray);

        public Result<Option<JsonArraySyntax>> GetOptionalArray(string propertyName) =>
            syntax.GetOptionalProperty(propertyName).Traverse(GetValueAsArray);

        public Result<JsonValueSyntax> GetRequiredValue(string propertyName) =>
            syntax.GetRequiredProperty(propertyName).Bind(GetValueAsValue);

        //////////////
        public Result<SchemaName> GetSchemaName(string? containingNamespace) =>
            syntax.GetAvroName(AvroJsonKeys.Name, containingNamespace);

        public Result<SchemaName> GetProtocolName(string? containingNamespace) =>
            syntax.GetAvroName(AvroJsonKeys.Protocol, containingNamespace);

        private Result<SchemaName> GetAvroName(string propertyName, string? containingNamespace)
        {
            var name = syntax.GetRequiredProperty(propertyName).Bind(GetValueAsValue);
            if (name.IsError) return name.GetError();
            if (name.GetValue().StringValue?.Contains('.') is true)
                return name.GetValue().AsSchemaName(containingNamespace: null);
            var @namespace = syntax.GetOptionalProperty(AvroJsonKeys.Namespace).Traverse(GetValueAsValue).Traverse(AsNamespace);
            if (@namespace.IsError) return @namespace.GetError();
            return name.GetValue().AsSchemaName(@namespace.GetValue().GetValueOrDefault() ?? containingNamespace);
        }
        public Result<MemberName> GetMemberName() =>
            syntax.GetRequiredProperty(AvroJsonKeys.Name).Bind(GetValueAsValue).Bind(AsMemberName);

        public Result<Option<string>> GetLogicalTypeName() => syntax.Property(AvroJsonKeys.LogicalType) is null
            ? Option.None<string>()
            : syntax.GetRequiredString(AvroJsonKeys.LogicalType).Map(Option.Some);

        public Result<Option<string>> GetDocumentation() =>
            syntax.GetOptionalNullableString(AvroJsonKeys.Doc);

        public Result<ImmutableSortedDictionary<string, JsonElement>> GetSchemaProperties() =>
            syntax.GetProperties(ReservedSchemaProperties.IsReserved);

        public Result<ImmutableSortedDictionary<string, JsonElement>> GetProtocolProperties() =>
            syntax.GetProperties(ReservedProtocolProperties.IsReserved);

        private Result<ImmutableSortedDictionary<string, JsonElement>> GetProperties(Func<string, bool> isReserved)
        {
            ImmutableSortedDictionary<string, JsonElement>.Builder? properties = null;
            foreach (var (name, value) in syntax.Properties)
            {
                if (isReserved(name.Value)) continue;
                // Preserve the existing dictionary value equality; JSON content equality is a separate contract.
                // ReSharper disable once UsageOfDefaultStructEquality
                properties ??= ImmutableSortedDictionary.CreateBuilder<string, JsonElement>(StringComparer.Ordinal);
                properties[name.Value] = value.AsJsonElement();
            }
            return properties?.ToImmutable() ?? ImmutableSortedDictionary<string, JsonElement>.Empty;
        }

        public Result<ImmutableArray<string>> GetAliases() => syntax.GetOptionalArray(AvroJsonKeys.Aliases)
            .Traverse(static array => array.AsStringArray(static alias => !string.IsNullOrWhiteSpace(alias)))
            .Map(static option => option.GetValueOrDefault([]));

        public Result<int> GetFixedSize() => syntax.GetRequiredValue(AvroJsonKeys.Size).Bind<int>(static syntax => syntax.AsInt32() switch
        {
            int size and > 0 => size,
            AvroDiagnostic diagnostic => diagnostic,
            _ => AvroDiagnostic.InvalidProperty(syntax)
        });

        public Result<Option<DefaultValue>> GetDefaultValue() => syntax.Property(AvroJsonKeys.Default) is { } property
            ? Option.Some<DefaultValue>(new UnboundDefaultValue(property.Value.AsJsonElement()))
            : Option.None<DefaultValue>();
    }

    extension(JsonArraySyntax syntax)
    {
        public Result<ImmutableArray<string>> AsStringArray(Func<string?, bool>? validate = null)
        {
            var builder = ImmutableArray.CreateBuilder<string>(syntax.Items.Length);
            foreach (var item in syntax.Items)
            {
                switch (item.AsValue().Bind(AsString))
                {
                    case string text when validate?.Invoke(text) is false:
                        return AvroDiagnostic.InvalidProperty(syntax);
                    case string text:
                        builder.Add(text);
                        break;
                    case AvroDiagnostic diagnostic:
                        return diagnostic;
                }
            }
            return builder.MoveToImmutable();
        }
    }

    extension(JsonValueSyntax syntax)
    {
        public Result<string> AsString() =>
            syntax.StringValue is { } text && !string.IsNullOrWhiteSpace(text) ? text : AvroDiagnostic.InvalidProperty(syntax);

        public Result<int> AsInt32() =>
            syntax.Int32Value is { } value ? value : AvroDiagnostic.InvalidProperty(syntax);

        public Result<SchemaName> AsSchemaName(string? containingNamespace)
        {
            var maybeName = syntax.AsString();
            if (maybeName.IsError)
                return maybeName.GetError();

            var isQualified = maybeName.GetValue().TrySplitQualifiedName(out var localName, out var @namespace);
            if (!NameValidator.IsValidName(localName))
                return AvroDiagnostic.InvalidProperty(syntax);
            if (!isQualified)
                return new SchemaName(localName, containingNamespace);
            if (!NameValidator.IsValidName(@namespace))
                return AvroDiagnostic.InvalidProperty(syntax);
            return new SchemaName(localName, @namespace);
        }

        public Result<string> AsNamespace() =>
            syntax.StringValue is { } @namespace && (@namespace.Length == 0 || NameValidator.IsValidName(@namespace))
                ? @namespace
                : AvroDiagnostic.InvalidProperty(syntax);

        public Result<MemberName> AsMemberName() => syntax.AsString().Bind<MemberName>(name =>
        {
            var isQualified = name.TrySplitQualifiedName(out var localName, out var @namespace);
            if (isQualified || !NameValidator.IsValidName(localName))
                return AvroDiagnostic.InvalidProperty(syntax);
            return new MemberName(localName);
        });

    }

    extension(JsonPropertySyntax property)
    {
        public Result<bool> GetBoolean() => property.Value.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            _ => AvroDiagnostic.InvalidProperty(property)
        };

        public Result<string> GetRequiredString() => property.Value.AsValue().Bind(AsString);

        public Result<Option<string>> GetNullableString() => property.Value.AsValue() switch
        {
            JsonValueSyntax { TokenType: JsonTokenType.String or JsonTokenType.Null } value => !string.IsNullOrWhiteSpace(value.StringValue) ? value.StringValue! : Option.None<string>(),
            JsonValueSyntax => AvroDiagnostic.InvalidProperty(property),
            AvroDiagnostic diagnostic => diagnostic
        };

        public Result<JsonObjectSyntax> GetValueAsObject() =>
            property.Value is JsonObjectSyntax obj ? obj : AvroDiagnostic.InvalidProperty(property);

        public Result<JsonArraySyntax> GetValueAsArray() =>
            property.Value is JsonArraySyntax array ? array : AvroDiagnostic.InvalidProperty(property);

        public Result<JsonValueSyntax> GetValueAsValue() =>
            property.Value is JsonValueSyntax value ? value : AvroDiagnostic.InvalidProperty(property);
    }
}

internal static class NameValidator
{
    private static bool IsValidPart(ReadOnlySpan<char> part)
    {
        if (part.IsWhiteSpace() || !(char.IsAsciiLetter(part[0]) || part[0] is '_'))
            return false;

        for (var i = 0; i < part.Length; i++)
        {
            if (!(char.IsAsciiLetterOrDigit(part[i]) || part[i] is '_'))
                return false;
        }

        return true;
    }

    public static bool IsValidName(string? name) => IsValidName(name.AsSpan());

    public static bool IsValidName(ReadOnlySpan<char> name)
    {
        if (name.IsWhiteSpace())
            return false;

        foreach (var part in new SplitEnumerable(name, '.'))
        {
            if (!IsValidPart(part))
                return false;
        }

        return true;
    }
}
