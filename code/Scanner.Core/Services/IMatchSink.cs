namespace Scanner.Core;

internal interface IMatchSink
{
    bool HandleMatch(string source, string kind, string keyword, string location, string? uniqueKey = null, string? evidence = null, bool cleanupEligible = true);
    bool HandleMatch(string source, string kind, string keyword, string location, Func<string>? evidenceFactory, string? uniqueKey = null, bool cleanupEligible = true);
}
