using System.Collections.Concurrent;

namespace TrayMotors;

public sealed class CompositeMetricSampler : IMetricSampler, IDisposable
{
    private readonly object lifecycleGate = new();
    private readonly ConcurrentDictionary<ResourceKind, MetricSnapshot> latest = new();
    private readonly ConcurrentDictionary<ResourceKind, double?> temperatures = new();
    private readonly PdhCpuUsageReader pdhCpuUsageReader = new();
    private readonly Win32CpuUsageReader win32CpuUsageReader = new();
    private readonly Win32MemoryUsageReader memoryUsageReader = new();
    private readonly PdhGpuUsageReader gpuUsageReader = new();
    private readonly AppSettings settings;
    private LibreHardwareTemperatureReader? temperatureReader;
    private CancellationTokenSource? cancellationTokenSource;
    private Task? usageTask;
    private Task? temperatureTask;

    public CompositeMetricSampler(AppSettings settings)
    {
        this.settings = settings;

        foreach (var kind in Enum.GetValues<ResourceKind>())
        {
            latest[kind] = MetricSnapshot.Unknown(kind);
        }
    }

    public void Start()
    {
        lock (lifecycleGate)
        {
            if (cancellationTokenSource is not null)
            {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            usageTask = Task.Run(() => RunUsageLoopAsync(cancellationTokenSource.Token));

            if (settings.EnableTemperatureSensors)
            {
                temperatureReader = new LibreHardwareTemperatureReader();
                temperatureTask = Task.Run(() => RunTemperatureLoopAsync(cancellationTokenSource.Token));
            }
        }
    }

    public void Stop()
    {
        CancellationTokenSource? cts;
        Task?[] tasks;

        lock (lifecycleGate)
        {
            cts = cancellationTokenSource;
            if (cts is null)
            {
                return;
            }

            cancellationTokenSource = null;
            tasks = [usageTask, temperatureTask];
            usageTask = null;
            temperatureTask = null;
        }

        cts.Cancel();
        try
        {
            Task.WaitAll(tasks.Where(task => task is not null).Cast<Task>().ToArray(), TimeSpan.FromSeconds(2));
        }
        catch (Exception exception) when (exception is AggregateException or OperationCanceledException)
        {
            AppLog.Info("Metric sampler stopped with pending background work.");
        }
        finally
        {
            cts.Dispose();
            temperatureReader?.Dispose();
            temperatureReader = null;
        }
    }

    public MetricSnapshot GetLatest(ResourceKind kind) =>
        latest.TryGetValue(kind, out var snapshot) ? snapshot : MetricSnapshot.Unknown(kind);

    private async Task RunUsageLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(Math.Max(250, settings.SamplingIntervalMs)));

        while (!cancellationToken.IsCancellationRequested)
        {
            CaptureUsage();

            try
            {
                await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunTemperatureLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(Math.Max(1000, settings.TemperatureIntervalMs)));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var readings = temperatureReader?.ReadTemperatures() ?? new Dictionary<ResourceKind, double?>();
                foreach (var reading in readings)
                {
                    temperatures[reading.Key] = reading.Value;
                }
            }
            catch (Exception exception)
            {
                AppLog.Error("Temperature sampling failed; disabling temperature reader", exception);
                temperatures.Clear();
                break;
            }

            try
            {
                await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void CaptureUsage()
    {
        Capture(ResourceKind.Cpu, ReadCpu());
        Capture(ResourceKind.Memory, memoryUsageReader.ReadUsage());
        Capture(ResourceKind.Gpu, gpuUsageReader.ReadUsage());
    }

    private MetricReading ReadCpu()
    {
        var reading = pdhCpuUsageReader.ReadUsage();
        return reading.Source == SampleSource.Unavailable ? win32CpuUsageReader.ReadUsage() : reading;
    }

    private void Capture(ResourceKind kind, MetricReading reading)
    {
        temperatures.TryGetValue(kind, out var temperature);
        var usage = ClampPercent(reading.UsagePercent);
        latest[kind] = new MetricSnapshot(
            kind,
            usage,
            temperature,
            PressureClassifier.Classify(kind, usage, temperature),
            reading.Source,
            DateTimeOffset.Now);
    }

    private static double ClampPercent(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        return Math.Clamp(value, 0, 100);
    }

    public void Dispose()
    {
        Stop();
        pdhCpuUsageReader.Dispose();
        win32CpuUsageReader.Dispose();
        gpuUsageReader.Dispose();
    }
}
