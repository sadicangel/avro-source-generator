using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests.Chr.RoundtripTests;

public class SelfReferencesTests(DockerFixture dockerFixture)
{
    [Fact]
    public async Task Types_with_self_references_remain_unchanged_after_roundtrip_to_kafka()
    {
        Assert.Skip("Self references are not supported out of the box");
        var expected = Create();

        var actual = await dockerFixture.RoundtripAsync(expected, SelfReference.GetSchema(), TestContext.Current.CancellationToken);

        Assert.EqualAsJson(expected, actual);
    }

    private static SelfReference Create()
    {
        var next1 = new SelfReference
        {
            items = [],
            lookup = new Dictionary<string, SelfReference>(),
            next = null
        };

        var root = new SelfReference
        {
            items =
            [
                new SelfReference
                {
                    items = [],
                    lookup = new Dictionary<string, SelfReference>(),
                    next = null
                }
            ],
            lookup = new Dictionary<string, SelfReference>
            {
                ["next1"] = next1,
            },
            next = next1
        };

        root.lookup["self"] = root;

        return root;
    }
}
