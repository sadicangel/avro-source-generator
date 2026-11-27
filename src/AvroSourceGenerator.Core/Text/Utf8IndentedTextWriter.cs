using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace AvroSourceGenerator.Text;

public sealed class Utf8IndentedTextWriter : IDisposable
{
    private static readonly Encoding s_encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly IBufferWriter<byte>? _writer;
    private readonly Stream? _stream;
    private readonly bool _leaveOpen;

    private readonly byte[] _indentation;
    private readonly byte[] _newLine;

    private byte[]? _buffer;
    private int _buffered;

    private bool _atLineStart = true;
    private bool _disposed;

    public Utf8IndentedTextWriter(IBufferWriter<byte> writer, string indentation = "    ", string newLine = "\n")
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _indentation = s_encoding.GetBytes(indentation);
        _newLine = s_encoding.GetBytes(newLine);
    }

    public Utf8IndentedTextWriter(Stream stream, string indentation = "    ", string newLine = "\n", int bufferSize = 4096, bool leaveOpen = false)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        if (!stream.CanWrite)
            throw new ArgumentException("Stream must be writable.", nameof(stream));

        if (bufferSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize));

        _stream = stream;
        _leaveOpen = leaveOpen;

        _indentation = s_encoding.GetBytes(indentation);
        _newLine = s_encoding.GetBytes(newLine);

        _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
    }

    public int Indent { get; set => field = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value)); }

    public void Write(byte value)
    {
        if (value is (byte)'\r' or (byte)'\n')
        {
            Append(value);
            _atLineStart = true;
            return;
        }

        EnsureIndentation();
        Append(value);
    }

    public void Write(char value)
    {
        if (value <= 0x7F)
        {
            Write((byte)value);
            return;
        }

        Span<char> buffer = stackalloc char[1];
        buffer[0] = value;

        Write(buffer);
    }

    public void Write(string? value) => Write(value.AsSpan());

    public void Write(ReadOnlySpan<byte> value)
    {
        while (!value.IsEmpty)
        {
            var index = value.IndexOfAny((byte)'\r', (byte)'\n');

            if (index < 0)
            {
                EnsureIndentation();
                Append(value);
                return;
            }

            if (index > 0)
            {
                EnsureIndentation();
                Append(value[..index]);
            }

            Append(value[index]);
            _atLineStart = true;

            value = value[(index + 1)..];
        }
    }

    public void Write(ReadOnlySpan<char> value)
    {
        while (!value.IsEmpty)
        {
            var index = value.IndexOfAny('\r', '\n');

            if (index < 0)
            {
                EnsureIndentation();
                Append(value);
                return;
            }

            if (index > 0)
            {
                EnsureIndentation();
                Append(value[..index]);
            }

            Append((byte)value[index]);
            _atLineStart = true;

            value = value[(index + 1)..];
        }
    }

    public void Write([InterpolatedStringHandlerArgument("")] ref WriteInterpolatedStringHandler _) { }

    public void WriteLine()
    {
        Append(_newLine);
        _atLineStart = true;
    }

    public void WriteLine(byte value)
    {
        Write(value);
        WriteLine();
    }

    public void WriteLine(char value)
    {
        Write(value);
        WriteLine();
    }

    public void WriteLine(string? value)
    {
        Write(value);
        WriteLine();
    }

    public void WriteLine(ReadOnlySpan<byte> value)
    {
        Write(value);
        WriteLine();
    }

    public void WriteLine(ReadOnlySpan<char> value)
    {
        Write(value);
        WriteLine();
    }

    public void WriteLine([InterpolatedStringHandlerArgument("")] ref WriteInterpolatedStringHandler _) => WriteLine();

    public IndentScope PushIndent() => new(this);

    public BlockScope WriteBlock(BraceStyle braceStyle) => new(this, braceStyle);

    public void Flush()
    {
        ThrowIfDisposed();

        FlushBuffer();
        _stream?.Flush();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            Flush();
        }
        finally
        {
            _disposed = true;

            if (_buffer is not null)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = null;
            }

            if (!_leaveOpen)
                _stream?.Dispose();
        }
    }
    private void EnsureNewLine()
    {
        if (!_atLineStart)
            WriteLine();
    }

    private void EnsureIndentation()
    {
        if (!_atLineStart)
            return;

        if (Indent > 0 && _indentation.Length > 0)
        {
            var length = checked(Indent * _indentation.Length);
            var destination = GetSpan(length);

            for (var offset = 0; offset < length; offset += _indentation.Length)
            {
                _indentation.AsSpan().CopyTo(
                    destination[offset..]);
            }

            Advance(length);
        }

        _atLineStart = false;
    }

    private void Append(byte value)
    {
        var destination = GetSpan(1);
        destination[0] = value;

        Advance(1);
    }

    private void Append(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
            return;

        var destination = GetSpan(value.Length);

        value.CopyTo(destination);

        Advance(value.Length);
    }

    private void Append(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            return;

        var byteCount = GetByteCount(value);
        var destination = GetSpan(byteCount);

        var written = Encode(value, destination);

        Advance(written);
    }

    private void WritePadding(int count)
    {
        if (count <= 0)
            return;

        EnsureIndentation();

        var destination = GetSpan(count);

        destination[..count].Fill((byte)' ');

        Advance(count);
    }

    private Span<byte> GetSpan(int sizeHint)
    {
        ThrowIfDisposed();

        if (_writer is not null)
            return _writer.GetSpan(sizeHint);

        var buffer = _buffer!;

        if (buffer.Length - _buffered >= sizeHint)
            return buffer.AsSpan(_buffered);

        FlushBuffer();

        if (buffer.Length < sizeHint)
        {
            var replacement = ArrayPool<byte>.Shared.Rent(sizeHint);

            _buffer = replacement;

            ArrayPool<byte>.Shared.Return(buffer);

            buffer = replacement;
        }

        return buffer.AsSpan();
    }

    private void Advance(int count)
    {
        if (_writer is not null)
            _writer.Advance(count);
        else
            _buffered += count;
    }

    private void FlushBuffer()
    {
        if (_stream is null || _buffered == 0)
            return;

        _stream.Write(_buffer!, 0, _buffered);

        _buffered = 0;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Utf8IndentedTextWriter));
    }

    private static int GetByteCount(ReadOnlySpan<char> value)
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        return s_encoding.GetByteCount(value);
#else
        unsafe
        {
            fixed (char* pointer = value)
                return s_encoding.GetByteCount(pointer, value.Length);
        }
