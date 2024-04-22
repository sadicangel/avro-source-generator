using Xunit;

namespace AvroNet.IntegrationTests;

public class AvroGeneratorIntegrationTests
{
    [Fact]
    public void Generate_User()
    {
        var user = new User
        {
            Name = "John Doe",
            Age = 69,
        };

        Console.WriteLine($"Name: {user.Name}; Age: {user.Age}");
    }
}

[AvroClass]
partial class User
{
    private const string SchemaJson = """
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
