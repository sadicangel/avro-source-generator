namespace AvroSourceGenerator.Extensions;

internal static class EnumerableExtensions
{
    extension<T>(IEnumerable<T> source)
    {
        public IEnumerable<T> WithCancellation(CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(source);
            using var enumerator = source.GetEnumerator();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!enumerator.MoveNext())
                    yield break;

                yield return enumerator.Current;
            }
        }
    }
}
