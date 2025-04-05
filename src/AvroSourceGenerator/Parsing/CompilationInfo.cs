using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.Parsing;

internal readonly record struct CompilationInfo(LanguageVersion LanguageVersion);
