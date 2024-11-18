using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AvroSourceGenerator.Output;
using AvroSourceGenerator.Schemas;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator;

[Generator(LanguageNames.CSharp)]
internal sealed partial class AvroSourceGenerator : IIncrementalGenerator
{
    internal const string AvroAttributeFullName = $"AvroSourceGenerator.{nameof(AvroAttribute)}";
    internal const string AvroSchemaName = "AvroSchema";
    internal const string AvroSchemaTypeName = "global::Avro.Schema";
    internal const string AvroFixedSchemaTypeName = "global::Avro.FixedSchema";
    internal const string AvroISpecificRecordTypeName = "global::Avro.Specific.ISpecificRecord";
    internal const string AvroSpecificExceptionTypeName = "global::Avro.Specific.SpecificException";
    internal const string AvroSpecificFixedTypeName = "global::Avro.Specific.SpecificFixed";
    internal const string AvroGenericFixedTypeName = "global::Avro.Generic.GenericFixed";

    internal static readonly AssemblyName AssemblyName = typeof(AvroSourceGenerator).Assembly.GetName();
    internal static readonly string GeneratedCodeAttribute = $@"global::System.CodeDom.Compiler.GeneratedCodeAttribute(""{AssemblyName.Name}"", ""{AssemblyName.Version}"")";
    internal static readonly string ExcludeFromCodeCoverageAttribute = "global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var models = context.SyntaxProvider
            .ForAttributeWithMetadataName(AvroAttributeFullName,
                predicate: static (node, cancellationToken) =>
                {
                    if (!node.IsKind(SyntaxKind.ClassDeclaration) && !node.IsKind(SyntaxKind.RecordDeclaration))
                        return false;

                    return Unsafe.As<TypeDeclarationSyntax>(node).Modifiers.Any(SyntaxKind.PartialKeyword);
                },
                transform: static (context, cancellationToken) =>
                {
                    var typeDeclaration = Unsafe.As<TypeDeclarationSyntax>(context.TargetNode);
                    var typeSymbol = Unsafe.As<INamedTypeSymbol>(context.TargetSymbol);
                    var modelFeatures = (LanguageFeatures)context.Attributes
                        .Single(attr => attr.AttributeClass?.Name == nameof(AvroAttribute))
                        .ConstructorArguments[0].Value!;

                    var schemaJsonValue = default(string);
                    for (var i = 0; i < typeDeclaration.Members.Count; ++i)
                    {
                        if (typeDeclaration.Members[i] is FieldDeclarationSyntax field)
                        {
                            var schemaJson = field.Declaration.Variables
                                .FirstOrDefault(v => v.Identifier.Text == AvroSchemaName);
                            if (schemaJson is not null)
                            {
                                var schemaJsonSymbol = (IFieldSymbol)context.SemanticModel
                                    .GetDeclaredSymbol(schemaJson, cancellationToken)!;
                                if (schemaJsonSymbol.IsConst)
                                {
                                    schemaJsonValue = (string?)schemaJsonSymbol.ConstantValue;
                                    break;
                                }
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(schemaJsonValue))
                        throw new NotSupportedException("add a diagnostic here for 'schema is null or empty'");

                    return new SourceTextWriterOptions(
                        Name: typeSymbol.Name,
                        Namespace: typeSymbol.ContainingNamespace?.ToDisplayString()! ?? "",
                        AvroSchema: schemaJsonValue!,
                        AccessModifier: typeDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword) ? "public" : "internal",
                        DeclarationType: typeDeclaration.IsKind(SyntaxKind.RecordDeclaration) ? "partial record class" : "partial class",
                        Features: modelFeatures
                    );
                });

        context.RegisterSourceOutput(models, static (context, options) =>
        {
            using var document = JsonDocument.Parse(options.AvroSchema);

            var writer = new ApacheAvroSourceTextWriter(new IndentedStringBuilder(), options);
            writer.Write(new AvroSchema(document.RootElement));

            var sourceText = writer.ToString();
            context.AddSource($"{options.Name}.Avro.g.cs", SourceText.From(sourceText, Encoding.UTF8));
        });
    }
}
