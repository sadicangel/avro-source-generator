namespace AvroSourceGenerator.Tests.Bugs;

public class NestedType
{
    private const string _schema = """
                                   {
                                     "fields": [
                                       {
                                         "description": "The displayName of the customer",
                                         "name": "displayName",
                                         "type": {
                                           "avro.java.string": "String",
                                           "type": "string"
                                         }
                                       }
                                     ],
                                     "name": "SomeSchema",
                                     "type": "record"
                                   }
                                   """;

    [Fact]
    public Task Works() => VerifySourceCode(_schema);
}