#endif
    }

    private static int GetCharCount(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
            return 0;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        return s_encoding.GetCharCount(value);
#else
        unsafe
        {
            fixed (byte* pointer = value)
                return s_encoding.GetCharCount(pointer, value.Length);
        }
#endif
    }

    private static int Encode(
        ReadOnlySpan<char> value,
        Span<byte> destination)
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        return s_encoding.GetBytes(value, destination);
#else
        unsafe
        {
            fixed (char* source = value)
            fixed (byte* target = destination)
            {
                return s_encoding.GetBytes(
                    source,
                    value.Length,
                    target,
                    destination.Length);
            }
        }
#endif
    }

    public readonly struct IndentScope : IDisposable
    {
        private readonly Utf8IndentedTextWriter _writer;

        internal IndentScope(Utf8IndentedTextWriter writer)
        {
            _writer = writer;
            _writer.Indent++;
        }

        public void Dispose() => _writer.Indent--;
    }

    public readonly struct BlockScope : IDisposable
    {
        private readonly Utf8IndentedTextWriter _writer;

        internal BlockScope(Utf8IndentedTextWriter writer, BraceStyle braceStyle)
        {
            _writer = writer;

            switch (braceStyle)
            {
                case BraceStyle.SameLine:
                    if (!writer._atLineStart)
                        writer.Write((byte)' ');
                    break;

                case BraceStyle.NewLine:
                    writer.EnsureNewLine();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(braceStyle));
            }

            writer.WriteLine((byte)'{');
            writer.Indent++;
        }

        public void Dispose()
        {
            _writer.EnsureNewLine();
            _writer.Indent--;
            _writer.WriteLine((byte)'}');
        }
    }

    [InterpolatedStringHandler]
    public readonly ref struct WriteInterpolatedStringHandler
    {
        private readonly Utf8IndentedTextWriter _writer;

        public WriteInterpolatedStringHandler(
            int literalLength,
            int formattedCount,
            Utf8IndentedTextWriter writer)
        {
            _ = literalLength;
            _ = formattedCount;

            _writer = writer;
        }

        public void AppendLiteral(string value)
            => _writer.Write(value);

        public void AppendFormatted(char value)
            => _writer.Write(value);

        public void AppendFormatted(
            string? value,
            int alignment = 0,
            string? format = null)
            => AppendFormatted(value.AsSpan(), alignment, format);

        public void AppendFormatted(
            scoped ReadOnlySpan<char> value,
            int alignment = 0,
            string? format = null)
        {
            _ = format;

            if (alignment == 0)
            {
                _writer.Write(value);
                return;
            }

            var padding = Math.Max(
                0,
                Math.Abs(alignment) - value.Length);

            if (alignment > 0)
                _writer.WritePadding(padding);

            _writer.Write(value);

            if (alignment < 0)
                _writer.WritePadding(padding);
        }

        public void AppendFormatted(
            scoped ReadOnlySpan<byte> value,
            int alignment = 0,
            string? format = null)
        {
            _ = format;

            if (alignment == 0)
            {
                _writer.Write(value);
                return;
            }

            var padding = Math.Max(
                0,
                Math.Abs(alignment) - GetCharCount(value));

            if (alignment > 0)
                _writer.WritePadding(padding);

            _writer.Write(value);

            if (alignment < 0)
                _writer.WritePadding(padding);
        }

        public void AppendFormatted<T>(
            T value,
            int alignment = 0,
            string? format = null)
        {
#if NET8_0_OR_GREATER
            if (value is IUtf8SpanFormattable utf8Formattable)
            {
                Span<byte> buffer = stackalloc byte[128];

                if (utf8Formattable.TryFormat(
                    buffer,
                    out var written,
                    format.AsSpan(),
                    CultureInfo.InvariantCulture))
                {
                    AppendFormatted(
                        buffer.Slice(0, written),
                        alignment);

                    return;
                }
            }
#endif

#if NET6_0_OR_GREATER
            if (value is ISpanFormattable spanFormattable)
            {
                Span<char> buffer = stackalloc char[128];

                if (spanFormattable.TryFormat(
                    buffer,
                    out var written,
                    format.AsSpan(),
                    CultureInfo.InvariantCulture))
                {
                    AppendFormatted(
                        buffer.Slice(0, written),
                        alignment);

                    return;
                }
            }
#endif

            var text = value switch
            {
                null => string.Empty,

                IFormattable formattable => formattable.ToString(
                    format,
                    CultureInfo.InvariantCulture),

                _ => value.ToString()
            };

            AppendFormatted(text.AsSpan(), alignment);
        }
    }
}
