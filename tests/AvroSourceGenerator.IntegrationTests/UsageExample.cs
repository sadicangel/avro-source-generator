using System;

namespace AvroSourceGenerator.IntegrationTests;

public static class UsageExample
{
    public static void DoSomethingWithGeneratedRecord(RecordExample record)
    {
        Console.WriteLine(record.UuidField);
        Console.WriteLine(record.DecimalField);
    }

    public static void DoSomethingWithGeneratedError(ErrorExample error)
    {
        Console.WriteLine(error.ErrorCode);
        Console.WriteLine(error.ErrorMessage);
    }
}

[Avro(LanguageFeatures = LanguageFeatures.Latest)]
public partial record MismatchedDeclaration { }
