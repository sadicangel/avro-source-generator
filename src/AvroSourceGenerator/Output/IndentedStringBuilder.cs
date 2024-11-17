using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

internal sealed class IndentedStringBuilder
{
    private const string DefaultIndentation = "    ";

    private readonly StringBuilder _builder;
    private int _indentation;
    private bool _indentationPending;

    public IndentedStringBuilder()
    {
        _builder = new StringBuilder();
    }

    public IndentedStringBuilder(int capacity)
    {
        _builder = new StringBuilder(capacity);
    }

    public override string ToString() => _builder.ToString();

    public IndentedStringBuilder IncrementIndentation(int indentation = 1)
    {
        _indentation += indentation;
        return this;
    }

    public IndentedStringBuilder DecrementIndentation(int indentation = 1)
    {
        _indentation = Math.Max(0, _indentation - indentation);
        return this;
    }

    private void AppendIndentation()
    {
        if (!_indentationPending) return;
        _indentationPending = false;
        for (int i = 0; i < _indentation; ++i)
            _builder.Append(DefaultIndentation);
    }

    public IndentedStringBuilder Append(char value)
    {
        AppendIndentation();
        _builder.Append(value);
        return this;
    }

    public IndentedStringBuilder Append(string? value)
    {
        AppendIndentation();
        _builder.Append(value);
        return this;
    }

    public IndentedStringBuilder Append(
        [InterpolatedStringHandlerArgument("")] ref WriteInterpolatedStringHandler handler) => this;

    public IndentedStringBuilder AppendLine()
    {
        AppendIndentation();
        _builder.AppendLine();
        _indentationPending = true;
        return this;
    }

    public IndentedStringBuilder AppendLine(string? value)
    {
        AppendIndentation();
        _builder.AppendLine(value);
        _indentationPending = true;
        return this;
    }

    public IndentedStringBuilder AppendLine(
        [InterpolatedStringHandlerArgument("")] ref WriteInterpolatedStringHandler handler) => AppendLine();

    /// <summary>
    /// Provides a handler used by the language compiler to append interpolated strings into <see cref="IndentedTextWriter"/> instances.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [InterpolatedStringHandler]
    public readonly ref struct WriteInterpolatedStringHandler
    {
        private readonly IndentedStringBuilder _builder;

        public WriteInterpolatedStringHandler(int literalLength, int formattedCount, IndentedStringBuilder builder)
        {
            _builder = builder;

            // Assume that each formatted section adds at least one character.
            _builder._builder.EnsureCapacity(_builder._builder.Length + literalLength + formattedCount);
        }

        public void AppendLiteral(string literal)
            => _builder.Append(literal);

        public void AppendFormatted<T>(T value)
        {
            var str = value?.ToString();
            if (str is not null)
                _builder.Append(str);
        }

        public void AppendFormatted<T>(T value, string format) where T : IFormattable
        {
            var str = value?.ToString(format, formatProvider: null);
            if (str is not null)
                _builder.Append(str);
        }
    }
}
