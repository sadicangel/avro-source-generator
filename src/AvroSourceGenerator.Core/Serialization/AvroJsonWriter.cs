using System.Text;
using System.Text.Json;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Schemas.Defaults;

namespace AvroSourceGenerator.Serialization;

public sealed class AvroJsonWriter(Stream stream, IReadOnlyDictionary<SchemaName, TopLevelSchema> schemaIndex, JsonWriterOptions options = default) : IDisposable
{
    private readonly Utf8JsonWriter _writer = new Utf8JsonWriter(stream, options);
    private readonly HashSet<SchemaName> _writtenSchemas = [];

    internal static string WriteToString(AvroSchema schema, IReadOnlyDictionary<SchemaName, TopLevelSchema> schemaIndex, JsonWriterOptions options)
    {
        using var stream = new MemoryStream();
        using var writer = new AvroJsonWriter(stream, schemaIndex, options);
        writer.Write(schema);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.GetBuffer(), 0, checked((int)stream.Length));
    }

    public void Dispose() => _writer.Dispose();

    public void Write(AvroSchema schema) => Write(schema, schema.SchemaName.Namespace);

    public void Flush() => _writer.Flush();

    private void Write(AvroSchema schema, string? containingNamespace)
    {
        switch (schema)
        {
            case PrimitiveSchema x: WritePrimitive(x); break;
            case ArraySchema x: WriteArray(x, containingNamespace); break;
            case MapSchema x: WriteMap(x, containingNamespace); break;
            case UnionSchema x: WriteUnion(x, containingNamespace); break;
            case EnumSchema x: WriteEnum(x, containingNamespace); break;
            case FixedSchema x: WriteFixed(x, containingNamespace); break;
            case RecordSchema x: WriteRecord(x, containingNamespace); break;
            case ErrorSchema x: WriteError(x, containingNamespace); break;
            case ProtocolSchema x: WriteProtocol(x); break;
            case LogicalSchema x: WriteLogical(x, containingNamespace); break;
            case AvroSchemaReference x: WriteSchemaReference(x, containingNamespace); break;
            // Generated C# union types have no Avro JSON representation.
            case UnionTypeSchema: break;
            default: throw new InvalidOperationException($"Unsupported Avro schema type: {schema.GetType().Name}");
        }
    }

    private void WritePrimitive(PrimitiveSchema schema)
    {
        if (schema.Properties.IsEmpty)
        {
            _writer.WriteStringValue(schema.SchemaName.Name);
        }
        else
        {
            _writer.WriteStartObject();
            _writer.WritePropertyName(AvroJsonKeys.Type);
            _writer.WriteStringValue(schema.SchemaName.Name);
            foreach (var entry in schema.Properties)
            {
                _writer.WritePropertyName(entry.Key);
                entry.Value.WriteTo(_writer);
            }

            _writer.WriteEndObject();
        }
    }

    private void WriteArray(ArraySchema schema, string? containingNamespace)
    {
        _writer.WriteStartObject();
        _writer.WriteString(AvroJsonKeys.Type, AvroTypeNames.Array);
        _writer.WritePropertyName(AvroJsonKeys.Items);
        Write(schema.ItemSchema, containingNamespace);
        foreach (var entry in schema.Properties)
        {
            _writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(_writer);
        }
        _writer.WriteEndObject();
    }

    private void WriteMap(MapSchema schema, string? containingNamespace)
    {
        _writer.WriteStartObject();
        _writer.WriteString(AvroJsonKeys.Type, AvroTypeNames.Map);
        _writer.WritePropertyName(AvroJsonKeys.Values);
        Write(schema.ValueSchema, containingNamespace);
        foreach (var entry in schema.Properties)
        {
            _writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(_writer);
        }
        _writer.WriteEndObject();
    }

    private void WriteUnion(UnionSchema schema, string? containingNamespace)
    {
        _writer.WriteStartArray();
        foreach (var branch in schema.Schemas)
            Write(branch, containingNamespace);
        _writer.WriteEndArray();
    }

    private void WriteEnum(EnumSchema schema, string? containingNamespace)
    {
        if (!_writtenSchemas.Add(schema.SchemaName))
        {
            WriteReferenceName(schema.SchemaName, containingNamespace);
            return;
        }

        _writer.WriteStartObject();
        _writer.WriteString(AvroJsonKeys.Type, AvroTypeNames.Enum);
        _writer.WriteString(AvroJsonKeys.Name, schema.SchemaName.Name);
        var @namespace = schema.SchemaName.Namespace ?? containingNamespace;
        if (@namespace is not null)
            _writer.WriteString(AvroJsonKeys.Namespace, @namespace);
        if (schema.Documentation is not null)
            _writer.WriteString(AvroJsonKeys.Doc, schema.Documentation);
        if (schema.Aliases.Length > 0)
        {
            _writer.WriteStartArray(AvroJsonKeys.Aliases);
            foreach (var alias in schema.Aliases)
                _writer.WriteStringValue(alias);
            _writer.WriteEndArray();
        }
        _writer.WriteStartArray(AvroJsonKeys.Symbols);
        foreach (var symbol in schema.Symbols)
            _writer.WriteStringValue(symbol.SchemaName);
        _writer.WriteEndArray();
        if (schema.Default is not null)
            _writer.WriteString(AvroJsonKeys.Default, schema.Default);
        foreach (var entry in schema.Properties)
        {
            _writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(_writer);
        }
        _writer.WriteEndObject();
    }

    private void WriteFixed(FixedSchema schema, string? containingNamespace)
    {
        if (!_writtenSchemas.Add(schema.SchemaName))
        {
            WriteReferenceName(schema.SchemaName, containingNamespace);
            return;
        }

        _writer.WriteStartObject();
        _writer.WriteString(AvroJsonKeys.Type, AvroTypeNames.Fixed);
        _writer.WriteString(AvroJsonKeys.Name, schema.SchemaName.Name);
        var @namespace = schema.SchemaName.Namespace ?? containingNamespace;
        if (@namespace is not null)
            _writer.WriteString(AvroJsonKeys.Namespace, @namespace);
        if (schema.Documentation is not null)
            _writer.WriteString(AvroJsonKeys.Doc, schema.Documentation);
        if (schema.Aliases.Length > 0)
        {
            _writer.WriteStartArray(AvroJsonKeys.Aliases);
            foreach (var alias in schema.Aliases)
                _writer.WriteStringValue(alias);
            _writer.WriteEndArray();
        }
        _writer.WriteNumber(AvroJsonKeys.Size, schema.Size);
        foreach (var entry in schema.Properties)
        {
            _writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(_writer);
        }
        _writer.WriteEndObject();
    }

    private void WriteRecord(RecordSchema schema, string? containingNamespace)
    {
        if (!_writtenSchemas.Add(schema.SchemaName))
        {
            WriteReferenceName(schema.SchemaName, containingNamespace);
            return;
        }

        _writer.WriteStartObject();
        _writer.WriteString(AvroJsonKeys.Type, AvroTypeNames.Record);
        _writer.WriteString(AvroJsonKeys.Name, schema.SchemaName.Name);
        var @namespace = schema.SchemaName.Namespace ?? containingNamespace;
        if (@namespace is not null)
            _writer.WriteString(AvroJsonKeys.Namespace, @namespace);
        if (schema.Documentation is not null)
            _writer.WriteString(AvroJsonKeys.Doc, schema.Documentation);
        if (schema.Aliases.Length > 0)
        {
            _writer.WriteStartArray(AvroJsonKeys.Aliases);
            foreach (var alias in schema.Aliases)
                _writer.WriteStringValue(alias);
            _writer.WriteEndArray();
        }
        _writer.WriteStartArray(AvroJsonKeys.Fields);
        foreach (var field in schema.Fields)
            WriteField(field, @namespace);
        _writer.WriteEndArray();
        foreach (var entry in schema.Properties)
        {
            _writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(_writer);
        }
        _writer.WriteEndObject();
    }

    private void WriteError(ErrorSchema schema, string? containingNamespace)
    {
        if (!_writtenSchemas.Add(schema.SchemaName))
        {
            WriteReferenceName(schema.SchemaName, containingNamespace);
            return;
        }

        _writer.WriteStartObject();
        _writer.WriteString(AvroJsonKeys.Type, AvroTypeNames.Error);
        _writer.WriteString(AvroJsonKeys.Name, schema.SchemaName.Name);
        var @namespace = schema.SchemaName.Namespace ?? containingNamespace;
        if (@namespace is not null)
            _writer.WriteString(AvroJsonKeys.Namespace, @namespace);
        if (schema.Documentation is not null)
            _writer.WriteString(AvroJsonKeys.Doc, schema.Documentation);
        if (schema.Aliases.Length > 0)
        {
            _writer.WriteStartArray(AvroJsonKeys.Aliases);
            foreach (var alias in schema.Aliases)
                _writer.WriteStringValue(alias);
            _writer.WriteEndArray();
        }
        _writer.WriteStartArray(AvroJsonKeys.Fields);
        foreach (var field in schema.Fields)
            WriteField(field, @namespace);
        _writer.WriteEndArray();
        foreach (var entry in schema.Properties)
        {
            _writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(_writer);
        }
        _writer.WriteEndObject();
    }

    private void WriteField(Field field, string? containingNamespace)
    {
        _writer.WriteStartObject();
        _writer.WriteString(AvroJsonKeys.Name, field.Name.SchemaName);
        _writer.WritePropertyName(AvroJsonKeys.Type);
        Write(field.Type, containingNamespace);
        if (field.Documentation is not null)
            _writer.WriteString(AvroJsonKeys.Doc, field.Documentation);
        if (field.Aliases.Length > 0)
        {
            _writer.WriteStartArray(AvroJsonKeys.Aliases);
            foreach (var alias in field.Aliases)
                _writer.WriteStringValue(alias);
            _writer.WriteEndArray();
        }
        WriteDefault(field.Default);

        // Omit field order to match the schema text emitted by Apache.Avro CodeGen.
        foreach (var entry in field.Properties)
        {
            _writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(_writer);
        }
        _writer.WriteEndObject();
    }

    private void WriteProtocol(ProtocolSchema schema)
    {
        _writer.WriteStartObject();
        _writer.WriteString(AvroJsonKeys.Protocol, schema.SchemaName.Name);
        if (schema.SchemaName.Namespace is not null)
        {
            _writer.WriteString(AvroJsonKeys.Namespace, schema.SchemaName.Namespace);
        }
        if (schema.Documentation is not null)
        {
            _writer.WriteString(AvroJsonKeys.Doc, schema.Documentation);
        }
        if (schema.Types.Length > 0)
        {
            _writer.WriteStartArray(AvroJsonKeys.Types);
            foreach (var type in schema.Types)
            {
                Write(type, schema.SchemaName.Namespace);
            }

            _writer.WriteEndArray();
        }
        if (schema.Messages.Length > 0)
        {
            _writer.WriteStartObject(AvroJsonKeys.Messages);
            foreach (var message in schema.Messages)
            {
                _writer.WritePropertyName(message.MethodName.SchemaName);
                WriteMessage(message, schema.SchemaName.Namespace);
            }

            _writer.WriteEndObject();
        }
        foreach (var entry in schema.Properties)
        {
            _writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(_writer);
        }
        _writer.WriteEndObject();
    }

    private void WriteMessage(ProtocolMessage message, string? containingNamespace)
    {
        _writer.WriteStartObject();
        if (message.Documentation is not null)
            _writer.WriteString(AvroJsonKeys.Doc, message.Documentation);

        _writer.WritePropertyName(AvroJsonKeys.Request);
        _writer.WriteStartArray();
        foreach (var parameter in message.RequestParameters)
            WriteParameter(parameter, containingNamespace);

        _writer.WriteEndArray();
        _writer.WritePropertyName(AvroJsonKeys.Response);
        Write(message.Response.Type, containingNamespace);
        if (message.OneWay is { } oneWay)
            _writer.WriteBoolean(AvroJsonKeys.OneWay, oneWay);

        switch (message.Errors)
        {
            case UnionSchema union:
                if (!union.Schemas.Any(static error => error.SchemaType is not SchemaType.String)) break;
                _writer.WriteStartArray(AvroJsonKeys.Errors);
                foreach (var error in union.Schemas)
                {
                    if (error.SchemaType is SchemaType.String) continue;
                    Write(error, containingNamespace);
                }
                _writer.WriteEndArray();
                break;

            case AvroSchema { SchemaType: not SchemaType.String } schema:
                _writer.WriteStartArray(AvroJsonKeys.Errors);
                Write(schema, containingNamespace);
                _writer.WriteEndArray();
                break;
        }

        _writer.WriteEndObject();
    }

    private void WriteParameter(ProtocolRequestParameter parameter, string? containingNamespace)
    {
        _writer.WriteStartObject();
        _writer.WriteString(AvroJsonKeys.Name, parameter.Name.SchemaName);
        _writer.WritePropertyName(AvroJsonKeys.Type);
        Write(parameter.Type, containingNamespace);
        if (parameter.Documentation is not null)
        {
            _writer.WriteString(AvroJsonKeys.Doc, parameter.Documentation);
        }
        WriteDefault(parameter.Default);
        _writer.WriteEndObject();
    }

    private void WriteDefault(DefaultValue? value)
    {
        if (value is null)
            return;

        if (value is not UnboundDefaultValue unbound)
            throw new InvalidOperationException($"Unsupported default value type: {value.GetType().Name}");

        _writer.WritePropertyName(AvroJsonKeys.Default);
        // TODO: Indent nested defaults without changing their JSON token spelling.
        _writer.WriteRawValue(unbound.JsonText);
    }

    private void WriteLogical(LogicalSchema schema, string? containingNamespace)
    {
        var logicalType = JsonSerializer.SerializeToElement(schema.SchemaName.Name);
        var underlyingSchema = schema.UnderlyingSchema with { Properties = schema.Properties.SetItem(AvroJsonKeys.LogicalType, logicalType) };
        Write(underlyingSchema, containingNamespace);
    }

    private void WriteSchemaReference(AvroSchemaReference schema, string? containingNamespace)
    {
        if (_writtenSchemas.Contains(schema.SchemaName))
        {
            WriteReferenceName(schema.SchemaName, containingNamespace);
            return;
        }

        Write(schemaIndex[schema.SchemaName], containingNamespace);
    }

    private void WriteReferenceName(SchemaName name, string? containingNamespace) =>
        _writer.WriteStringValue(name.Namespace == containingNamespace ? name.Name : name.FullName);
}
