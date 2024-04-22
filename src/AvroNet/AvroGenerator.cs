using Avro;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CSharp;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Text;

namespace AvroNet;

[Generator(LanguageNames.CSharp)]
internal sealed partial class AvroGenerator : IIncrementalGenerator
{
    internal const string AvroClassAttributeName = "AvroNet.AvroClassAttribute";
    internal const string AvroClassSchemaConstName = "SchemaJson";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(
            static ctx => ctx.AddSource("AvroClassAttribute.g.cs", SourceText.From(AvroClassAttribute, Encoding.UTF8)));

        var infoList = context.SyntaxProvider
            .ForAttributeWithMetadataName(AvroClassAttributeName,
                predicate: static (node, token) => node is ClassDeclarationSyntax @class && @class.Modifiers.Any(SyntaxKind.PartialKeyword),
                transform: static (ctx, token) =>
                {
                    var classDeclaration = Unsafe.As<ClassDeclarationSyntax>(ctx.TargetNode);
                    var classSymbol = Unsafe.As<INamedTypeSymbol>(ctx.TargetSymbol);

                    var schema = default(string);
                    for (int i = 0; i < classDeclaration.Members.Count; ++i)
                    {
                        if (classDeclaration.Members[i] is FieldDeclarationSyntax field)
                        {
                            if (field.Declaration.Variables.FirstOrDefault(v => v.Identifier.Text == AvroClassSchemaConstName) is VariableDeclaratorSyntax variable)
                            {
                                var fieldDeclaration = (IFieldSymbol)ctx.SemanticModel.GetDeclaredSymbol(variable, token)!;
                                if (fieldDeclaration.IsConst)
                                {
                                    schema = (string?)fieldDeclaration.ConstantValue;
                                    break;
                                }
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(schema))
                        throw new NotSupportedException("add a diagnostic here for 'schema is null or empty'");

                    return new AvroClassInfo(classSymbol, schema!);
                });


        context.RegisterSourceOutput(infoList,
            static (ctx, info) => ctx.AddSource($"{info.Class.Name}.AvroClass.g.cs", AvroCodeGenEx.Instance.GenerateSourceText(info)));
    }
}

file readonly record struct AvroClassInfo
{
    public readonly INamedTypeSymbol Class;
    public readonly string Schema;

    public AvroClassInfo(INamedTypeSymbol @class, string schema) : this()
    {
        Class = @class;
        Schema = schema;
    }
}

file sealed class AvroCodeGenEx : CodeGen
{
    public static readonly AvroCodeGenEx Instance = new();

    private AvroCodeGenEx() { }

    public SourceText GenerateSourceText(AvroClassInfo info)
    {
        Schemas.Clear();
        Protocols.Clear();
        NamespaceLookup.Clear();

        var schema = Schema.Parse(info.Schema);
        AddSchema(schema);
        var unit = GenerateCode();
        if (schema is NamedSchema namedSchema)
        {
            var classNameSpace = info.Class.ContainingNamespace?.ToDisplayString();
            if (string.IsNullOrEmpty(classNameSpace))
                throw new NotSupportedException("add a diagnostic here for 'cannot be declared in global namespace'");

            foreach (CodeNamespace schemaNamespace in unit.Namespaces)
            {
                if (schemaNamespace.Name == namedSchema.Namespace)
                {
                    schemaNamespace.Name = classNameSpace;
                    foreach (CodeTypeDeclaration type in schemaNamespace.Types)
                    {
                        if (type.Name == namedSchema.Name)
                        {
                            type.Name = info.Class.Name;
                        }
                    }
                    break;
                }
            }
        }

        var cSharpCodeProvider = new CSharpCodeProvider();
        var codeGeneratorOptions = new CodeGeneratorOptions
        {
            BracingStyle = "C",
            IndentString = "    ",
            BlankLinesBetweenMembers = false,
        };
        using var writer = new StringWriter();
        cSharpCodeProvider.GenerateCodeFromCompileUnit(unit, writer, codeGeneratorOptions);
        var sourceText = writer.ToString();
        return SourceText.From(sourceText, Encoding.UTF8);
    }

    protected override void ProcessSchemas() => base.ProcessSchemas();

    protected override void createSchemaField(Schema schema, CodeTypeDeclaration ctd, bool overrideFlag)
    {
        var type = new CodeTypeReference(typeof(Schema), CodeTypeReferenceOptions.GlobalReference);
        string text = "_SCHEMA";
        var codeMemberField = new CodeMemberField(type, text)
        {
            Attributes = (MemberAttributes)24579,
            InitExpression = new CodeMethodInvokeExpression(
                new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(type), "Parse"),
                new CodeFieldReferenceExpression { FieldName = AvroGenerator.AvroClassSchemaConstName })
        };
        ctd.Members.Add(codeMemberField);

        var codeMemberProperty = new CodeMemberProperty
        {
            Attributes = MemberAttributes.Public,
            Name = "Schema",
            Type = type
        };
        codeMemberProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeTypeReferenceExpression(ctd.Name + "." + text)));
        if (overrideFlag)
        {
            codeMemberProperty.Attributes |= MemberAttributes.Override;
        }
        ctd.Members.Add(codeMemberProperty);
    }
}
