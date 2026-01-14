using System.Runtime.CompilerServices;

namespace AvroSourceGenerator.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
        VerifyDiffPlex.Initialize();
        DerivePathInfo((sourceFile, projectDirectory, type, method) => new PathInfo(
            directory: Path.Combine(projectDirectory, Path.GetDirectoryName(sourceFile) ?? string.Empty),
            typeName: type.Name,
            methodName: method.Name));
    }
}
