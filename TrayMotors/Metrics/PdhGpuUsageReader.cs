using System.Diagnostics;

namespace TrayMotors;

public sealed class PdhGpuUsageReader : IDisposable
{
    private const int MaxCounterInstances = 96;
    private readonly object gate = new();
    private List<PerformanceCounter>? counters;
    private DateTimeOffset lastDiscovery = DateTimeOffset.MinValue;

    public MetricReading ReadUsage()
    {
        try
        {
            EnsureCounters();
            if (counters is null || counters.Count == 0)
            {
                return MetricReading.Unavailable;
            }

            var sum = 0.0;
            foreach (var counter in counters)
            {
                sum += SafeNextValue(counter);
            }

            return new MetricReading(Math.Clamp(sum, 0, 100), SampleSource.Pdh);
        }
        catch (Exception exception)
        {
            AppLog.Error("PDH GPU counters failed", exception);
            DisposeCounters();
            return MetricReading.Unavailable;
        }
    }

    private void EnsureCounters()
    {
        lock (gate)
        {
            if (counters is { Count: > 0 })
            {
                return;
            }

            if (DateTimeOffset.Now - lastDiscovery < TimeSpan.FromSeconds(10))
            {
                return;
            }

            lastDiscovery = DateTimeOffset.Now;
            DisposeCounters();

            if (!PerformanceCounterCategory.Exists("GPU Engine"))
            {
                counters = [];
                return;
            }

            var category = new PerformanceCounterCategory("GPU Engine");
            var selectedInstances = category
                .GetInstanceNames()
                .Where(name => name.Contains("engtype_", StringComparison.OrdinalIgnoreCase))
                .OrderBy(EnginePriority)
                .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Take(MaxCounterInstances)
                .ToArray();

            counters = selectedInstances
                .Select(CreateCounter)
                .Where(counter => counter is not null)
                .Cast<PerformanceCounter>()
                .ToList();

            if (selectedInstances.Length == MaxCounterInstances)
            {
                AppLog.Info($"GPU counter discovery capped at {MaxCounterInstances} instances.");
            }

            foreach (var counter in counters)
            {
                _ = SafeNextValue(counter);
            }
        }
    }

    private static PerformanceCounter? CreateCounter(string instanceName)
    {
        try
        {
            return new PerformanceCounter("GPU Engine", "Utilization Percentage", instanceName, readOnly: true);
        }
        catch (Exception exception)
        {
            AppLog.Error($"GPU counter skipped: {instanceName}", exception);
            return null;
        }
    }

    private static int EnginePriority(string instanceName)
    {
        if (instanceName.Contains("engtype_3D", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (instanceName.Contains("engtype_Compute", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (instanceName.Contains("engtype_Video", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (instanceName.Contains("engtype_Copy", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        return 4;
    }

    private static double SafeNextValue(PerformanceCounter counter)
    {
        try
        {
            return counter.NextValue();
        }
        catch
        {
            return 0;
        }
    }

    private void DisposeCounters()
    {
        if (counters is null)
        {
            return;
        }

        foreach (var counter in counters)
        {
            counter.Dispose();
        }

        counters = null;
    }

    public void Dispose() => DisposeCounters();
}
