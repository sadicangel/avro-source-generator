using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Schemas.Defaults;
using AvroSourceGenerator.Syntax;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Serialization;

public sealed class AvdlWriter(Stream stream, IReadOnlyDictionary<SchemaName, TopLevelSchema>? schemaIndex = null, CancellationToken cancellationToken = default) : IDisposable
{
    private readonly Utf8IndentedTextWriter _writer = new Utf8IndentedTextWriter(stream);
    private readonly HashSet<SchemaName> _writtenSchemas = [];

    internal static string WriteToString(AvroSchema schema, IReadOnlyDictionary<SchemaName, TopLevelSchema>? schemaIndex, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        using var writer = new AvdlWriter(stream, schemaIndex, cancellationToken);
        writer.Write(schema);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.GetBuffer(), 0, checked((int)stream.Length));
    }

    public void Dispose() => _writer.Dispose();

    public void Flush() => _writer.Flush();

    public void Write(AvroSchema schema)
    {
        if (schema is not ProtocolSchema protocol)
            WriteSchema(schema);
        else
            WriteProtocol(protocol);
    }

    private void WriteSchema(AvroSchema schema)
    {
        var @namespace = schema.SchemaName.Namespace;
        if (@namespace is { Length: > 0 })
            _writer.WriteLine($"namespace {EscapeName(@namespace)};");
        _writer.Write("schema ");
        WriteType(schema, @namespace);
        _writer.WriteLine(';');
        WriteSchemaDeclarations(schema, @namespace, []);
    }

    private void WriteSchemaDeclarations(AvroSchema schema, string? containingNamespace, HashSet<SchemaName> visited)
    {
        cancellationToken.ThrowIfCancellationRequested();
        switch (schema)
        {
            case AvroSchemaReference reference when schemaIndex?.TryGetValue(reference.SchemaName, out var target) == true:
                WriteSchemaDeclarations(target, containingNamespace, visited);
                break;
            case ArraySchema array:
                WriteSchemaDeclarations(array.ItemSchema, containingNamespace, visited);
                break;
            case MapSchema map:
                WriteSchemaDeclarations(map.ValueSchema, containingNamespace, visited);
                break;
            case UnionSchema union:
                foreach (var branch in union.Schemas)
                    WriteSchemaDeclarations(branch, containingNamespace, visited);
                break;
            case NamedSchema named when visited.Add(named.SchemaName):
                var fields = named switch
                {
                    RecordSchema record => record.Fields,
                    ErrorSchema error => error.Fields,
                    _ => [],
                };
                foreach (var field in fields)
                    WriteSchemaDeclarations(field.Type, containingNamespace, visited);
                WriteDeclaration(named, containingNamespace);
                break;
        }
    }

    private void WriteType(AvroSchema schema, string? containingNamespace)
    {
        cancellationToken.ThrowIfCancellationRequested();
        switch (schema)
        {
            case PrimitiveSchema primitive:
                _writer.Write(primitive.SchemaName.Name);
                break;

            case ArraySchema array:
                _writer.Write("array<");
                WriteType(array.ItemSchema, containingNamespace);
                _writer.Write('>');
                break;

            case MapSchema map:
                _writer.Write("map<");
                WriteType(map.ValueSchema, containingNamespace);
                _writer.Write('>');
                break;

            case LogicalSchema logical:
                WriteLogicalType(logical, containingNamespace);
                break;

            case UnionSchema union when union.Schemas is [_, { SchemaType: SchemaType.Null }] or [{ SchemaType: SchemaType.Null }, _]:
                WriteType(union.UnderlyingSchema, containingNamespace);
                _writer.Write('?');
                break;

            case UnionSchema union:
                _writer.Write("union { ");
                foreach (var (index, branch) in union.Schemas.Index())
                {
                    WriteType(branch, containingNamespace);
                    if (index + 1 < union.Schemas.Length)
                        _writer.Write(", ");
                }
                _writer.Write(" }");
                break;

            case NamedSchema or AvroSchemaReference:
                _writer.Write(EscapeName(schema.SchemaName.Namespace == containingNamespace ? schema.SchemaName.Name : schema.SchemaName.FullName));
                break;

            default:
                throw new NotSupportedException(schema.GetType().Name);
        }
    }

    private void WriteLogicalType(LogicalSchema schema, string? containingNamespace)
    {
        var keyword = schema.SchemaName.Name switch
        {
            LogicalTypeNames.Date => "date",
            LogicalTypeNames.TimeMillis => "time_ms",
            LogicalTypeNames.TimestampMillis => "timestamp_ms",
            LogicalTypeNames.LocalTimestampMillis => "local_timestamp_ms",
            LogicalTypeNames.Uuid => "uuid",
            LogicalTypeNames.Decimal when schema.Properties.TryGetValue(AvroJsonKeys.Precision, out var precision) &&
                schema.Properties.TryGetValue(AvroJsonKeys.Scale, out var scale) => $"decimal({precision.GetRawText()}, {scale.GetRawText()})",
            _ => null,
        };
        if (keyword is not null)
        {
            _writer.Write(keyword);
            return;
        }

        _writer.Write($"@logicalType({JsonSerializer.Serialize(schema.SchemaName.Name)}) ");
        foreach (var (name, value) in schema.Properties)
            _writer.Write($"@{name}({value.GetRawText()}) ");
        WriteType(schema.UnderlyingSchema, containingNamespace);
    }

    private void WriteDeclaration(NamedSchema schema, string? containingNamespace)
    {
        if (!_writtenSchemas.Add(schema.SchemaName))
            return;

        WriteDocumentation(schema.Documentation);
        WriteProperties(schema.Properties);
        WriteAliases(schema.Aliases);
        WriteNamespace(schema.SchemaName.Namespace, containingNamespace);
        _writer.WriteLine($"{GetSyntaxForType(schema.SchemaType)} {EscapeName(schema.SchemaName.Name)}");
        switch (schema)
        {
            case EnumSchema x: WriteEnum(x); break;
            case FixedSchema x: WriteFixed(x); break;
            case RecordSchema x: WriteRecord(x, containingNamespace); break;
            case ErrorSchema x: WriteError(x, containingNamespace); break;
        }
    }

    private void WriteEnum(EnumSchema schema)
    {
        using var block = _writer.WriteBlock(BraceStyle.SameLine);
        foreach (var (index, symbol) in schema.Symbols.Index())
        {
            _writer.Write(EscapeName(symbol.SchemaName));
            if (index + 1 < schema.Symbols.Length)
                _writer.Write(',');
            _writer.WriteLine();
        }
    }

    private void WriteFixed(FixedSchema schema) =>
        _writer.WriteLine($"({schema.Size});");

    private void WriteRecord(RecordSchema schema, string? containingNamespace) =>
        WriteRecordOrError(schema, schema.Fields, containingNamespace);

    private void WriteError(ErrorSchema schema, string? containingNamespace) =>
        WriteRecordOrError(schema, schema.Fields, containingNamespace);

    private void WriteRecordOrError(NamedSchema schema, ImmutableArray<Field> fields, string? containingNamespace)
    {
        using var block = _writer.WriteBlock(BraceStyle.SameLine);
        var @namespace = schema.SchemaName.Namespace ?? containingNamespace;
        foreach (var field in fields)
            WriteField(field, @namespace);
    }

    private void WriteField(Field field, string? containingNamespace)
    {
        WriteDocumentation(field.Documentation);
        WriteProperties(field.Properties);
        WriteAliases(field.Aliases);
        WriteOrder(field.Order);
        WriteType(field.Type, containingNamespace);
        _writer.Write($" {EscapeName(field.Name.SchemaName)}");
        WriteDefault(field.Default);
        _writer.WriteLine(';');
    }

    private void WriteProtocol(ProtocolSchema protocol)
    {
        WriteDocumentation(protocol.Documentation);
        var @namespace = protocol.SchemaName.Namespace;
        WriteNamespace(@namespace, containingNamespace: null);
        _writer.WriteLine($"protocol {protocol.SchemaName.Name}");
        using var block = _writer.WriteBlock(BraceStyle.SameLine);
        foreach (var schema in protocol.Types)
            WriteDeclaration(schema, @namespace);
        foreach (var message in protocol.Messages)
            WriteMessage(message, @namespace);
    }

    private void WriteMessage(ProtocolMessage message, string? containingNamespace)
    {
        WriteDocumentation(message.Documentation);
        WriteType(message.Response.Type, containingNamespace);
        _writer.Write($" {message.MethodName.SchemaName}(");
        foreach (var (index, parameter) in message.RequestParameters.Index())
        {
            WriteType(parameter.Type, containingNamespace);
            _writer.Write($" {parameter.Name.SchemaName}");
            if (index + 1 < message.RequestParameters.Length)
                _writer.Write(", ");
        }
        _writer.Write(')');
        WriteThrows(message.Errors, containingNamespace);
        WriteOneWay(message.OneWay);
        _writer.WriteLine(';');
    }

    private void WriteThrows(AvroSchema? errors, string? containingNamespace)
    {
        if (errors is null) return;

        _writer.Write(" throws ");
        if (errors is UnionSchema union)
        {
            foreach (var (index, schema) in union.Schemas.Index())
            {
                WriteType(schema, containingNamespace);
                if (index + 1 < union.Schemas.Length)
                    _writer.Write(", ");
            }
        }
        else
        {
            WriteType(errors, containingNamespace);
        }
    }

    private void WriteOneWay(bool? oneWay)
    {
        if (oneWay is not true) return;
        _writer.Write(" oneway");
    }

    private void WriteDefault(DefaultValue? defaultValue)
    {
        switch (defaultValue)
        {
            case null:
                return;
            case UnboundDefaultValue { JsonText: string jsonText }:
                _writer.Write($" = {jsonText}");
                return;
            default:
                throw new InvalidOperationException($"Unexpected DefaultValue {defaultValue.GetType().Name}");
        }
    }

    private void WriteNamespace(string? schemaNamespace, string? containingNamespace)
    {
        if (schemaNamespace is null || schemaNamespace == containingNamespace) return;
        _writer.WriteLine($"@namespace(\"{schemaNamespace}\")");
    }

    private string GetSyntaxForType(SchemaType schemaType) => schemaType switch
    {
        SchemaType.Null => "null",
        SchemaType.Boolean => "boolean",
        SchemaType.Int => "int",
        SchemaType.Long => "long",
        SchemaType.Float => "float",
        SchemaType.Double => "double",
        SchemaType.Bytes => "bytes",
        SchemaType.String => "string",
        SchemaType.Array => "array",
        SchemaType.Map => "map",
        SchemaType.Union => "union",
        SchemaType.Enum => "enum",
        SchemaType.Fixed => "fixed",
        SchemaType.Record => "record",
        SchemaType.Error => "error",
        SchemaType.Protocol => "protocol",
        SchemaType.Logical or
        SchemaType.Reference or
        SchemaType.UnionType or
        _ => throw new InvalidOperationException($"Unsupported SchemaType {schemaType}"),
    };


    private void WriteDocumentation(string? documentation)
    {
        if (documentation is not { Length: > 0 }) return;
        _writer.WriteLine($"/**{documentation.Replace("*/", "* /")}*/");
    }

    private static string EscapeName(string name)
    {
        StringBuilder? builder = null;
        var position = 0;
        var unchangedStart = 0;
        foreach (var part in new SplitEnumerable(name, '.'))
        {
            if (SyntaxFacts.GetKeywordKind(part) != SyntaxKind.IdentifierToken)
            {
                builder ??= new StringBuilder(name.Length);
                builder.Append(name.AsSpan(unchangedStart, position - unchangedStart));
                builder.Append('`').Append(part).Append('`');
                unchangedStart = position + part.Length;
            }
            position += part.Length + 1;
        }

        return builder is null ? name : builder.Append(name.AsSpan(unchangedStart)).ToString();
    }

    private void WriteAliases(ImmutableArray<string> aliases)
    {
        if (aliases.IsDefaultOrEmpty) return;
        _writer.Write("@aliases([");
        foreach (var (index, alias) in aliases.Index())
        {
            _writer.Write($"\"{alias}\"");
            if (index + 1 < aliases.Length)
                _writer.Write(", ");
        }
        _writer.WriteLine("])");
    }

    private void WriteOrder(string? order)
    {
        if (string.IsNullOrEmpty(order)) return;
        _writer.WriteLine($"@order(\"{order}\")");
    }

    private void WriteProperties(ImmutableSortedDictionary<string, JsonElement> properties)
    {
        foreach (var (name, value) in properties)
            _writer.WriteLine($"@{name}({value.GetRawText()})");
    }
}
