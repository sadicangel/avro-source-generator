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
using Scriban.Runtime;

namespace AvroSourceGenerator;

[Generator(LanguageNames.CSharp)]
internal sealed class AvroSourceGenerator : IIncrementalGenerator
{
    private const string AvroAttributeFullName = $"AvroSourceGenerator.{nameof(AvroAttribute)}";
    private const string AvroSchemaName = "AvroSchema";

    internal static readonly Assembly Assembly = typeof(AvroSourceGenerator).Assembly;
    internal static readonly AssemblyName AssemblyName = Assembly.GetName();
    internal static readonly Template MainTemplate = LoadMainTemplate();

    private static Template LoadMainTemplate()
    {
        using var reader = new StreamReader(Assembly.GetManifestResourceStream("AvroSourceGenerator.Templates.main.sbncs"));
        return Template.Parse(reader.ReadToEnd());
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var models = context.SyntaxProvider
            .ForAttributeWithMetadataName(AvroAttributeFullName,
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
                    var rootSchemaJson = default(string);
                    for (var i = 0; i < declaration.Members.Count; ++i)
                    {
                        if (declaration.Members[i] is FieldDeclarationSyntax field)
                        {
                            var schemaJson = field.Declaration.Variables
                                .FirstOrDefault(v => v.Identifier.Text == AvroSchemaName);
                            if (schemaJson is not null)
                            {
                                var schemaJsonSymbol = (IFieldSymbol)context.SemanticModel
                                    .GetDeclaredSymbol(schemaJson, cancellationToken)!;
                                if (schemaJsonSymbol.IsConst)
                                {
                                    rootSchemaJson = (string?)schemaJsonSymbol.ConstantValue;
                                    break;
                                }
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(rootSchemaJson))
                    {
                        // TODO: Emit a diagnostic here for 'schema is null or empty' / must be provided.
                        return null;
                    }

                    var rootSchema = default(JsonDocument);
                    try
                    {
                        rootSchema = JsonDocument.Parse(rootSchemaJson!);
                    }
                    catch (JsonException ex)
                    {
                        _ = ex;
                        // TODO: Emit a diagnostic here for 'schema is invalid' / must be valid AVRO JSON Schema.
                        return null;
                    }


                    languageFeatures &= ~LanguageFeatures.RequiredProperties;

                    var schemaRegistry = new SchemaRegistry(rootSchema, languageFeatures);

                    var isRecordDeclaration = declaration.IsKind(SyntaxKind.RecordDeclaration);

                    var accessModifier = GetAccessModifier(declaration);

                    return CreateContext(
                        schemaRegistry,
                        isRecordDeclaration,
                        accessModifier,
                        languageFeatures
                    );
                });

        context.RegisterSourceOutput(models, static (sourceProductionContext, templateContext) =>
        {
            var names = Assembly.GetManifestResourceNames();
            var sourceText = MainTemplate.Render(templateContext);
            sourceProductionContext.AddSource($"{"TestClass"}.Avro.g.cs", SourceText.From(sourceText, Encoding.UTF8));
        });
    }

    public static string GetAccessModifier(TypeDeclarationSyntax typeDeclaration)
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

    private static TemplateContext CreateContext(
        SchemaRegistry schemaRegistry,
        bool isRecordDeclaration,
        string accessModifier,
        LanguageFeatures languageFeatures)
    {
        var builtin = new BuiltinFunctions();
        builtin.Import(new
        {
            SchemaRegistry = schemaRegistry,
            IsRecordDeclaration = isRecordDeclaration,
            AccessModifier = accessModifier,
            LanguageFeatures = languageFeatures,
            UseNullableReferenceTypes = (languageFeatures & LanguageFeatures.NullableReferenceTypes) != 0,
            UseFileScopedNamespaces = (languageFeatures & LanguageFeatures.FileScopedNamespaces) != 0,
            UseRequiredProperties = (languageFeatures & LanguageFeatures.RequiredProperties) != 0,
            UseInitOnlyProperties = (languageFeatures & LanguageFeatures.InitOnlyProperties) != 0,
            UseUnsafeAccessors = (languageFeatures & LanguageFeatures.UnsafeAccessors) != 0,
        },
        null,
        static member => member.Name);
        var context = new TemplateContext(builtin)
        {
            MemberRenamer = static member => member.Name,
            TemplateLoader = new TemplateLoader(),
        };
        return context;
    }
}
