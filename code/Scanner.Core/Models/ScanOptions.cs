namespace Scanner.Core;

public sealed class ScanOptions
{
    public List<string> Keywords { get; } = new();
    public List<string> Roots { get; } = new();
    public List<string> ExcludedRoots { get; } = new();
    public bool StopOnFirstMatch { get; set; }
    public bool SkipElevation { get; set; }
    public bool DeleteFound { get; set; }
    public bool PruneMatchedDirectories { get; set; }
    public int? FileReadWorkers { get; set; }
    public int? DirectoryEnumWorkers { get; set; }
    public int? MaxReadsPerVolume { get; set; }
    public int? ParserWorkers { get; set; }
    public int? RegistryWorkers { get; set; }
    public int? ReadBufferBytes { get; set; }
    public bool DeepContentScan { get; set; }
    public long? MaxContentScanBytes { get; set; }
    public bool LowImpact { get; set; }
    public bool SkipRegistry { get; set; }
    public bool FullRegistryScan { get; set; }
}
