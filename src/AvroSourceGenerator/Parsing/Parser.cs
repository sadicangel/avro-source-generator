using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Schemas.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AvroSourceGenerator.Parsing;

internal static class Parser
{
    private static readonly SymbolDisplayFormat s_partiallyQualifiedFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

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

    public static GeneratorSettings GetGeneratorSettings(AnalyzerConfigOptionsProvider provider, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (!provider.GlobalOptions.TryGetValue("build_property.AvroSourceGeneratorAccessModifier", out var accessModifier) ||
            accessModifier is not ("public" or "internal"))
        {
            accessModifier = null;
        }

        if (!provider.GlobalOptions.TryGetValue("build_property.AvroSourceGeneratorRecordDeclaration", out var recordDeclaration) ||
            recordDeclaration is not ("record" or "class"))
        {
            recordDeclaration = null;
        }

        var languageFeatures = default(LanguageFeatures?);
        if (provider.GlobalOptions.TryGetValue("build_property.AvroSourceGeneratorLanguageFeatures", out var languageFeaturesString) &&
            Enum.TryParse<LanguageFeatures>(languageFeaturesString, ignoreCase: true, out var parsedLanguageFeatures))
        {
            languageFeatures = parsedLanguageFeatures;
        }

        return new GeneratorSettings(accessModifier, recordDeclaration, languageFeatures);
    }

    public static CompilationInfo GetCompilationInfo(Compilation compilation, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var csharpCompilation = (CSharpCompilation)compilation;
        var languageVersion = csharpCompilation.LanguageVersion;

        return new CompilationInfo(languageVersion);
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

    public static AvroOptions GetAvroOptions(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var symbol = Unsafe.As<INamedTypeSymbol>(context.TargetSymbol);
        var typeName = symbol.Name;
        var typeNamespace = symbol.ContainingNamespace?.ToDisplayString(s_partiallyQualifiedFormat);

        var declaration = Unsafe.As<TypeDeclarationSyntax>(context.TargetNode);
        var recordDeclaration = declaration.IsKind(SyntaxKind.RecordDeclaration) ? "record" : "class";
        var accessModifier = GetAccessModifier(declaration);

        var attribute = context.Attributes
            .Single(attr => attr.AttributeClass?.Name == nameof(AvroAttribute));

        var languageFeatures = default(LanguageFeatures?);
        var location = context.TargetNode.GetLocation();

        foreach (var kvp in attribute.NamedArguments)
        {
            var name = kvp.Key;
            var value = kvp.Value.Value;

            if (name is nameof(AvroAttribute.LanguageFeatures) && value is not null)
            {
                languageFeatures = (LanguageFeatures)value;
            }
        }

        return new AvroOptions(
            typeName,
            typeNamespace,
            accessModifier,
            recordDeclaration,
            languageFeatures,
            location);
    }

    private static string GetAccessModifier(TypeDeclarationSyntax typeDeclaration)
    {
        // Default to internal if no access modifier is specified
        var accessModifier = "internal";

        var modifiers = typeDeclaration.Modifiers;

        if (modifiers.Any(SyntaxKind.PublicKeyword))
        {
            accessModifier = "public";
        }
        else if (modifiers.Any(SyntaxKind.ProtectedKeyword))
        {
            if (modifiers.Any(SyntaxKind.InternalKeyword))
            {
                accessModifier = "protected internal";
            }
            else if (modifiers.Any(SyntaxKind.PrivateKeyword))
            {
                accessModifier = "private protected";
            }
            else
            {
                accessModifier = "protected";
            }
        }
        else if (modifiers.Any(SyntaxKind.PrivateKeyword))
        {
            accessModifier = "private";
        }
        else if (modifiers.Any(SyntaxKind.FileKeyword))
        {
            accessModifier = "file";
        }

        return accessModifier;
    }
}
