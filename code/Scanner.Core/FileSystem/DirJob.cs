namespace Scanner.Core;

internal readonly struct DirJob
{
    public string Path { get; }
    public string Root { get; }
    public string Source { get; }
    public bool CleanupEligible { get; }

    public DirJob(string path, string root, string source, bool cleanupEligible)
    {
        Path = path;
        Root = root;
        Source = source;
        CleanupEligible = cleanupEligible;
    }
}
