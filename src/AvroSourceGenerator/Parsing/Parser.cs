using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Registry.Extensions;
using AvroSourceGenerator.Schemas;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AvroSourceGenerator.Parsing;

internal static class Parser
{
    public static bool IsAvroFile(AdditionalText text) =>
        text.Path.EndsWith(".avsc", StringComparison.OrdinalIgnoreCase);

    public static AvroFile GetAvroFile(AdditionalText additionalText, CancellationToken cancellationToken)
    {
        var path = additionalText.Path;
        var text = additionalText.GetText(cancellationToken)?.ToString();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        if (string.IsNullOrWhiteSpace(text))
        {
            diagnostics.Add(InvalidJsonDiagnostic.Create(path.GetLocation(text), "The file is empty."));

            return new AvroFile(path, text, default, default, diagnostics.ToImmutable());
        }

        try
        {
            using var jsonDocument = JsonDocument.Parse(text!);
            var json = jsonDocument.RootElement.Clone();
            var name = json.GetOptionalSchemaName();
            return new AvroFile(path, text, json, name, diagnostics.ToImmutable());
        }
        catch (JsonException ex)
        {
            diagnostics.Add(InvalidJsonDiagnostic.Create(path.GetLocation(text, ex), ex.Message));
        }
        catch (InvalidSchemaException ex)
        {
            diagnostics.Add(InvalidSchemaDiagnostic.Create(path.GetLocation(text), ex.Message));
        }

        return new AvroFile(path, text, default, default, diagnostics.ToImmutable());
    }

    public static GeneratorSettings GetGeneratorSettings(
        AnalyzerConfigOptionsProvider provider,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var avroLibrary = default(AvroLibrary?);
        if (provider.GlobalOptions.TryGetValue(
                "build_property.AvroSourceGeneratorAvroLibrary",
                out var avroLibraryString) &&
            Enum.TryParse<AvroLibrary>(avroLibraryString, ignoreCase: true, out var parsedAvroLibrary))
        {
            avroLibrary = parsedAvroLibrary;
        }

        var languageFeatures = default(LanguageFeatures?);
        if (provider.GlobalOptions.TryGetValue(
                "build_property.AvroSourceGeneratorLanguageFeatures",
                out var languageFeaturesString) &&
            Enum.TryParse<LanguageFeatures>(languageFeaturesString, ignoreCase: true, out var parsedLanguageFeatures))
        {
            languageFeatures = parsedLanguageFeatures;
        }

        if (!provider.GlobalOptions.TryGetValue(
                "build_property.AvroSourceGeneratorAccessModifier",
                out var accessModifier) ||
            accessModifier is not ("public" or "internal"))
        {
            accessModifier = null;
        }

        if (!provider.GlobalOptions.TryGetValue(
                "build_property.AvroSourceGeneratorRecordDeclaration",
                out var recordDeclaration) ||
            recordDeclaration is not ("record" or "class"))
        {
            recordDeclaration = null;
        }

        return new GeneratorSettings(avroLibrary, languageFeatures, accessModifier, recordDeclaration);
    }

    public static CompilationInfo GetCompilationInfo(Compilation compilation, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var csharpCompilation = (CSharpCompilation)compilation;

        var avroLibraries = ImmutableArray.CreateBuilder<AvroLibrary>();

        if (csharpCompilation.GetTypeByMetadataName("Avro.Specific.ISpecificRecord") is not null)
            avroLibraries.Add(AvroLibrary.Apache);

        return new CompilationInfo(avroLibraries.ToImmutable(), csharpCompilation.LanguageVersion);
    }

    public static bool IsCandidateDeclaration(SyntaxNode node, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        // The attribute can only be applied to a class or record declaration.
        if (!node.IsKind(SyntaxKind.ClassDeclaration) && !node.IsKind(SyntaxKind.RecordDeclaration))
        {
            return false;
        }

        // TODO: Should we allow non partial and then emit a diagnostic if it's not partial?
        return Unsafe.As<TypeDeclarationSyntax>(node).Modifiers.Any(SyntaxKind.PartialKeyword);
    }
}
