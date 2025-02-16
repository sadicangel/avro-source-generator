using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.Parsing;
public readonly record struct CompilationInfo(string? AssemblyNamespace, LanguageVersion LanguageVersion);
