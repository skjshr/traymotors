namespace TrayMotors;

public readonly record struct MetricReading(double UsagePercent, SampleSource Source)
{
    public static MetricReading Unavailable => new(0, SampleSource.Unavailable);
}
