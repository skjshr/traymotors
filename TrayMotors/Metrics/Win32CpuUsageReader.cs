using System.Runtime.InteropServices;

namespace TrayMotors;

public sealed class Win32CpuUsageReader : IDisposable
{
    private ulong previousIdle;
    private ulong previousKernel;
    private ulong previousUser;
    private bool hasPrevious;

    public MetricReading ReadUsage()
    {
        if (!NativeMethods.GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
        {
            return MetricReading.Unavailable;
        }

        var idle = ToUInt64(idleTime);
        var kernel = ToUInt64(kernelTime);
        var user = ToUInt64(userTime);

        if (!hasPrevious)
        {
            previousIdle = idle;
            previousKernel = kernel;
            previousUser = user;
            hasPrevious = true;
            return new MetricReading(0, SampleSource.Win32);
        }

        var idleDelta = idle - previousIdle;
        var kernelDelta = kernel - previousKernel;
        var userDelta = user - previousUser;
        var total = kernelDelta + userDelta;

        previousIdle = idle;
        previousKernel = kernel;
        previousUser = user;

        if (total == 0)
        {
            return new MetricReading(0, SampleSource.Win32);
        }

        var busy = total > idleDelta ? total - idleDelta : 0;
        return new MetricReading(busy * 100.0 / total, SampleSource.Win32);
    }

    private static ulong ToUInt64(NativeMethods.FileTime fileTime) =>
        ((ulong)fileTime.HighDateTime << 32) | fileTime.LowDateTime;

    public void Dispose()
    {
    }

    private static partial class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct FileTime
        {
            public uint LowDateTime;
            public uint HighDateTime;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);
    }
}
