namespace Scanner.Core;

internal static class PathUtil
{
    public static string NormalizeDirectory(string p) => Path.GetFullPath(p).TrimEnd('\\', '/');
    public static string FileName(string p) => Path.GetFileName(p.TrimEnd('\\', '/'));
}
