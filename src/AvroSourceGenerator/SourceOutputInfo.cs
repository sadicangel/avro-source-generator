using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator;

internal sealed record class SourceOutputInfo(
    string SchemaJson,
    LanguageFeatures LanguageFeatures,
    string? NamespaceOverride,
    string RecordDeclaration,
    string AccessModifier,
    ImmutableArray<Diagnostic> Diagnostics);
