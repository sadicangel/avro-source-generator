using Avro;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.CompilerServices;
using System.Text;

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
            return (node.IsKind(SyntaxKind.ClassDeclaration) || node.IsKind(SyntaxKind.RecordDeclaration))
                && node is TypeDeclarationSyntax @class && @class.Modifiers.Any(SyntaxKind.PartialKeyword);
        }

        static AvroModelOptions GetClassInfo(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
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

            var value = context.Attributes.Single(a => a.AttributeClass?.Name == AvroModelAttributeName);
            var dotnetVersion = (int)((IFieldSymbol)value.AttributeClass!.GetMembers().Single(s => s.Name == "NET")).ConstantValue!;

            return new AvroModelOptions(
                Name: typeSymbol.Name,
                Schema: modelSchema!,
                Namespace: typeSymbol.ContainingNamespace?.ToDisplayString()! ?? "",
                AccessModifier: typeDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword) ? "public" : "internal",
                DeclarationType: typeDeclaration.IsKind(SyntaxKind.RecordDeclaration) ? "partial record" : "partial class",
                DotnetVersion: dotnetVersion
            );
        }

        context.RegisterSourceOutput(infoList, GenerateSourceText);

        static void GenerateSourceText(SourceProductionContext context, AvroModelOptions options)
        {
            using var writer = new SourceTextWriter(options);

            writer.ProcessSchema(Schema.Parse(options.Schema));
            var sourceText = writer.ToString();
            context.AddSource($"{options.Name}.AvroModel.g.cs", SourceText.From(sourceText, Encoding.UTF8));
        }
    }
}
