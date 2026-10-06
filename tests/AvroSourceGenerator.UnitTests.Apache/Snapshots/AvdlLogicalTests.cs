namespace AvroSourceGenerator.UnitTests.Apache.Snapshots;

public sealed class AvdlLogicalTests
{
    [Fact]
    public Task Verify_annotated_fields() => Snapshot.Source(
        """
        schema R;
        record R {
            @logicalType("future-type") string future;
            @logicalType("uuid") bytes unsupportedUuid;
        }
        """);

    [Fact]
    public Task Verify_fixed_decimal_declaration() => Snapshot.Source(
        """
        schema R;
        @logicalType("decimal") @precision(4) @scale(2)
        fixed Amount(4);
        record R { Amount amount; }
        """);

    [Fact]
    public Task Verify_ignored_fixed_uuid_declaration() => Snapshot.Source(
        """
        schema R;
        @logicalType("uuid")
        fixed Identifier(16);
        record R { Identifier identifier; }
        """);

    [Fact]
    public Task Verify_unknown_fixed_declaration() => Snapshot.Source(
        """
        schema R;
        @logicalType("future-type")
        fixed Future(2);
        record R { Future value; }
        """);
}
