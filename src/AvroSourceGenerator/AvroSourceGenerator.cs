using AvroSourceGenerator.Emit;
using AvroSourceGenerator.Parsing;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class AvroSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var modelProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName("AvroSourceGenerator.AvroAttribute",
                predicate: Parser.IsCandidateDeclaration,
                transform: Parser.GetAvroModel);

        var languageVersionProvider = context.CompilationProvider.Select(Parser.GetLanguageVersion);

        var combinedProvider = modelProvider.Combine(languageVersionProvider);

        context.RegisterImplementationSourceOutput(combinedProvider, Emitter.Emit);
    }
}
