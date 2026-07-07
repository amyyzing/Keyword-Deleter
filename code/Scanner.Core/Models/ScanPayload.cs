namespace Scanner.Core;

public sealed class ScanPayload
{
    public DateTimeOffset CompletedUtc { get; set; }
    public int Count { get; set; }
    public IReadOnlyList<ScanFinding> Findings { get; set; } = Array.Empty<ScanFinding>();
}
