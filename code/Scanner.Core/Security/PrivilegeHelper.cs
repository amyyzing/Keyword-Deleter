namespace Scanner.Core;

internal static class PrivilegeHelper
{
    public static bool IsBackupPrivilegeEnabled { get; private set; }

    public static void EnableScannerPrivileges()
    {
        if (!OperatingSystem.IsWindows()) return;
        EnablePrivilege("SeBackupPrivilege");
        EnablePrivilege("SeSecurityPrivilege");
    }

    public static void EnablePrivilege(string name)
    {
        if (!OpenProcessToken(Process.GetCurrentProcess().Handle, 0x0020 | 0x0008, out IntPtr token)) return;
        try
        {
            if (!LookupPrivilegeValue(null, name, out Luid luid)) return;
            var tp = new TokenPrivileges { PrivilegeCount = 1, Luid = luid, Attributes = 0x00000002 };
            if (AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero) && Marshal.GetLastWin32Error() == 0)
            {
                if (name == "SeBackupPrivilege") IsBackupPrivilegeEnabled = true;
            }
        }
        finally { CloseHandle(token); }
    }

    [StructLayout(LayoutKind.Sequential)] private struct Luid { public uint LowPart; public int HighPart; }
    [StructLayout(LayoutKind.Sequential)] private struct TokenPrivileges { public uint PrivilegeCount; public Luid Luid; public uint Attributes; }
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool OpenProcessToken(IntPtr h, uint a, out IntPtr t);
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)] private static extern bool LookupPrivilegeValue(string? s, string n, out Luid l);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool AdjustTokenPrivileges(IntPtr t, bool d, ref TokenPrivileges n, uint b, IntPtr p, IntPtr r);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CloseHandle(IntPtr h);
}
