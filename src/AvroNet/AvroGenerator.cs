using AvroNet.Schemas;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace AvroNet;

[Generator(LanguageNames.CSharp)]
internal sealed partial class AvroGenerator : IIncrementalGenerator
{
    internal const string AvroModelAttributeName = "AvroModelAttribute";
    internal const string AvroModelAttributeFullName = $"AvroNet.{AvroModelAttributeName}";
    internal const string AvroClassSchemaConstName = "SchemaJson";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(
            static ctx => ctx.AddSource("AvroModelAttribute.g.cs", SourceText.From(AvroModelAttribute, Encoding.UTF8)));

        var infoList = context.SyntaxProvider
            .ForAttributeWithMetadataName(AvroModelAttributeFullName, IsPartialClassOrRecord, GetClassInfo);

        static bool IsPartialClassOrRecord(SyntaxNode node, CancellationToken cancellationToken)
        {
            var result = (node.IsKind(SyntaxKind.ClassDeclaration) || node.IsKind(SyntaxKind.RecordDeclaration))
                && node is TypeDeclarationSyntax @class && @class.Modifiers.Any(SyntaxKind.PartialKeyword);
            return result;
        }

        static AvroModelOptions GetClassInfo(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            var languageVersion = ((CSharpCompilation)context.SemanticModel.Compilation).LanguageVersion;
            var typeDeclaration = Unsafe.As<TypeDeclarationSyntax>(context.TargetNode);
            var typeSymbol = Unsafe.As<INamedTypeSymbol>(context.TargetSymbol);

            var modelSchema = default(string);
            for (int i = 0; i < typeDeclaration.Members.Count; ++i)
            {
                if (typeDeclaration.Members[i] is FieldDeclarationSyntax field)
                {
                    if (field.Declaration.Variables.FirstOrDefault(v => v.Identifier.Text == AvroClassSchemaConstName) is VariableDeclaratorSyntax variable)
                    {
                        var fieldDeclaration = (IFieldSymbol)context.SemanticModel.GetDeclaredSymbol(variable, cancellationToken)!;
                        if (fieldDeclaration.IsConst)
                        {
                            modelSchema = (string?)fieldDeclaration.ConstantValue;
                            break;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(modelSchema))
                throw new NotSupportedException("add a diagnostic here for 'schema is null or empty'");

            return new AvroModelOptions(
                Name: typeSymbol.Name,
                Schema: modelSchema!,
                Namespace: typeSymbol.ContainingNamespace?.ToDisplayString()! ?? "",
                AccessModifier: typeDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword) ? "public" : "internal",
                DeclarationType: typeDeclaration.IsKind(SyntaxKind.RecordDeclaration) ? "partial record class" : "partial class",
                LanguageVersion: languageVersion
            );
        }

        context.RegisterSourceOutput(infoList, GenerateSourceText);

        static void GenerateSourceText(SourceProductionContext context, AvroModelOptions options)
        {
            using var writer = new SourceTextWriter(options);
            using var document = JsonDocument.Parse(options.Schema);
            writer.Write(new AvroSchema(document.RootElement));
            var sourceText = writer.ToString();
            context.AddSource($"{options.Name}.AvroModel.g.cs", SourceText.From(sourceText, Encoding.UTF8));
        }
    }
}
