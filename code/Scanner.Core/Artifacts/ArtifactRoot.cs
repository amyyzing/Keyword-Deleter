namespace Scanner.Core;

internal readonly struct ArtifactRoot
{
    public string Name { get; }
    public string Path { get; }
    public bool IsFile { get; }
    public bool CleanupEligible { get; }

    public ArtifactRoot(string name, string path, bool isFile, bool cleanupEligible = true)
    {
        Name = name;
        Path = path;
        IsFile = isFile;
        CleanupEligible = cleanupEligible;
    }
}

// Registry Scanner (scan only)
