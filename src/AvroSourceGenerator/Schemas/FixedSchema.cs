﻿using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class FixedSchema(
    JsonElement Json,
    string Name,
    string? Namespace,
    string? Documentation,
    ImmutableArray<string> Aliases,
    int Size)
    : NamedSchema(SchemaType.Fixed, Json, Name, Namespace, Documentation, Aliases);
