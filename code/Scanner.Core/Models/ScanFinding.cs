namespace Scanner.Core;

public sealed class ScanFinding
{
    public long Id { get; set; }
    public DateTimeOffset FoundUtc { get; set; }
    public string Source { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Keyword { get; set; } = "";
    public string Location { get; set; } = "";
    public string? Evidence { get; set; }
    public bool CleanupEligible { get; set; } = true;
}
