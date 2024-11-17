using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.Tests;
internal static class LanguageVersionHelper
{
        public static LanguageVersion GetLanguageVersion()
        {
#if NET8_0_OR_GREATER
        return LanguageVersion.CSharp12;
#elif NET7_0_OR_GREATER
        return LanguageVersion.CSharp11;
#elif NET6_0_OR_GREATER
        return LanguageVersion.CSharp10;
#elif NET5_0_OR_GREATER
        return LanguageVersion.CSharp9;
#elif NETCOREAPP3_0_OR_GREATER
        return LanguageVersion.CSharp8;
#else
                return LanguageVersion.CSharp7_3;
#endif
        }
}
