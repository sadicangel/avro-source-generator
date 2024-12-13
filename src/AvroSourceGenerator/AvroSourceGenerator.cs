using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class AvroSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var models = context.SyntaxProvider
            .ForAttributeWithMetadataName("AvroSourceGenerator.AvroAttribute",
                predicate: static (node, cancellationToken) =>
                {
                    // The attribute can only be applied to a class or record declaration.
                    if (!node.IsKind(SyntaxKind.ClassDeclaration) && !node.IsKind(SyntaxKind.RecordDeclaration))
                    {
                        return false;
                    }

                    // TODO: Should we emit a diagnostic if the class is not partial?
                    return Unsafe.As<TypeDeclarationSyntax>(node).Modifiers.Any(SyntaxKind.PartialKeyword);
                },
                transform: static (context, cancellationToken) =>
                {
                    var symbol = Unsafe.As<INamedTypeSymbol>(context.TargetSymbol);

                    var containingClassName = symbol.Name;
                    var containingNamespace = symbol.ContainingNamespace?
                        .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? $"{symbol.Name}Namespace";


                    var attribute = context.Attributes
                        .Single(attr => attr.AttributeClass?.Name == nameof(AvroAttribute));

                    var schemaJson = GetAttributeData(attribute, containingNamespace, out var languageFeatures, out var namespaceOverride, out var diagnostics);

                    var declaration = Unsafe.As<TypeDeclarationSyntax>(context.TargetNode);

                    return new SourceOutputModel(
                        LanguageFeatures: languageFeatures,
                        ContainingClassName: containingClassName,
                        ContainingNamespace: containingNamespace,
                        NamespaceOverride: namespaceOverride,
                        RecordDeclaration: declaration.IsKind(SyntaxKind.RecordDeclaration) ? "record" : "class",
                        AccessModifier: GetAccessModifier(declaration),
                        SchemaJson: schemaJson,
                        SchemaLocation: attribute.ApplicationSyntaxReference!.GetSyntax().GetLocation(),
                        Diagnostics: diagnostics);
                });

        context.RegisterImplementationSourceOutput(models, static (context, model) =>
        {
            foreach (var diagnostic in model.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }

            if (model.SchemaJson is null)
            {
                return;
            }

            try
            {
                using var document = JsonDocument.Parse(model.SchemaJson);
                var schemaRegistry = new SchemaRegistry(model.LanguageFeatures, model.NamespaceOverride);
                var rootSchema = schemaRegistry.Register(document.RootElement, model.ContainingNamespace);

                //if (rootSchema.Name != model.ContainingClassName)
                //{
                //    context.ReportDiagnostic(InvalidNameDiagnostic.Create(model.SchemaLocation, rootSchema.Name, model.ContainingClassName));
                //    return;
                //}

                //if (model.NamespaceOverride is null && !string.IsNullOrWhiteSpace(rootSchema.Namespace) && rootSchema.Namespace != model.ContainingNamespace)
                //{
                //    context.ReportDiagnostic(InvalidNamespaceDiagnostic.Create(model.SchemaLocation, rootSchema.Namespace!, model.ContainingNamespace));
                //    return;
                //}

                // We should get no render errors, so we don't have to handle anything else.
                var renderOutputs = AvroTemplate.Render(
                    schemaRegistry: schemaRegistry,
                    languageFeatures: model.LanguageFeatures,
                    recordDeclaration: model.RecordDeclaration,
                    accessModifier: model.AccessModifier);

                foreach (var renderOutput in renderOutputs)
                {
                    context.AddSource(renderOutput.HintName, SourceText.From(renderOutput.SourceText, Encoding.UTF8));
                }
            }
            catch (JsonException ex)
            {
                context.ReportDiagnostic(InvalidJsonDiagnostic.Create(model.SchemaLocation, ex.Message));
            }
            catch (InvalidSchemaException ex)
            {
                context.ReportDiagnostic(InvalidAvroSchemaDiagnostic.Create(model.SchemaLocation, ex.Message));
            }
        });
    }

    private static string? GetAttributeData(
        AttributeData attribute,
        string containingNamespace,
        out LanguageFeatures languageFeatures,
        out string? namespaceOverride,
        out EquatableArray<Diagnostic> diagnostics)
    {
        diagnostics = [];
        languageFeatures = LanguageFeatures.Latest;
        namespaceOverride = default;

        if (attribute.ConstructorArguments.Length < 1)
        {
            // TODO: This would be invalid code (the build would fail),
            // should we emit a diagnostic anyway?
            return default;
        }

        var schemaJson = attribute.ConstructorArguments[0].Value as string;
        if (string.IsNullOrWhiteSpace(schemaJson))
        {
            diagnostics = [InvalidJsonDiagnostic.Create(attribute.ApplicationSyntaxReference!.GetSyntax().GetLocation(), "Cannot be null or whitespace")];
            return default;
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

        return schemaJson;
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
