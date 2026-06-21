namespace TrayMotors;

public enum ResourceKind
{
    Cpu,
    Memory,
    Gpu
}

public enum PressureState
{
    Normal,
    Warm,
    Hot,
    Critical,
    Unknown
}

public enum SampleSource
{
    Pdh,
    Win32,
    LibreHardwareMonitor,
    Unavailable
}

public sealed record MetricSnapshot(
    ResourceKind ResourceKind,
    double UsagePercent,
    double? TemperatureCelsius,
    PressureState PressureState,
    SampleSource SampleSource,
    DateTimeOffset CapturedAt)
{
    public static MetricSnapshot Unknown(ResourceKind kind) =>
        new(kind, 0, null, PressureState.Unknown, SampleSource.Unavailable, DateTimeOffset.Now);
}
