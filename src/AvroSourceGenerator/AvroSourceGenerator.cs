﻿using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AvroSourceGenerator.Schemas;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Scriban;
using Scriban.Functions;
using Scriban.Parsing;
using Scriban.Runtime;

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
                    var languageFeatures = (LanguageFeatures)context.Attributes
                        .Single(attr => attr.AttributeClass?.Name == nameof(AvroAttribute))
                        .ConstructorArguments[0].Value!;

                    var declaration = Unsafe.As<TypeDeclarationSyntax>(context.TargetNode);

                    var schemaVariable = declaration.Members
                        .OfType<FieldDeclarationSyntax>()
                        .SelectMany(f => f.Declaration.Variables)
                        .FirstOrDefault(v => v.Identifier.Text == "AvroSchema");

                    if (schemaVariable is null)
                    {
                        // TODO: Emit a diagnostic here for 'schema is null or empty' / must be provided.
                        return null;
                    }

                    if (context.SemanticModel.GetDeclaredSymbol(schemaVariable, cancellationToken) is not IFieldSymbol schemaSymbol)
                    {
                        // TODO: Emit a diagnostic here for 'schema is null or empty' / must be provided.
                        return null;
                    }

                    if (!schemaSymbol.IsConst)
                    {
                        // TODO: Emit a diagnostic here for 'schema must be a constant'.
                        return null;
                    }

                    if (schemaSymbol.ConstantValue is not string { Length: > 0 } schemaJson)
                    {
                        // TODO: Emit a diagnostic here for 'schema is null or empty' / must be provided.
                        return null;
                    }

                    return new SourceOutputInfo(
                        SchemaJson: schemaJson,
                        LanguageFeatures: languageFeatures,
                        IsRecordDeclaration: declaration.IsKind(SyntaxKind.RecordDeclaration),
                        AccessModifier: GetAccessModifier(declaration),
                        Diagnostics: []);
                });

        context.RegisterSourceOutput(models, (context, info) =>
        {
            if (info is null)
            {
                // TODO: Report diagnostics here.
                return;
            }
            var sourceText = TemplateLoader.MainTemplate.Render(CreateContext(info));
            Debug.WriteLine(sourceText);
            context.AddSource($"{"TestClass"}.Avro.g.cs", SourceText.From(sourceText, Encoding.UTF8));
        });
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

    private static TemplateContext CreateContext(SourceOutputInfo info)
    {
        // TODO: Catch and handle JsonException and emit a diagnostic for invalid JSON schema.
        var schemaRegistry = new SchemaRegistry(JsonDocument.Parse(info.SchemaJson), info.LanguageFeatures);

        var builtin = new BuiltinFunctions();
        builtin.Import(new
        {
            SchemaRegistry = schemaRegistry,
            info.IsRecordDeclaration,
            info.AccessModifier,
            info.LanguageFeatures,
            UseNullableReferenceTypes = (info.LanguageFeatures & LanguageFeatures.NullableReferenceTypes) != 0,
            UseFileScopedNamespaces = (info.LanguageFeatures & LanguageFeatures.FileScopedNamespaces) != 0,
            UseRequiredProperties = (info.LanguageFeatures & LanguageFeatures.RequiredProperties) != 0,
            UseInitOnlyProperties = (info.LanguageFeatures & LanguageFeatures.InitOnlyProperties) != 0,
            UseUnsafeAccessors = (info.LanguageFeatures & LanguageFeatures.UnsafeAccessors) != 0,
        },
        null,
        static member => member.Name);
        var context = new TemplateContext(builtin)
        {
            MemberRenamer = static member => member.Name,
            TemplateLoader = TemplateLoader.Instance,
        };
        return context;
    }
}

internal sealed record class SourceOutputInfo(
    string SchemaJson,
    LanguageFeatures LanguageFeatures,
    bool IsRecordDeclaration,
    string AccessModifier,
    ImmutableArray<Diagnostic> Diagnostics);

file sealed class TemplateLoader : ITemplateLoader
{
    private static readonly Dictionary<string, string> s_templatePaths = new()
    {
        ["enum"] = "AvroSourceGenerator.Templates.enum.sbncs",
        ["error"] = "AvroSourceGenerator.Templates.error.sbncs",
        ["field"] = "AvroSourceGenerator.Templates.field.sbncs",
        ["fixed"] = "AvroSourceGenerator.Templates.fixed.sbncs",
        ["getput"] = "AvroSourceGenerator.Templates.getput.sbncs",
        ["main"] = "AvroSourceGenerator.Templates.main.sbncs",
        ["record"] = "AvroSourceGenerator.Templates.record.sbncs",
        ["schema"] = "AvroSourceGenerator.Templates.schema.sbncs",
    };

    private static readonly Lazy<Template> s_mainTemplate = new(() =>
    {
        using var stream = typeof(AvroSourceGenerator).Assembly.GetManifestResourceStream(s_templatePaths["main"]);
        using var reader = new StreamReader(stream);
        return Template.Parse(reader.ReadToEnd());
    });

    public static ITemplateLoader Instance { get; } = new TemplateLoader();

    public static Template MainTemplate => s_mainTemplate.Value;

    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) =>
        s_templatePaths[templateName];

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        using var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(templatePath));
        return reader.ReadToEnd();
    }

    public async ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath) =>
        await Task.FromResult(Load(context, callerSpan, templatePath));
}
