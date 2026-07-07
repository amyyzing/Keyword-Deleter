namespace Scanner.Core;

internal sealed class MatchCollector : IMatchSink
{
    private readonly ConcurrentDictionary<string, byte> _seenMatches = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentBag<ScanFinding> _findings = new();
    private long _sequence;

    public bool StopOnFirstMatch { get; init; }
    public CancellationTokenSource? CancellationSource { get; init; }
    public event Action<ScanFinding>? FindingFound;

    public bool HandleMatch(string source, string kind, string keyword, string location, string? uniqueKey = null, string? evidence = null, bool cleanupEligible = true)
    {
        return HandleMatchCore(source, kind, keyword, location, uniqueKey, evidence, null, cleanupEligible);
    }

    public bool HandleMatch(string source, string kind, string keyword, string location, Func<string>? evidenceFactory, string? uniqueKey = null, bool cleanupEligible = true)
    {
        return HandleMatchCore(source, kind, keyword, location, uniqueKey, null, evidenceFactory, cleanupEligible);
    }

    private bool HandleMatchCore(string source, string kind, string keyword, string location, string? uniqueKey, string? evidence, Func<string>? evidenceFactory, bool cleanupEligible)
    {
        uniqueKey ??= $"{source}|{kind}|{keyword}|{location}";
        if (!_seenMatches.TryAdd(uniqueKey, 0))
            return false;

        var finding = new ScanFinding
        {
            Id = Interlocked.Increment(ref _sequence),
            FoundUtc = DateTimeOffset.UtcNow,
            Source = source,
            Kind = kind,
            Keyword = keyword,
            Location = location,
            Evidence = evidence ?? evidenceFactory?.Invoke(),
            CleanupEligible = cleanupEligible
        };

        _findings.Add(finding);
        FindingFound?.Invoke(finding);

        if (StopOnFirstMatch)
            CancellationSource?.Cancel();

        return true;
    }

    public IReadOnlyList<ScanFinding> GetFindings()
    {
        return _findings.OrderBy(f => f.Id).ToArray();
    }
}
