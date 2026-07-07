namespace Scanner.Core;

internal static class PrivilegedFile
{
    public static FileStream OpenRead(string path, int bufSize, bool backup)
    {
        var handle = CreateFileW(path, 0x80000000, 0x7, IntPtr.Zero, 3,
            (uint)(0x80 | 0x08000000 | 0x40000000 | (backup ? 0x02000000 : 0)), IntPtr.Zero);
        if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
        return new FileStream(handle, FileAccess.Read, bufSize, true);
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFileW(string fn, uint acc, uint share,
        IntPtr sec, uint disp, uint flags, IntPtr tmpl);
}
