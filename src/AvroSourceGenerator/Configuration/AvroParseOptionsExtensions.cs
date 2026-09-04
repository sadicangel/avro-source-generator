using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Templating;

namespace AvroSourceGenerator.Configuration;

internal static class AvroParseOptionsExtensions
{
    extension(AvroParseOptions parseOptions)
    {
        public static AvroParseOptions FromAvroProjectOptions(AvroProjectOptions options, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new AvroParseOptions(
                options.TargetProfile,
                options.LanguageFeatures.HasFlag(LanguageFeatures.NullableReferenceTypes));
        }
    }
}
