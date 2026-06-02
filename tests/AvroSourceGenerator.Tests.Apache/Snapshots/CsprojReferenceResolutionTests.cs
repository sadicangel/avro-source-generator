using System.Diagnostics.CodeAnalysis;

namespace AvroSourceGenerator.Tests.Apache.Snapshots;

public sealed class CsprojReferenceResolutionTests
{
    [Fact]
    public Task Verify_Deferred() => Snapshot.Files(
        [
            ProjectFile.Schema(OrderSchema),
            ProjectFile.Schema(CustomerSchema),
            ProjectFile.Schema(OrderLineSchema),
        ],
        config => config with { ReferenceResolution = "Deferred" });

    [Fact]
    public Task Diagnostic_Strict() => Snapshot.Diagnostic(
    [
        ProjectFile.Schema(OrderSchema),
        ProjectFile.Schema(CustomerSchema),
        ProjectFile.Schema(OrderLineSchema),
    ]);

    [Fact]
    public Task Diagnostic_Deferred_MissingReference() => Snapshot.Diagnostic(
        [
            ProjectFile.Schema(OrderSchema),
            ProjectFile.Schema(CustomerSchema),
        ],
        config => config with { ReferenceResolution = "Deferred" });

    [StringSyntax(StringSyntaxAttribute.Json)]
    private const string OrderSchema = """
        {
            "type": "record",
            "name": "Order",
            "namespace": "Demo.Store",
            "fields": [
                {
                    "name": "Customer",
                    "type": "Customer"
                },
                {
                    "name": "Lines",
                    "type": {
                        "type": "array",
                        "items": {
                            "type": "OrderLine"
                        }
                    }
                }
            ]
        }
        """;

    [StringSyntax(StringSyntaxAttribute.Json)]
    private const string CustomerSchema = """
        {
            "type": "record",
            "name": "Customer",
            "namespace": "Demo.Store",
            "fields": [
                {
                    "name": "Name",
                    "type": "string"
                }
            ]
        }
        """;

    [StringSyntax(StringSyntaxAttribute.Json)]
    private const string OrderLineSchema = """
        {
            "type": "record",
            "name": "OrderLine",
            "namespace": "Demo.Store",
            "fields": [
                {
                    "name": "Sku",
                    "type": "string"
                },
                {
                    "name": "Quantity",
                    "type": "int"
                }
            ]
        }
        """;
}
