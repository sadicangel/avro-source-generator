using System.Runtime.CompilerServices;

namespace AvroSourceGenerator.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
        VerifyDiffPlex.Initialize();
        DerivePathInfo((_, projectDirectory, type, method) => new PathInfo(
            directory: projectDirectory,
            typeName: type.Name,
            methodName: method.Name));
    }
}
