using Scanner.Core;
using System.Text;

internal static class Program
{
    private static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "scanner-artifact-catalog-test-" + Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(root);

            string windows = Path.Combine(root, "Windows");
            string programData = Path.Combine(root, "ProgramData");
            string usersRoot = Path.Combine(root, "Users");
            string profile = Path.Combine(usersRoot, "Alice");
            string local = Path.Combine(profile, "AppData", "Local");
            string roaming = Path.Combine(profile, "AppData", "Roaming");

            Touch(Path.Combine(windows, "System32", "Config", "SYSTEM"));
            Touch(Path.Combine(windows, "System32", "Config", "SOFTWARE.LOG1"));
            Touch(Path.Combine(windows, "System32", "Config", "SYSTEM.regtrans-ms"));
            Directory.CreateDirectory(Path.Combine(windows, "System32", "winevt", "Logs"));
            Touch(Path.Combine(windows, "System32", "winevt", "Logs", "Application.evtx"));
            Touch(Path.Combine(programData, "Microsoft", "Network", "Downloader", "qmgr0.dat"));
            Directory.CreateDirectory(Path.Combine(programData, "Microsoft", "Windows Defender", "Scans", "History", "Service", "DetectionHistory"));
            Directory.CreateDirectory(Path.Combine(local, "BraveSoftware", "Brave-Browser", "User Data"));
            Directory.CreateDirectory(Path.Combine(local, "Packages", "TheBrowserCompany.Arc_abc", "LocalCache", "Local", "Arc", "User Data"));
            Directory.CreateDirectory(Path.Combine(roaming, "zen", "Profiles"));
            Directory.CreateDirectory(Path.Combine(roaming, "Microsoft", "Windows", "Recent"));
            Directory.CreateDirectory(Path.Combine(roaming, "Microsoft", "Windows", "Recent", "AutomaticDestinations"));
            Directory.CreateDirectory(Path.Combine(roaming, "Microsoft", "Windows", "Recent", "CustomDestinations"));
            Touch(Path.Combine(profile, "NTUSER.DAT"));

            var environment = new ArtifactCatalog.ArtifactCatalogEnvironment
            {
                Windows = windows,
                SystemDrive = root,
                UsersRoot = usersRoot,
                ProgramData = programData,
                ProgramFiles = Path.Combine(root, "Program Files"),
                ProgramFilesX86 = Path.Combine(root, "Program Files (x86)"),
                FixedDriveRoots = [root]
            };

            var roots = ArtifactCatalog.GetArtifactRoots(environment);

            Assert(roots.Any(r => r.Name == "SystemHive-SYSTEM" && !r.CleanupEligible), "SYSTEM hive should be scan-only.");
            Assert(roots.Any(r => r.Name == "SystemHive-SYSTEM-RegTrans" && !r.CleanupEligible), "Registry transaction sidecar should expand.");
            Assert(roots.Any(r => r.Name == "BITS-Qmgr" && !r.CleanupEligible), "BITS qmgr wildcard should expand as scan-only.");
            Assert(roots.Any(r => r.Name == "EventLogs" && !r.CleanupEligible), "Event log root should be scan-only.");
            Assert(roots.Any(r => r.Name == "DefenderDetectionHistory" && !r.CleanupEligible), "Defender history should be scan-only.");
            Assert(roots.Any(r => r.Name == "RecentFiles" && r.CleanupEligible), "Explorer Recent files should be cleanup-eligible.");
            Assert(roots.Any(r => r.Name == "JumpLists-AutomaticDestinations" && r.CleanupEligible), "Explorer automatic Jump Lists should be cleanup-eligible.");
            Assert(roots.Any(r => r.Name == "JumpLists-CustomDestinations" && r.CleanupEligible), "Explorer custom Jump Lists should be cleanup-eligible.");
            Assert(roots.Any(r => r.Name == "BraveUserData" && r.CleanupEligible), "Brave profile should be cleanup-eligible.");
            Assert(roots.Any(r => r.Name == "ArcUserData" && r.CleanupEligible), "Arc wildcard profile should expand.");
            Assert(roots.GroupBy(r => (Path: Normalize(r.Path), r.IsFile)).All(g => g.Count() == 1), "Artifact roots should not contain duplicate paths.");

