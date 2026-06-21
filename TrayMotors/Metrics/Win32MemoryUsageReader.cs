using System.Runtime.InteropServices;

namespace TrayMotors;

public sealed class Win32MemoryUsageReader
{
    public MetricReading ReadUsage()
    {
        var status = new NativeMethods.MemoryStatusEx();
        status.Length = (uint)Marshal.SizeOf<NativeMethods.MemoryStatusEx>();

        if (!NativeMethods.GlobalMemoryStatusEx(ref status) || status.TotalPhys == 0)
        {
            return MetricReading.Unavailable;
        }

        var used = status.TotalPhys - status.AvailPhys;
        return new MetricReading(used * 100.0 / status.TotalPhys, SampleSource.Win32);
    }

    private static partial class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct MemoryStatusEx
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhys;
            public ulong AvailPhys;
            public ulong TotalPageFile;
            public ulong AvailPageFile;
            public ulong TotalVirtual;
            public ulong AvailVirtual;
            public ulong AvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);
    }
}
