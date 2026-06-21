using LibreHardwareMonitor.Hardware;

namespace TrayMotors;

public sealed class LibreHardwareTemperatureReader : IDisposable
{
    private readonly Computer computer;

    public LibreHardwareTemperatureReader()
    {
        computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true
        };

        try
        {
            computer.Open();
        }
        catch (Exception exception)
        {
            AppLog.Error("LibreHardwareMonitor initialization failed", exception);
            throw;
        }
    }

    public IReadOnlyDictionary<ResourceKind, double?> ReadTemperatures()
    {
        var result = new Dictionary<ResourceKind, double?>();

        foreach (var hardware in computer.Hardware)
        {
            hardware.Update();
            UpdateHardware(hardware, result);
        }

        return result;
    }

    private static void UpdateHardware(IHardware hardware, IDictionary<ResourceKind, double?> result)
    {
        var kind = hardware.HardwareType switch
        {
            HardwareType.Cpu => ResourceKind.Cpu,
            HardwareType.GpuAmd or HardwareType.GpuNvidia or HardwareType.GpuIntel => ResourceKind.Gpu,
            HardwareType.Memory => ResourceKind.Memory,
            _ => (ResourceKind?)null
        };

        if (kind is { } resourceKind)
        {
            var temperature = hardware.Sensors
                .Where(sensor => sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                .Select(sensor => (double)sensor.Value!.Value)
                .DefaultIfEmpty(double.NaN)
                .Max();

            if (!double.IsNaN(temperature))
            {
                result[resourceKind] = temperature;
            }
        }

        foreach (var subHardware in hardware.SubHardware)
        {
            subHardware.Update();
            UpdateHardware(subHardware, result);
        }
    }

    public void Dispose() => computer.Close();
}
