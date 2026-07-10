namespace Scanner.Core;

internal readonly struct FileJob
{
    public string Path { get; }
    public string Root { get; }
    public string Source { get; }
    public bool CleanupEligible { get; }
    public long Length { get; }

    public FileJob(string path, string root, string source, bool cleanupEligible, long length = -1)
    {
        Path = path;
        Root = root;
        Source = source;
        CleanupEligible = cleanupEligible;
        Length = length;
    }
}
