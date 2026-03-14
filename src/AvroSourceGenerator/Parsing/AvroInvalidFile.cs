using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Exceptions;

namespace AvroSourceGenerator.Parsing;

internal sealed record class AvroInvalidFile(string Path, string? Text, ImmutableArray<DiagnosticInfo> Diagnostics) : IAvroFile
{
    public bool Equals(AvroInvalidFile? other) => other is not null && Path == other.Path;

    public override int GetHashCode() => Path?.GetHashCode() ?? 0;

    public static IAvroFile Empty(string path) =>
        new AvroInvalidFile(path, null, [InvalidJsonDiagnostic.Create(LocationInfo.FromSourceFile(path, null), "The file is empty.")]);

    public static IAvroFile Invalid(string path, string? text, JsonException exception) =>
        new AvroInvalidFile(path, text, [InvalidJsonDiagnostic.Create(LocationInfo.FromException(path, text, exception), exception.Message)]);

    public static IAvroFile Invalid(string path, string? text, InvalidSchemaException exception) =>
        new AvroInvalidFile(path, text, [InvalidJsonDiagnostic.Create(LocationInfo.FromSourceFile(path, text), exception.Message)]);
}
