namespace AvroNet.Example.Models;

#if NET8_0_OR_GREATER
[AvroModel(AvroModelFeatures.Net8)]
#else
[AvroModel(AvroModelFeatures.NetStandard2_0)]
#endif
public sealed partial record class Record
{
    private const string SchemaJson = """
    {
        "namespace": "de.mh.examples.avro",
        "type": "record",
        "name": "User",
        "fields": [
            {
                "name": "name",
                "type": "string",
                "doc": "Full name of User!"
            },
            {
                "name": "email",
                "type": [ "null", "string" ],
                "default": null,
                "doc": "Email address of user"
            },
            {
                "name": "salary",
                "type": {
                    "type": "bytes",
                    "logicalType": "decimal",
                    "precision": 10,
                    "scale": 2
                },
                "default": "\u0000",
                "doc": "Salary of User"
            }
        ]
    }
    """;
}
