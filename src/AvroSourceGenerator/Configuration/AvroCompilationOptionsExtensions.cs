using AvroSourceGenerator.Compiler;

namespace AvroSourceGenerator.Configuration;

internal static class AvroCompilationOptionsExtensions
{
    extension(AvroCompilationOptions)
    {
        public static AvroCompilationOptions FromGeneratorConfiguration(GeneratorConfiguration configuration, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new AvroCompilationOptions(configuration.ReferenceResolution, configuration.DuplicateResolution);
        }
    }
}
