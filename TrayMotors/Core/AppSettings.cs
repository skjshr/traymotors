namespace TrayMotors;

public sealed record AppSettings
{
    public static AppSettings Default { get; } = new();

    public bool StartWithWindows { get; init; }

    public bool EnableTemperatureSensors { get; init; }

    public int MaxIconFps { get; init; } = 12;

    public int SamplingIntervalMs { get; init; } = 500;

    public int TemperatureIntervalMs { get; init; } = 2000;

    public CpuMode CpuMode { get; init; } = CpuMode.TaskManagerApprox;

    public AppSettings Normalized() =>
        this with
        {
            MaxIconFps = Math.Clamp(MaxIconFps, 1, 24),
            SamplingIntervalMs = Math.Clamp(SamplingIntervalMs, 250, 5000),
            TemperatureIntervalMs = Math.Clamp(TemperatureIntervalMs, 1000, 30000)
        };
}
