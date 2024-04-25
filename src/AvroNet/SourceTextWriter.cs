using Avro;
using Microsoft.CodeAnalysis.CSharp;
using System.CodeDom.Compiler;

namespace AvroNet;

internal sealed class SourceTextWriter : IDisposable
{
    private readonly StringWriter _stream;
    private readonly IndentedTextWriter _writer;
    private readonly AvroModelOptions _options;

    public SourceTextWriter(AvroModelOptions options)
    {
        _stream = new StringWriter();
        _writer = new IndentedTextWriter(_stream, AvroGenerator.TabString);
        _options = options;
    }

    private static string ValidIdentifier(string identifier)
    {
        return SyntaxFacts.IsReservedKeyword(SyntaxFacts.GetKeywordKind(identifier))
            ? $"@{identifier}"
            : identifier;
    }

    private void Comment(string? comment)
    {
        if (!string.IsNullOrWhiteSpace(comment))
            _writer.WriteLine(SyntaxFactory.Comment(comment!).ToFullString());
    }

    private void StartDefinition(string typeDefinition, string typeIdentifier, string? baseTypeIdentifier = null)
    {
        _writer.Write('[');
        _writer.Write(AvroGenerator.GeneratedCodeAttribute);
        _writer.WriteLine(']');
        _writer.Write(_options.AccessModifier);
        _writer.Write(' ');
        _writer.Write(typeDefinition);
        _writer.Write(' ');
        _writer.Write(typeIdentifier);
        if (baseTypeIdentifier is not null)
        {
            _writer.Write(' ');
            _writer.Write(':');
            _writer.Write(' ');
            _writer.Write(baseTypeIdentifier);
        }
        _writer.WriteLine();
        _writer.WriteLine('{');
        ++_writer.Indent;
    }

    private void EndDefinition()
    {
        --_writer.Indent;
        _writer.WriteLine('}');
    }

    private void SchemaProperty(Schema schema, bool isOverride)
    {
        _writer.Write("public static readonly ");
        _writer.Write(AvroGenerator.AvroSchemaTypeName);
        _writer.Write(" _SCHEMA = ");
        _writer.Write(AvroGenerator.AvroSchemaTypeName);
        _writer.Write(".Parse(");
        var schemaJson = schema.Name == _options.Name
            ? AvroGenerator.AvroClassSchemaConstName
            : SymbolDisplay.FormatLiteral(schema.ToString(), quote: true);
        _writer.Write(schemaJson);
        _writer.WriteLine(");");

        _writer.Write("public ");
        if (isOverride)
            _writer.Write("override ");
        _writer.Write(AvroGenerator.AvroSchemaTypeName);
        _writer.Write(" Schema { get => ");
        _writer.Write(schema.Name);
        _writer.Write('.');
        _writer.Write("_SCHEMA");
        _writer.WriteLine("; }");
    }

    private string Field(Field field, ref GetPutBuilder getPutBuilder)
    {
        var fieldName = ValidIdentifier(field.Name);
        var fieldType = FieldType.FromSchema(field.Schema, _options.Namespace);
        Comment(field.Documentation);
        _writer.Write("public ");
        if (!fieldType.IsNullable)
            _writer.Write("required ");
        _writer.Write(fieldType);
        _writer.Write(' ');
        _writer.Write(fieldName);
        var defaultValue = DefaultValue(field, field.Schema, _options.Namespace);
        if (defaultValue is not null)
        {
            _writer.Write(" { get; init; } = ");
            _writer.Write(defaultValue);
            _writer.WriteLine(';');
        }
        else
        {
            _writer.WriteLine(" { get; init; }");
        }

        getPutBuilder.AddCase(field.Pos, fieldName, fieldType);

        return fieldName;


        static string? DefaultValue(Field field, Schema schema, string @namespace)
        {
            return schema.Tag switch
            {
                Schema.Type.Null => null,
                Schema.Type.Boolean => field.DefaultValue?.ToObject<bool?>()?.ToString(),
                Schema.Type.Int => field.DefaultValue?.ToObject<int?>()?.ToString(),
                Schema.Type.Long => field.DefaultValue?.ToObject<long?>()?.ToString(),
                Schema.Type.Float => field.DefaultValue?.ToObject<float?>()?.ToString(),
                Schema.Type.Double => field.DefaultValue?.ToObject<double?>()?.ToString(),
                Schema.Type.Bytes => field.DefaultValue?.ToObject<byte[]?>()?.ToString(),
                Schema.Type.String => field.DefaultValue?.ToObject<string?>() is string s ? SymbolDisplay.FormatLiteral(s, quote: true) : null,
                Schema.Type.Enumeration => ((EnumSchema)schema).Default is string enumValue ? $"{@namespace}.{ValidIdentifier(enumValue)}" : null,
                Schema.Type.Union => GetUnionDefaultValue(field, (UnionSchema)schema, @namespace),
                _ => null,
            };

            static string? GetUnionDefaultValue(Field field, UnionSchema schema, string @namespace)
            {
                if (schema.Count != 2)
                    return null;

                return (schema.Schemas[0].Tag, schema.Schemas[1].Tag) switch
                {
                    (_, Schema.Type.Null) => DefaultValue(field, schema.Schemas[0], @namespace),
                    (Schema.Type.Null, _) => DefaultValue(field, schema.Schemas[1], @namespace),
                    (_, _) => null,
                };
            }
        }
    }

