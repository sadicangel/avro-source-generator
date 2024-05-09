using System.CodeDom.Compiler;
using System.Text;

namespace AvroNet;

internal ref struct GetPutBuilder
{
    private readonly StringBuilder _getBuilder;
    private readonly StringBuilder _putBuilder;

    private readonly StringBuilder _unsafeSetters;
    private readonly AvroModelOptions _options;
    private readonly string _ownerType;
    private readonly int _accessorIndentation;

    private int _builderIndentation;

    public GetPutBuilder(string ownerType, int indent, bool isOverride, AvroModelOptions options)
    {
        _ownerType = ownerType;
        _builderIndentation = indent;
        _getBuilder = new StringBuilder(256);
        _putBuilder = new StringBuilder(256);
        _unsafeSetters = new StringBuilder(256);
        _options = options;

        _getBuilder.Append("public ");
        _putBuilder.Append("public ");
        if (isOverride)
        {
            _getBuilder.Append("override ");
            _putBuilder.Append("override ");
        }
        var objectType = _options.UseNullableReferenceTypes ? "object?" : "object";
        _getBuilder.Append(objectType);
        _getBuilder.AppendLine(" Get(int fieldPos)");
        _putBuilder.Append("void Put(int fieldPos, ");
        _putBuilder.Append(objectType);
        _putBuilder.AppendLine(" fieldValue)");

        IndentBuilders();
        _getBuilder.AppendLine("{");
        _putBuilder.AppendLine("{");
        ++_builderIndentation;
        IndentBuilders();
        _getBuilder.AppendLine("switch (fieldPos)");
        _putBuilder.AppendLine("switch (fieldPos)");
        IndentBuilders();
        _getBuilder.AppendLine("{");
        _putBuilder.AppendLine("{");
        ++_builderIndentation;
        _accessorIndentation = _builderIndentation;
    }

    private readonly void IndentBuilders()
    {
        for (int i = 0; i < _builderIndentation; ++i)
        {
            _getBuilder.Append(AvroGenerator.TabString);
            _putBuilder.Append(AvroGenerator.TabString);
        }
    }

    private readonly void IndentSetters()
    {
        for (int i = 0; i < _accessorIndentation - 1; ++i)
        {
            _unsafeSetters.Append(AvroGenerator.TabString);
        }
    }

    public readonly GetPutBuilder AddCase(int position, string name, TypeSymbol type)
    {
        if (_options.UseUnsafeAccessors)
        {
            IndentBuilders();
            _getBuilder.Append("case ");
            _getBuilder.Append(position);
            _getBuilder.Append(": return this.");
            _getBuilder.Append(name);
            _getBuilder.AppendLine(";");

            _putBuilder.Append("case ");
            _putBuilder.Append(position);
            _putBuilder.Append(": Set_");
            _putBuilder.Append(name);
            _putBuilder.Append("(this, (");
            _putBuilder.Append(type);
            if (_options.UseNullableReferenceTypes)
                _putBuilder.AppendLine(")fieldValue!); break;");
            else
                _putBuilder.AppendLine(")fieldValue); break;");

            const string UnsafeAccessorInit =
                "[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = ";

            IndentSetters();
            _unsafeSetters.Append(UnsafeAccessorInit);
            _unsafeSetters.Append("\"set_");
            _unsafeSetters.Append(name);
            _unsafeSetters.AppendLine("\")]");

            IndentSetters();
            _unsafeSetters.Append("extern static ");
            _unsafeSetters.Append(type);
            _unsafeSetters.Append(" Set_");
            _unsafeSetters.Append(name);
            _unsafeSetters.Append('(');
            _unsafeSetters.Append(_ownerType);
            _unsafeSetters.Append(" obj, ");
            _unsafeSetters.Append(type);
            _unsafeSetters.AppendLine(" value);");
        }
        else
        {
            IndentBuilders();
            _getBuilder.Append("case ");
            _getBuilder.Append(position);
            _getBuilder.Append(": return this.");
            _getBuilder.Append(name);
            _getBuilder.AppendLine(";");

            _putBuilder.Append("case ");
            _putBuilder.Append(position);
            _putBuilder.Append(": this.");
            _putBuilder.Append(name);
            _putBuilder.Append(" = (");
            _putBuilder.Append(type);
            if (_options.UseNullableReferenceTypes)
                _putBuilder.AppendLine(")fieldValue!; break;");
            else
                _putBuilder.AppendLine(")fieldValue; break;");
        }

        return this;
    }

    public GetPutBuilder AddDefault()
    {
        IndentBuilders();

        _getBuilder.AppendLine("""default: throw new global::Avro.AvroRuntimeException($"Bad index {fieldPos} in Get()");""");
        _putBuilder.AppendLine("""default: throw new global::Avro.AvroRuntimeException($"Bad index {fieldPos} in Put()");""");

        --_builderIndentation;
        IndentBuilders();

        _getBuilder.AppendLine("}");
        _putBuilder.AppendLine("}");

        --_builderIndentation;

        _putBuilder.AppendLine();
        _putBuilder.Append(_unsafeSetters);

        IndentBuilders();

        _getBuilder.AppendLine("}");
        _putBuilder.AppendLine("}");

        return this;
    }

    public readonly void WriteTo(IndentedTextWriter writer)
    {
        writer.WriteLine(_getBuilder);
        writer.WriteLine(_putBuilder);
    }
}