            string cleanupCandidate = Path.Combine(root, "candidate.txt");
            Touch(cleanupCandidate);
            var findings = new[]
            {
                new ScanFinding { Kind = "FILE-NAME", Location = Path.Combine(root, "scan-only.txt"), CleanupEligible = false },
                new ScanFinding { Kind = "FILE-NAME", Location = cleanupCandidate, CleanupEligible = true }
            };
            Assert(FindingCleanupService.CountTargets(findings) == 1, "Scan-only findings should not become cleanup targets.");

            RunArtifactClassificationTests();
            RunTextMatcherFastPathTest();
            RunByteMatcherDifferentialTest();
            RunSmallFileScan(root).GetAwaiter().GetResult();
            RunMultipleRootScan(root).GetAwaiter().GetResult();
            RunChunkBoundaryScan(root).GetAwaiter().GetResult();
            RunConcurrentRandomAccessScan(root).GetAwaiter().GetResult();
            RunSmartContentGateScan(root).GetAwaiter().GetResult();
            RunArchiveEntryScan(root).GetAwaiter().GetResult();
            RunDeepArchiveCoverageScan(root).GetAwaiter().GetResult();
            RunCleanupDeleteScan(root).GetAwaiter().GetResult();

            Console.WriteLine("Scanner.Core catalog tests passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    private static void Touch(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "keyword");
    }

