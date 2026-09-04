using System.Collections.Immutable;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Compiler;

public readonly record struct ParseResult(
    AvroSchema Root,
    ImmutableArray<TopLevelSchema> Declarations,
    ImmutableArray<SchemaName> References,
    IReadOnlyDictionary<SchemaName, ImmutableArray<SchemaName>> Dependencies,
    ImmutableArray<string> Imports);
