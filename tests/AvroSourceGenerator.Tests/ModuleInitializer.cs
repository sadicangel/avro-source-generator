using System.Runtime.CompilerServices;

namespace AvroSourceGenerator.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
        VerifyDiffPlex.Initialize();
        Verifier.UseProjectRelativeDirectory("Snapshots");
    }
}
