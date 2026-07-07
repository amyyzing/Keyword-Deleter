namespace Scanner.Core;

public sealed class ScannerService
{
    public event Action<ScanFinding>? FindingFound;
    public event Action<string>? Diagnostic;

    public async Task<ScanPayload> RunAsync(ScanOptions options, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("This scanner is Windows-only.");

        using var scanCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var ct = scanCts.Token;

        void DiagnosticHandler(string message) => Diagnostic?.Invoke(message);
        ScannerDiagnostics.MessageLogged += DiagnosticHandler;

        try
        {
            if (!options.SkipElevation)
                PrivilegeHelper.EnableScannerPrivileges();

            var keywords = options.Keywords
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => k.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (keywords.Count == 0)
                throw new InvalidOperationException("At least one keyword is required.");

            var prepared = keywords
                .Select((text, index) => PreparedKeyword.Prepare(text, index))
                .ToArray();

            var matcher = KeywordMatcher.Build(prepared);

            bool hasExplicitRoots = options.Roots.Count > 0;
            var roots = hasExplicitRoots
                ? options.Roots.Where(r => !string.IsNullOrWhiteSpace(r)).ToList()
                : DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                    .Select(d => d.RootDirectory.FullName)
                    .ToList();

            var collector = new MatchCollector
            {
                StopOnFirstMatch = options.StopOnFirstMatch,
                CancellationSource = scanCts
            };

            collector.FindingFound += finding => FindingFound?.Invoke(finding);

            var seenFiles = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
            var fileScanner = new FileKeywordScanner(
                matcher,
                seenFiles,
                collector,
                ct,
                pruneMatchedDirectories: options.PruneMatchedDirectories,
                excludedRoots: options.ExcludedRoots,
                enumWorkers: options.DirectoryEnumWorkers,
                readWorkers: options.FileReadWorkers,
                maxReadsPerVolume: options.MaxReadsPerVolume,
                parserWorkers: options.ParserWorkers,
                readBufferBytes: options.ReadBufferBytes,
                deepContentScan: options.DeepContentScan,
                maxContentScanBytes: options.MaxContentScanBytes);
            var unifiedRoots = BuildUnifiedFileRoots(roots, includeArtifactRoots: !hasExplicitRoots);
            int registryWorkers = NormalizeRegistryWorkers(options.RegistryWorkers);

            var tasks = new List<Task>
            {
                fileScanner.RunArtifactRootsAsync(unifiedRoots, ct)
            };

            if (!options.SkipRegistry)
                tasks.Add(new RegistryKeywordScanner(matcher, collector, ct, options.FullRegistryScan).RunAsync(registryWorkers, ct));

            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Expected when the caller cancels or StopOnFirstMatch cancels the scan.
            }

            var findings = collector.GetFindings();
            return new ScanPayload
            {
                CompletedUtc = DateTimeOffset.UtcNow,
                Count = findings.Count,
                Findings = findings
            };
        }
        finally
        {
            ScannerDiagnostics.MessageLogged -= DiagnosticHandler;
        }
    }

    private static int NormalizeRegistryWorkers(int? workerCount)
    {
        int defaultWorkers = Math.Min(4, Math.Max(1, Environment.ProcessorCount / 2));
        return Math.Clamp(workerCount ?? defaultWorkers, 1, 8);
    }

    private static IReadOnlyList<ArtifactRoot> BuildUnifiedFileRoots(IEnumerable<string> fullRoots, bool includeArtifactRoots)
    {
        var result = new List<ArtifactRoot>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddRoot(ArtifactRoot root)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(root.Path)) return;
                string normalized = Path.GetFullPath(root.Path).TrimEnd('\\', '/');
                if (!seen.Add(normalized)) return;
                result.Add(root);
            }
            catch
            {
                // Invalid or inaccessible roots are ignored here; the scanner checks again while running.
            }
        }

        if (includeArtifactRoots)
        {
            foreach (var root in ArtifactCatalog.GetArtifactRoots())
                AddRoot(root);
        }

        foreach (string root in fullRoots)
            AddRoot(new ArtifactRoot("FileSystem", root, false));

        return result;
    }
}
