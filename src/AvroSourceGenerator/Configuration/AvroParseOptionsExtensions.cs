using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Templating;

namespace AvroSourceGenerator.Configuration;

internal static class AvroParseOptionsExtensions
{
    extension(AvroParseOptions)
    {
        public static AvroParseOptions FromGeneratorConfiguration(GeneratorConfiguration configuration, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new AvroParseOptions(
                configuration.GenerationTarget,
                configuration.LanguageFeatures.HasFlag(LanguageFeatures.NullableReferenceTypes));
        }
    }
}