    private static string Normalize(string path)
    {
        try { return Path.GetFullPath(path).TrimEnd('\\', '/'); }
        catch { return path.TrimEnd('\\', '/'); }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void RunTextMatcherFastPathTest()
    {
        var matcher = KeywordMatcher.Build(
        [
            PreparedKeyword.Prepare("mixed-case", 0),
            PreparedKeyword.Prepare("caf\u00e9", 1)
        ]);

        var asciiHits = matcher.SearchDecodedText(@"C:\Temp\MIXED-CASE.txt").ToArray();
        Assert(asciiHits.Any(hit => hit.Keyword.Index == 0),
            "ASCII transition table should preserve case-insensitive path matching.");

        var unicodeHits = matcher.SearchDecodedText("prefix CAF\u00c9 suffix").ToArray();
        Assert(unicodeHits.Any(hit => hit.Keyword.Index == 1),
            "ASCII transition table should fall back correctly for Unicode text.");
    }

    private static void RunByteMatcherDifferentialTest()
    {
        var prepared = new[]
        {
            PreparedKeyword.Prepare("alpha", 0),
            PreparedKeyword.Prepare("beta", 1),
            PreparedKeyword.Prepare("gamma", 2)
        };
        var matcher = KeywordMatcher.Build(prepared);
        Assert(matcher.UsesCompactTransitions,
            "Small keyword sets should use compact 16-bit transition tables.");

        byte[] data = Enumerable.Repeat((byte)'x', 20_000).ToArray();
        CopyInto(data, 4_094, Encoding.ASCII.GetBytes("aLpHa"));
        CopyInto(data, 255, Encoding.Unicode.GetBytes("BeTa"));
        CopyInto(data, 8_190, Encoding.BigEndianUnicode.GetBytes("gAmMa"));

        foreach (int chunkSize in new[] { 17, 257, 4_096, 65_536 })
        {
            var tracker = new KeywordHitTracker(prepared.Length);
            var found = new HashSet<int>();
            int state = 0;
            int remaining = matcher.ByteSearchableKeywordCount;

            for (int offset = 0; offset < data.Length; offset += chunkSize)
            {
                int length = Math.Min(chunkSize, data.Length - offset);
                matcher.SearchBytesUnique(
                    data.AsMemory(offset, length),
                    ref state,
                    tracker,
                    ref remaining,
                    hit =>
                    {
                        found.Add(hit.Keyword.Index);
                        return true;
                    });
            }

            Assert(remaining == 0 && found.SetEquals(new[] { 0, 1, 2 }),
                $"Compact matcher/prefilter lost an encoded or boundary-spanning hit at chunk size {chunkSize}.");
        }
    }

    private static void CopyInto(byte[] destination, int offset, byte[] source) =>
        Buffer.BlockCopy(source, 0, destination, offset, source.Length);

    private static void RunArtifactClassificationTests()
    {
        (string Path, string Expected)[] fileCases =
        [
            (@"C:\WINDOWS\Prefetch\APP.EXE-1234.pf", "Prefetch-Execution"),
            (@"C:\$MFT", "NTFS-MFT"),
            (@"C:\$Extend\$UsnJrnl:$J", "NTFS-USNJournal"),
            (@"C:\Windows\System32\winevt\Logs\Application.EVTX", "EventLog-EVTX"),
            (@"C:\Windows\System32\Config\SYSTEM", "SystemHive"),
            (@"C:\Windows\System32\Config\RegBack\SOFTWARE", "SystemHive-RegBack"),
            (@"C:\Windows\AppCompat\Programs\Amcache.hve.LOG1", "Amcache-TransactionLog"),
            (@"C:\Windows\AppCompat\PCA\PcaAppLaunchDic.txt", "AppCompat-PCA"),
            (@"C:\ProgramData\Microsoft\Network\Downloader\qmgr0.dat", "BITS-Qmgr"),
            (@"C:\Windows\System32\Tasks\Vendor\Task", "ScheduledTask"),
            (@"C:\Users\Alice\AppData\Roaming\Microsoft\Windows\Recent\AutomaticDestinations\a.ms", "JumpList-AutomaticDestinations"),
            (@"C:\Users\Alice\AppData\Local\Packages\App\Settings\settings.dat", "PackageSettingsHive"),
            (@"C:\Users\Alice\AppData\Local\Microsoft\Windows\UsrClass.dat.regtrans-ms", "UsrClassDat-TransactionLog"),
            (@"C:\Users\Alice\NTUSER.DAT", "NTUSER-DAT"),
            (@"C:\ProgramData\Microsoft\Windows Defender\Scans\History\Service\DetectionHistory\x", "DefenderDetectionHistory"),
            (@"C:\Windows\INF\setupapi.dev.LOG", "SetupAPI"),
            (@"C:\Users\Alice\AppData\Local\BraveSoftware\Brave-Browser\User Data\Default\History", "BrowserProfile-Brave"),
            (@"C:\Users\Alice\AppData\Local\Google\Chrome\User Data\Default\History", "BrowserProfile-ChromeEdge"),
            (@"C:\Users\Alice\AppData\Roaming\zen\Profiles\default\places.sqlite", "BrowserProfile-Zen"),
            (@"C:\ProgramData\AnyDesk\ad.trace", "RemoteAdmin-AnyDesk"),
            (@"C:\Users\Alice\.ssh\config", "OpenSSH"),
            (@"C:\Users\Alice\AppData\Roaming\rclone\rclone.conf", "RcloneConfig"),
            (@"C:\Users\Alice\AppData\Local\Roblox\logs\log.txt", "RobloxLogs"),
            (@"C:\Users\Alice\AppData\Local\Temp\sample.tmp", "Temp"),
            ("C:/USERS/ALICE/APPDATA/ROAMING/SLACK/logs/app.log", "Slack"),
            (@"C:\Data\ordinary.bin", "")
        ];

        foreach (var test in fileCases)
        {
            Assert(ArtifactCatalog.ClassifyFilePath(test.Path) == test.Expected,
                $"File classifier mismatch for {test.Path}.");
        }

        (string Path, string Expected)[] registryCases =
        [
            (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist", "UserAssist-Execution"),
            (@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\RunMRU", "RunMRU"),
            (@"HKLM\SYSTEM\CurrentControlSet\Services\BAM\State", "BAM-DAM-Execution"),
            (@"HKLM\SYSTEM\CurrentControlSet\Services\Example", "Services-Drivers"),
            (@"HKLM\Software\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", "IFEO"),
            (@"HKCU\Software\Microsoft\Terminal Server Client\Servers", "RDP-TerminalServerClient"),
            (@"HKCU\Software\OpenSSH", "OpenSSH"),
            (@"HKCU\Software\Unrelated", "")
        ];

        foreach (var test in registryCases)
        {
            Assert(ArtifactCatalog.ClassifyRegistryPath(test.Path) == test.Expected,
                $"Registry classifier mismatch for {test.Path}.");
        }

        Assert(ForensicParserEngine.MightParseByPath(@"C:\Users\Alice\HISTORY", null),
            "Parser gating should recognize extensionless browser databases without lowercasing paths.");
        Assert(ForensicParserEngine.MightParseByPath(@"C:\Windows\Logs\APP.BIN", null),
            "Parser gating should recognize mixed-case log paths.");
        Assert(ForensicParserEngine.MightParseByPath(@"C:\Data\REPORT.JSON", null),
            "Parser gating should recognize mixed-case parser extensions.");
        Assert(ForensicParserEngine.IsZipLikePath(@"C:\Data\PACKAGE.ZIP"),
            "ZIP-family detection should remain case-insensitive.");
        Assert(!ForensicParserEngine.MightParseByPath(@"C:\Data\ordinary.bin", null),
            "Parser gating should not broaden unrelated binary files.");
    }

    private static async Task RunSmallFileScan(string root)
    {
        string scanRoot = Path.Combine(root, "ScanRoot");
        Touch(Path.Combine(scanRoot, "sample.txt"));
        string hiddenFile = Path.Combine(scanRoot, "Nested", "hidden-keyword.txt");
        Touch(hiddenFile);
        File.SetAttributes(hiddenFile, File.GetAttributes(hiddenFile) | FileAttributes.Hidden);

        var matcher = KeywordMatcher.Build([PreparedKeyword.Prepare("keyword", 0)]);
        var collector = new MatchCollector();
        var scanner = new FileKeywordScanner(
            matcher,
            new(StringComparer.OrdinalIgnoreCase),
            collector,
            CancellationToken.None,
            pruneMatchedDirectories: false,
            excludedRoots: null,
            enumWorkers: 1,
            readWorkers: 1,
            maxReadsPerVolume: 1,
            parserWorkers: 1,
            readBufferBytes: 64 * 1024);

        await scanner.RunArtifactRootsAsync([new ArtifactRoot("TestRoot", scanRoot, isFile: false)], CancellationToken.None);
        var results = collector.GetFindings();

        Assert(results.Any(f => f.Location.EndsWith("sample.txt", StringComparison.OrdinalIgnoreCase)), "Small temp-root file scan should find sample.txt.");
        Assert(results.Any(f => f.Location.Equals(hiddenFile, StringComparison.OrdinalIgnoreCase)),
            "Raw filesystem enumeration should preserve nested hidden-file coverage.");
        Assert(results.All(f => f.CleanupEligible), "Default explicit scan roots should remain cleanup-eligible.");
    }

    private static async Task RunMultipleRootScan(string root)
    {
        string firstRoot = Path.Combine(root, "MultiRootA");
        string secondRoot = Path.Combine(root, "MultiRootB");
        Touch(Path.Combine(firstRoot, "first-keyword.txt"));
        Touch(Path.Combine(secondRoot, "second-keyword.txt"));

        var service = new ScannerService();
        var options = new ScanOptions
        {
            SkipElevation = true,
            SkipRegistry = true,
            DirectoryEnumWorkers = 2,
            FileReadWorkers = 1,
            ParserWorkers = 1,
            ReadBufferBytes = 64 * 1024
        };
        options.Keywords.Add("first-keyword");
        options.Keywords.Add("second-keyword");
        options.Roots.Add(firstRoot);
        options.Roots.Add(secondRoot);

        var payload = await service.RunAsync(options);
        Assert(payload.Findings.Any(f => f.Location.EndsWith("first-keyword.txt", StringComparison.OrdinalIgnoreCase)),
            "Bounded directory queue should scan the first explicit root.");
        Assert(payload.Findings.Any(f => f.Location.EndsWith("second-keyword.txt", StringComparison.OrdinalIgnoreCase)),
            "Bounded directory queue should scan the second explicit root.");
    }

    private static async Task RunChunkBoundaryScan(string root)
    {
        string scanRoot = Path.Combine(root, "ChunkBoundary");
        Directory.CreateDirectory(scanRoot);
        string file = Path.Combine(scanRoot, "boundary.txt");
        File.WriteAllText(file, new string('x', (64 * 1024) - 2) + "boundary-keyword");

        var service = new ScannerService();
        var options = new ScanOptions
        {
            SkipElevation = true,
            SkipRegistry = true,
            DirectoryEnumWorkers = 1,
            FileReadWorkers = 1,
            ParserWorkers = 1,
            ReadBufferBytes = 64 * 1024
        };
        options.Keywords.Add("boundary-keyword");
        options.Roots.Add(scanRoot);

        var payload = await service.RunAsync(options);
        Assert(payload.Findings.Any(f => f.Kind == "FILE-CONTENT" && f.Location.Equals(file, StringComparison.OrdinalIgnoreCase)),
            "Byte prefilter should not skip matches that continue across read-buffer boundaries.");
    }

    private static async Task RunConcurrentRandomAccessScan(string root)
    {
        const int bufferSize = 64 * 1024;
        const string keyword = "parallel-boundary-keyword";
        string scanRoot = Path.Combine(root, "ConcurrentRandomAccess");
        Directory.CreateDirectory(scanRoot);
        byte[] marker = Encoding.ASCII.GetBytes(keyword);
        var expectedFiles = new List<string>();

        for (int i = 0; i < 12; i++)
        {
            byte[] data = Enumerable.Repeat((byte)'q', 3 * bufferSize).ToArray();
            CopyInto(data, bufferSize - 3, marker);
            string file = Path.Combine(scanRoot, $"parallel-{i:D2}.bin");
            File.WriteAllBytes(file, data);
            expectedFiles.Add(file);
        }

        var options = new ScanOptions
        {
            SkipElevation = true,
            SkipRegistry = true,
            DeepContentScan = true,
            DirectoryEnumWorkers = 2,
            FileReadWorkers = 4,
            MaxReadsPerVolume = 4,
            ParserWorkers = 1,
            ReadBufferBytes = bufferSize
        };
        options.Keywords.Add(keyword);
        options.Roots.Add(scanRoot);

        var payload = await new ScannerService().RunAsync(options);
        foreach (string file in expectedFiles)
        {
            Assert(payload.Findings.Any(f =>
                    f.Kind == "FILE-CONTENT" &&
                    f.Location.Equals(file, StringComparison.OrdinalIgnoreCase)),
                $"Double-buffered RandomAccess scanning lost the boundary match in {file}.");
        }
    }

    private static async Task RunSmartContentGateScan(string root)
    {
        string scanRoot = Path.Combine(root, "SmartContentGate");
        Directory.CreateDirectory(scanRoot);
        string bulkFile = Path.Combine(scanRoot, "movie.mp4");
        File.WriteAllText(bulkFile, "keyword");

        var matcher = KeywordMatcher.Build([PreparedKeyword.Prepare("keyword", 0)]);
        var smartCollector = new MatchCollector();
        var smartScanner = new FileKeywordScanner(
            matcher,
            new(StringComparer.OrdinalIgnoreCase),
            smartCollector,
            CancellationToken.None,
            pruneMatchedDirectories: false,
            excludedRoots: null,
            enumWorkers: 1,
            readWorkers: 1,
            maxReadsPerVolume: 1,
            parserWorkers: 1,
            readBufferBytes: 64 * 1024);

        await smartScanner.RunArtifactRootsAsync([new ArtifactRoot("TestRoot", scanRoot, isFile: false)], CancellationToken.None);
        Assert(!smartCollector.GetFindings().Any(f => f.Kind == "FILE-CONTENT" && f.Location.EndsWith("movie.mp4", StringComparison.OrdinalIgnoreCase)),
            "Smart scan should skip raw content reads for bulk media extensions.");

        var deepCollector = new MatchCollector();
        var deepScanner = new FileKeywordScanner(
            matcher,
            new(StringComparer.OrdinalIgnoreCase),
            deepCollector,
            CancellationToken.None,
            pruneMatchedDirectories: false,
            excludedRoots: null,
            enumWorkers: 1,
            readWorkers: 1,
            maxReadsPerVolume: 1,
            parserWorkers: 1,
            readBufferBytes: 64 * 1024,
            deepContentScan: true);

        await deepScanner.RunArtifactRootsAsync([new ArtifactRoot("TestRoot", scanRoot, isFile: false)], CancellationToken.None);
        Assert(deepCollector.GetFindings().Any(f => f.Kind == "FILE-CONTENT" && f.Location.EndsWith("movie.mp4", StringComparison.OrdinalIgnoreCase)),
            "Deep scan should still read raw content for bulk media extensions.");
    }

    private static async Task RunArchiveEntryScan(string root)
    {
        string scanRoot = Path.Combine(root, "ArchiveEntryScan");
        Directory.CreateDirectory(scanRoot);
        string archivePath = Path.Combine(scanRoot, "sample.zip");

        using (var zip = System.IO.Compression.ZipFile.Open(archivePath, System.IO.Compression.ZipArchiveMode.Create))
        {
            var entry = zip.CreateEntry("nested/readme.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("inside-archive-keyword");
        }

        var service = new ScannerService();
        var options = new ScanOptions
        {
            SkipElevation = true,
            SkipRegistry = true
        };
        options.Keywords.Add("inside-archive-keyword");
        options.Roots.Add(scanRoot);
        options.DirectoryEnumWorkers = 1;
        options.FileReadWorkers = 1;
        options.ParserWorkers = 1;
        options.ReadBufferBytes = 64 * 1024;

        var payload = await service.RunAsync(options);
        Assert(payload.Findings.Any(f => f.Kind == "FILE-ARCHIVE-ENTRY-CONTENT" && f.Location.Equals(archivePath, StringComparison.OrdinalIgnoreCase)),
            "Smart scan should find keyword content inside ZIP-family archive entries.");
        Assert(FindingCleanupService.CountTargets(payload.Findings) == 1,
            "Archive entry findings should resolve to one cleanup target: the containing archive.");
    }

    private static async Task RunDeepArchiveCoverageScan(string root)
    {
        string scanRoot = Path.Combine(root, "DeepArchiveCoverage");
        Directory.CreateDirectory(scanRoot);
        string archivePath = Path.Combine(scanRoot, "many-entries.zip");

        using (var zip = System.IO.Compression.ZipFile.Open(archivePath, System.IO.Compression.ZipArchiveMode.Create))
        {
            for (int i = 0; i < 305; i++)
            {
                var entry = zip.CreateEntry($"empty/{i:D3}.txt");
                using var writer = new StreamWriter(entry.Open());
                if (i == 304)
                    writer.Write("deep-archive-keyword");
            }
        }

        var options = new ScanOptions
        {
            SkipElevation = true,
            SkipRegistry = true,
            DeepContentScan = true,
            DirectoryEnumWorkers = 1,
            FileReadWorkers = 1,
            ParserWorkers = 1,
            ReadBufferBytes = 64 * 1024
        };
        options.Keywords.Add("deep-archive-keyword");
        options.Roots.Add(scanRoot);

        var payload = await new ScannerService().RunAsync(options);
        Assert(payload.Findings.Any(f =>
                f.Kind == "FILE-ARCHIVE-ENTRY-CONTENT" &&
                f.Location.Equals(archivePath, StringComparison.OrdinalIgnoreCase)),
            "Deep scan should inspect archive entries beyond the smart-profile entry limit.");
    }

    private static async Task RunCleanupDeleteScan(string root)
    {
        string scanRoot = Path.Combine(root, "CleanupDelete");
        Directory.CreateDirectory(scanRoot);
        string file = Path.Combine(scanRoot, "surface-delete-keyword.txt");
        File.WriteAllText(file, "plain content");

        var service = new ScannerService();
        var options = new ScanOptions
        {
            SkipElevation = true,
            SkipRegistry = true
        };
        options.Keywords.Add("surface-delete-keyword");
        options.Roots.Add(scanRoot);
        options.DirectoryEnumWorkers = 1;
        options.FileReadWorkers = 1;
        options.ParserWorkers = 1;

        var payload = await service.RunAsync(options);
        Assert(payload.Findings.Any(f => f.Kind == "FILE-NAME" && f.Location.Equals(file, StringComparison.OrdinalIgnoreCase)),
            "Cleanup delete scan should find keyword in a surface filename.");
        Assert(FindingCleanupService.CountTargets(payload.Findings) == 1,
            "Surface filename finding should become one cleanup target.");

        var result = FindingCleanupService.DeleteFindings(payload.Findings);
        Assert(result.DeletedCount == 1, "Surface filename cleanup target should delete successfully.");
        Assert(!File.Exists(file), "Surface filename file should be gone after cleanup.");
    }
}
