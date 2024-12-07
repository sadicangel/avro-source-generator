﻿using System.Collections.Immutable;
using System.Diagnostics;
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
internal sealed class AvroSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var models = context.SyntaxProvider
            .ForAttributeWithMetadataName("AvroSourceGenerator.AvroAttribute",
                predicate: static (node, cancellationToken) =>
                {
                    if (!node.IsKind(SyntaxKind.ClassDeclaration) && !node.IsKind(SyntaxKind.RecordDeclaration))
                    {
                        // TODO: Emit diagnostic here for invalid declaration type. Must be a class or record.
                        return false;
                    }

                    return Unsafe.As<TypeDeclarationSyntax>(node).Modifiers.Any(SyntaxKind.PartialKeyword);
                },
                transform: static (context, cancellationToken) =>
                {
                    var declaration = Unsafe.As<TypeDeclarationSyntax>(context.TargetNode);

                    var symbol = Unsafe.As<INamedTypeSymbol>(context.TargetSymbol);

                    var avroAttribute = context.Attributes
                        .Single(attr => attr.AttributeClass?.Name == nameof(AvroAttribute));

                    var languageFeatures = LanguageFeatures.Latest;
                    var namespaceOverride = default(string);

                    foreach (var kvp in avroAttribute.NamedArguments)
                    {
                        var name = kvp.Key;
                        var value = kvp.Value.Value;

                        switch (name)
                        {
                            case nameof(AvroAttribute.LanguageFeatures) when value is not null:
                                languageFeatures = (LanguageFeatures)value;
                                break;
                            case nameof(AvroAttribute.UseCSharpNamespace) when value is true:
                                namespaceOverride = symbol.ContainingNamespace?.ToDisplayString();
                                break;
                        }
                    }

                    var schemaField = GetSchemaField(context, declaration, cancellationToken, out var diagnostics);

                    return new SourceOutputModel(
                        LanguageFeatures: languageFeatures,
                        NamespaceOverride: namespaceOverride,
                        RecordDeclaration: declaration.IsKind(SyntaxKind.RecordDeclaration) ? "record" : "class",
                        AccessModifier: GetAccessModifier(declaration),
                        SchemaField: schemaField,
                        Diagnostics: diagnostics);
                });

        context.RegisterSourceOutput(models, static (context, model) =>
        {
            foreach (var diagnostic in model.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }

            if (!model.SchemaField.IsValid)
            {
                return;
            }

            try
            {
                using var document = JsonDocument.Parse(model.SchemaField.SchemaJson);
                var schemaRegistry = new SchemaRegistry(model.LanguageFeatures, model.NamespaceOverride);
                var rootSchema = schemaRegistry.Register(document.RootElement);

                // We should get no render errors, so we don't have to handle anything else.
                var sourceText = AvroTemplate.Render(
                    schemaRegistry: schemaRegistry,
                    languageFeatures: model.LanguageFeatures,
                    recordDeclaration: model.RecordDeclaration,
                    accessModifier: model.AccessModifier);

                Debug.WriteLine(sourceText);
                context.AddSource($"{rootSchema.Name}.Avro.g.cs", SourceText.From(sourceText, Encoding.UTF8));
            }
            catch (JsonException ex)
            {
                context.ReportDiagnostic(InvalidJsonDiagnostic.Create(model.SchemaField.Location, ex.Message));
            }
            catch (InvalidSchemaException ex)
            {
                context.ReportDiagnostic(InvalidAvroSchemaDiagnostic.Create(model.SchemaField.Location, ex.Message));
            }
        });
    }

    private static SchemaFieldInfo GetSchemaField(GeneratorAttributeSyntaxContext context, TypeDeclarationSyntax declaration, CancellationToken cancellationToken, out ImmutableArray<Diagnostic> diagnostics)
    {
        var schemaVariable = declaration.Members
            .OfType<FieldDeclarationSyntax>()
            .SelectMany(f => f.Declaration.Variables)
            .FirstOrDefault(v => v.Identifier.Text == "AvroSchema");

        if (schemaVariable is null)
        {
            diagnostics = [MissingAvroSchemaMemberDiagnostic.Create(declaration.GetLocation())];
            return default;
        }

        if (context.SemanticModel.GetDeclaredSymbol(schemaVariable, cancellationToken) is not IFieldSymbol schemaSymbol
            || schemaSymbol.IsConst is false || schemaSymbol.ConstantValue is not string { Length: > 0 } schemaJson)
        {
            diagnostics = [InvalidAvroSchemaMemberDiagnostic.Create(schemaVariable.GetLocation())];
            return default;
        }

        var initializer = schemaVariable.Initializer?.Value;
        if (initializer is null)
        {
            diagnostics = [InvalidAvroSchemaMemberDiagnostic.Create(schemaVariable.GetLocation())];
            return default;
        }

        diagnostics = [];

        return new SchemaFieldInfo(schemaJson, initializer.GetLocation());
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
            else
            {
                accessModifier = "protected";
            }
        }
        else if (modifiers.Any(SyntaxKind.PrivateKeyword))
        {
            if (modifiers.Any(SyntaxKind.ProtectedKeyword))
            {
                accessModifier = "private protected";
            }
            else
            {
                accessModifier = "private";
            }
        }
        else if (modifiers.Any(SyntaxKind.FileKeyword))
        {
            accessModifier = "file";
        }

        return accessModifier;
    }
}
