using AvroSourceGenerator.Templating;

namespace AvroSourceGenerator.Configuration;

internal static class RenderOptionsExtensions
{
    extension(RenderOptions)
    {
        public static RenderOptions FromGeneratorConfiguration(GeneratorConfiguration configuration, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new RenderOptions(configuration.GenerationTarget, configuration.LanguageFeatures, configuration.AccessModifier);
        }
    }
}
