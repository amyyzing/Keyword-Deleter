namespace Scanner.Core;

internal readonly struct ParseJob
{
    public string Path { get; }
    public string Source { get; }
    public FileInfo Info { get; }
    public byte[] PrefixBuffer { get; }
    public int PrefixLength { get; }
    public bool CleanupEligible { get; }

    public ParseJob(string path, string source, FileInfo info, byte[] prefixBuffer, int prefixLength, bool cleanupEligible)
    {
        Path = path;
        Source = source;
        Info = info;
        PrefixBuffer = prefixBuffer;
        PrefixLength = prefixLength;
        CleanupEligible = cleanupEligible;
    }
}