    private void Enum(EnumSchema schema)
    {
        Comment(schema.Documentation);
        var name = ValidIdentifier(schema.Name);
        StartDefinition("enum", name);
        foreach (var field in schema.Symbols)
        {
            _writer.Write(ValidIdentifier(field));
            _writer.WriteLine(',');
        }
        EndDefinition();
    }

    private void Fixed(FixedSchema schema)
    {
        Comment(schema.Documentation);
        var name = ValidIdentifier(schema.Name);
        StartDefinition("partial class", name, AvroGenerator.AvroSpecificFixedTypeName);
        SchemaProperty(schema, isOverride: true);
        _writer.Write("public uint FixedSize { get => ");
        _writer.Write(schema.Size);
        _writer.WriteLine("; }");
        _writer.WriteLine();
        _writer.Write("public ");
        _writer.Write(name);
        _writer.Write("() : base(");
        _writer.Write(schema.Size);
        _writer.WriteLine(") { }");
        EndDefinition();
    }

    private void Record(RecordSchema schema)
    {
        if (schema.Documentation is not null)
            Comment(schema.Documentation);

        var name = ValidIdentifier(schema.Name);
        StartDefinition(_options.DeclarationType, name, AvroGenerator.AvroISpecificRecordTypeName);
        SchemaProperty(schema, isOverride: false);

        var getPutBuilder = new GetPutBuilder(name, _writer.Indent, isOverride: schema.Tag is Schema.Type.Error);
        foreach (var field in schema.Fields)
            Field(field, ref getPutBuilder);
        getPutBuilder.AddDefault();
        _writer.WriteLine();
        getPutBuilder.WriteTo(_writer);

        EndDefinition();
    }

    public SourceTextWriter ProcessSchema(Schema schema)
    {
        _writer.WriteLine(AvroGenerator.AutoGeneratedComment);
        _writer.WriteLine("#nullable enable");
        _writer.Write("namespace ");
        _writer.Write(_options.Namespace);
        _writer.WriteLine(";");
        _writer.WriteLine();

        var namesSeen = new HashSet<SchemaName>();
        foreach (var namedSchema in schema.EnumerateNames())
        {
            if (namesSeen.Add(namedSchema.SchemaName))
            {
                switch (namedSchema.Tag)
                {
                    case Schema.Type.Enumeration: Enum((EnumSchema)namedSchema); break;
                    case Schema.Type.Fixed: Fixed((FixedSchema)namedSchema); break;
                    case Schema.Type.Record: Record((RecordSchema)namedSchema); break;
                    case Schema.Type.Error: Record((RecordSchema)namedSchema); break;
                }
            }
            _writer.WriteLine();
        }
        _writer.WriteLine("#nullable restore");
        return this;
    }

    public override string ToString() => _stream.ToString();

    public void Dispose()
    {
        _stream.Dispose();
        _writer.Dispose();
    }
}

file static class SchemaExtensions
{
    public static TSchema As<TSchema>(this Schema schema) where TSchema : Schema
    {
        if (schema is not TSchema derived)
            throw new InvalidOperationException($"Invalid schema type '{schema?.GetType().Name ?? "null"}'. Expected '{typeof(TSchema).Name}'");

        return derived;
    }

    public static IEnumerable<NamedSchema> EnumerateNames(this Schema schema)
    {
        var namedSchema = schema as NamedSchema;
        switch (schema.Tag)
        {
            case Schema.Type.Null:
            case Schema.Type.Boolean:
            case Schema.Type.Int:
            case Schema.Type.Long:
            case Schema.Type.Float:
            case Schema.Type.Double:
            case Schema.Type.Bytes:
            case Schema.Type.String:
            case Schema.Type.Logical:
                break;

            case Schema.Type.Enumeration:
            case Schema.Type.Fixed:
                yield return namedSchema!;
                break;

            case Schema.Type.Record:
            case Schema.Type.Error:
                yield return namedSchema!;
                foreach (var field in schema!.As<RecordSchema>().Fields)
                    foreach (var fieldSchema in EnumerateNames(field.Schema))
                        yield return fieldSchema;
                break;

            case Schema.Type.Array:
                foreach (var itemSchema in EnumerateNames(schema!.As<ArraySchema>().ItemSchema))
                    yield return itemSchema;
                break;

            case Schema.Type.Map:
                foreach (var valueSchema in EnumerateNames(schema!.As<MapSchema>().ValueSchema))
                    yield return valueSchema;
                break;

            case Schema.Type.Union:
                foreach (var unionSchema in schema!.As<UnionSchema>().Schemas)
                    foreach (var schema2 in EnumerateNames(unionSchema))
                        yield return schema2;
                break;

            default:
                throw new InvalidOperationException($"Unable to add name for schema '{schema.Name}' of type '{schema.Tag}'");
        }
    }
}