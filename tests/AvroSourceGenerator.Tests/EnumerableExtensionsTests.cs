using AvroSourceGenerator.Extensions;

namespace AvroSourceGenerator.Tests;

public sealed class EnumerableExtensionsTests
{
    [Fact]
    public void WithCancellation_checks_an_empty_sequence()
    {
        var cancellationToken = new CancellationToken(canceled: true);

        Assert.Throws<OperationCanceledException>(() =>
            Enumerable.Empty<int>().WithCancellation(cancellationToken).ToArray());
    }

    [Fact]
    public void WithCancellation_checks_before_each_item()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        using var enumerator = Enumerable.Range(0, 3)
            .WithCancellation(cancellationTokenSource.Token)
            .GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.Equal(0, enumerator.Current);

        cancellationTokenSource.Cancel();

        Assert.Throws<OperationCanceledException>(() => enumerator.MoveNext());
    }
}
