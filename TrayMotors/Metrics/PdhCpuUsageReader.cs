using System.Diagnostics;

namespace TrayMotors;

public sealed class PdhCpuUsageReader : IDisposable
{
    private PerformanceCounter? counter;
    private bool failed;

    public MetricReading ReadUsage()
    {
        if (failed)
        {
            return MetricReading.Unavailable;
        }

        try
        {
            counter ??= CreateCounter();
            return new MetricReading(counter.NextValue(), SampleSource.Pdh);
        }
        catch (Exception exception)
        {
            failed = true;
            AppLog.Error("PDH CPU counter failed", exception);
            return MetricReading.Unavailable;
        }
    }

    private static PerformanceCounter CreateCounter()
    {
        var counter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total", readOnly: true);
        _ = counter.NextValue();
        return counter;
    }

    public void Dispose() => counter?.Dispose();
}
