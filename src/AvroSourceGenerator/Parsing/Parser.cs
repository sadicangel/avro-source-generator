using System.Runtime.CompilerServices;
using AvroSourceGenerator.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AvroSourceGenerator.Parsing;
internal static class Parser
{
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

    public static AvroModel GetAvroModel(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var symbol = Unsafe.As<INamedTypeSymbol>(context.TargetSymbol);
        var containingClassName = symbol.Name;
        var containingNamespace = symbol.ContainingNamespace?
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? $"{symbol.Name}Namespace";

        var declaration = Unsafe.As<TypeDeclarationSyntax>(context.TargetNode);
        var recordDeclaration = declaration.IsKind(SyntaxKind.RecordDeclaration) ? "record" : "class";
        var accessModifier = GetAccessModifier(declaration);

        var attribute = context.Attributes
            .Single(attr => attr.AttributeClass?.Name == nameof(AvroAttribute));

        var languageFeatures = default(LanguageFeatures?);
        var namespaceOverride = default(string);
        var schemaJson = attribute.ConstructorArguments.FirstOrDefault().Value as string;
        var schemaLocation = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
        var diagnostics = EquatableArray<Diagnostic>.Empty;

        if (string.IsNullOrWhiteSpace(schemaJson))
        {
            diagnostics = new EquatableArray<Diagnostic>([
                InvalidJsonDiagnostic.Create(schemaLocation, "Cannot be null or whitespace")]);
        }

        foreach (var kvp in attribute.NamedArguments)
        {
            var name = kvp.Key;
            var value = kvp.Value.Value;

            switch (name)
            {
                case nameof(AvroAttribute.LanguageFeatures) when value is not null:
                    languageFeatures = (LanguageFeatures)value;
                    break;
                case nameof(AvroAttribute.UseCSharpNamespace) when value is true:
                    namespaceOverride = containingNamespace;
                    break;
            }
        }

        return new AvroModel(
            languageFeatures,
            containingClassName,
            containingNamespace,
            namespaceOverride,
            recordDeclaration,
            accessModifier,
            schemaJson,
            schemaLocation,
            diagnostics);
    }

    public static LanguageVersion GetLanguageVersion(Compilation compilation, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        return ((CSharpCompilation)compilation).LanguageVersion;
    }

    private

        static string GetAccessModifier(TypeDeclarationSyntax typeDeclaration)
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
