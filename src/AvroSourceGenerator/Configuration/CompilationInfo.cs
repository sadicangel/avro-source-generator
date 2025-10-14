using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.Configuration;

internal readonly record struct CompilationInfo(ImmutableArray<AvroLibrary> AvroLibraries, LanguageVersion LanguageVersion);
