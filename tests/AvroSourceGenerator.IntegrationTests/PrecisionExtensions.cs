namespace AvroSourceGenerator.IntegrationTests;

public static class PrecisionExtensions
{
    extension(DateTime dateTime)
    {
        // Kafka seems to store with less precision, so we fix that here for comparison.
        public DateTime WithPrecisionLossFixed() => dateTime.Date + TimeSpan.FromMilliseconds((long)dateTime.TimeOfDay.TotalMilliseconds);
    }

    extension(DateTimeOffset dateTime)
    {
        // Kafka seems to store with less precision, so we fix that here for comparison.
        public DateTimeOffset WithPrecisionLossFixed() => dateTime.Date + TimeSpan.FromMilliseconds((long)dateTime.TimeOfDay.TotalMilliseconds);
    }
}
