
namespace AvroNet.Example;

[AvroClass]
public partial class User
{
    public const string SchemaJson = """
    {
        "type" : "record",
        "namespace" : "Tests.User",
        "name" : "User",
        "fields" : [
            { "name" : "Name" , "type" : "string" },
            { "name" : "Age" , "type" : "int" },
            { "name" : "Description" , "type" : [ "string", "null" ] },
        ]
    }
    """;
}
