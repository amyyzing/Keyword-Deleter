namespace Scanner.Core;

internal sealed class FileKeywordScanner
{
    private readonly KeywordMatcher _matcher;
    private readonly ConcurrentDictionary<string, byte> _seenFiles;
    private readonly IMatchSink _matches;
    private readonly CancellationToken _matchToken;
    private readonly bool _pruneMatchedDirectories;
    private readonly string[] _excludedRoots;
    private readonly int _enumWorkers;
    private readonly int _readWorkers;
    private readonly int _maxReadsPerVolume;
    private readonly int _parserWorkers;
    private readonly int _readBufferBytes;
    private readonly bool _deepContentScan;
    private readonly long _maxContentScanBytes;
    private Channel<DirJob> _dirChannel = null!;
    private Channel<FileJob> _fileChannel = null!;
    private Channel<ParseJob> _parseChannel = null!;
    private readonly ConcurrentDictionary<string, byte> _seenDirs = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _readThrottles = new(StringComparer.OrdinalIgnoreCase);
    private int _pendingDirs;

    private const int DefaultEnumWorkers = 4;
    private const int DefaultReadWorkers = 16;
    private const int DefaultMaxReadsPerVolume = 8;
    private const int DefaultReadBufferBytes = 4 * 1024 * 1024;
    private const int FileQueuePerReader = 1024;
    private const int ParseQueuePerWorker = 16;
    private const int ParserPrefixBytes = 8 * 1024 * 1024;
    private const long DefaultMaxContentScanBytes = 256L * 1024 * 1024;

    private static readonly HashSet<string> SmartSkipRawContentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".7z", ".a", ".apk", ".appx", ".appxbundle", ".avi", ".bak", ".bin", ".bundle", ".cab", ".dmg",
        ".dll", ".dmp", ".esd", ".exe", ".flac", ".gif", ".gz", ".heic", ".iso", ".jar", ".jpeg",
        ".jpg", ".lib", ".m4a", ".mkv", ".mov", ".mp3", ".mp4", ".msi", ".msix", ".msixbundle",
        ".nupkg", ".obj", ".obb", ".ost", ".pak", ".pdb", ".pdf", ".png", ".rar", ".rom", ".scr",
        ".sys", ".tar", ".tga", ".ttf", ".ucas", ".utoc", ".vhd", ".vhdx", ".wav", ".webm", ".webp",
        ".wim", ".woff", ".woff2", ".xz", ".zip"
    };

    public FileKeywordScanner(
        KeywordMatcher matcher,
        ConcurrentDictionary<string, byte> seenFiles,
        IMatchSink matches,
        CancellationToken matchToken,
        bool pruneMatchedDirectories,
        IEnumerable<string>? excludedRoots,
        int? enumWorkers = null,
        int? readWorkers = null,
        int? maxReadsPerVolume = null,
        int? parserWorkers = null,
        int? readBufferBytes = null,
        bool deepContentScan = false,
        long? maxContentScanBytes = null)
    {
        _matcher = matcher;
        _seenFiles = seenFiles;
        _matches = matches;
        _matchToken = matchToken;
        _pruneMatchedDirectories = pruneMatchedDirectories;
        _excludedRoots = NormalizeExcludedRoots(excludedRoots);
        _enumWorkers = NormalizeCount(enumWorkers, DefaultEnumWorkers, 1, 64);
        _readWorkers = NormalizeCount(readWorkers, DefaultReadWorkers, 1, 128);
        _maxReadsPerVolume = NormalizeCount(maxReadsPerVolume, DefaultMaxReadsPerVolume, 1, 64);
        _parserWorkers = NormalizeCount(parserWorkers, Math.Max(1, Environment.ProcessorCount / 2), 1, 64);
        _readBufferBytes = NormalizeCount(readBufferBytes, DefaultReadBufferBytes, 64 * 1024, 64 * 1024 * 1024);
        _deepContentScan = deepContentScan;
        _maxContentScanBytes = NormalizeLong(maxContentScanBytes, DefaultMaxContentScanBytes, 1024 * 1024, 16L * 1024 * 1024 * 1024);
    }

    public async Task RunArtifactRootsAsync(IEnumerable<ArtifactRoot> roots, CancellationToken token)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, _matchToken);
        var ct = linked.Token;

        _dirChannel = Channel.CreateUnbounded<DirJob>();
        _fileChannel = Channel.CreateBounded<FileJob>(
            new BoundedChannelOptions(Math.Max(8192, _readWorkers * FileQueuePerReader))
            {
                FullMode = BoundedChannelFullMode.Wait
            });
        _parseChannel = Channel.CreateBounded<ParseJob>(
            new BoundedChannelOptions(Math.Max(16, _parserWorkers * ParseQueuePerWorker))
            {
                FullMode = BoundedChannelFullMode.Wait
            });
        _pendingDirs = 0;

        var readTasks = Enumerable.Range(0, _readWorkers).Select(_ => ReadWorker(ct)).ToArray();
        var enumTasks = Enumerable.Range(0, _enumWorkers).Select(_ => EnumWorker(ct)).ToArray();
        var parseTasks = Enumerable.Range(0, _parserWorkers).Select(_ => ParserWorker(ct)).ToArray();

        foreach (var root in roots)
        {
            if (ct.IsCancellationRequested) break;
            string source = ClassifyOrSource(root.Name, root.Path);
            try
            {
                if (root.IsFile && (File.Exists(root.Path) || IsSpecialFileRoot(root.Path)))
                {
                    if (!IsExcluded(root.Path))
                        await _fileChannel.Writer.WriteAsync(new FileJob(root.Path, root.Path, source, root.CleanupEligible), ct).ConfigureAwait(false);
                }
                else if (Directory.Exists(root.Path))
                {
                    if (!IsExcluded(root.Path))
                        EnqueueDir(new DirJob(root.Path, root.Path, source, root.CleanupEligible));
                }
                else if (File.Exists(root.Path) || IsSpecialFileRoot(root.Path))
                {
                    if (!IsExcluded(root.Path))
                        await _fileChannel.Writer.WriteAsync(new FileJob(root.Path, root.Path, source, root.CleanupEligible), ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { ScannerDiagnostics.Error($"Error enqueueing root {root.Path}: {ex.Message}"); }
        }
        if (Volatile.Read(ref _pendingDirs) == 0) _dirChannel.Writer.TryComplete();

        try { await Task.WhenAll(enumTasks).ConfigureAwait(false); } catch (OperationCanceledException) { }
        _fileChannel.Writer.TryComplete();
        try { await Task.WhenAll(readTasks).ConfigureAwait(false); } catch (OperationCanceledException) { }
        _parseChannel.Writer.TryComplete();
        try { await Task.WhenAll(parseTasks).ConfigureAwait(false); } catch (OperationCanceledException) { }
    }

    private void EnqueueDir(DirJob job) { Interlocked.Increment(ref _pendingDirs); _dirChannel.Writer.TryWrite(job); }

    private async Task EnumWorker(CancellationToken ct)
    {
        try
        {
            await foreach (var job in _dirChannel.Reader.ReadAllAsync(ct))
            {
                if (ct.IsCancellationRequested) break;
                try { await ProcessDirectoryAsync(job, ct).ConfigureAwait(false); }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
                catch (Exception ex) { ScannerDiagnostics.Error($"Enum error in {job.Path}: {ex.Message}"); }
                finally { if (Interlocked.Decrement(ref _pendingDirs) == 0) _dirChannel.Writer.TryComplete(); }
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task ProcessDirectoryAsync(DirJob job, CancellationToken ct)
    {
        if (IsExcluded(job.Path)) return;
        if (!_seenDirs.TryAdd(job.Path, 0)) return;
        try { if ((File.GetAttributes(job.Path) & FileAttributes.ReparsePoint) != 0) return; } catch { return; }

        bool directoryMatched =
            CheckText(job.Source, "DIRECTORY-NAME", job.Path, PathUtil.FileName(job.Path), job.CleanupEligible) |
            CheckText(job.Source, "DIRECTORY-PATH", job.Path, job.Path, job.CleanupEligible);

        if (_pruneMatchedDirectories && directoryMatched)
            return;

        var opts = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = false,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        try
        {
            foreach (var fsi in new DirectoryInfo(job.Path).EnumerateFileSystemInfos("*", opts))
            {
                if (ct.IsCancellationRequested) return;
                string p = fsi.FullName;
                if (IsExcluded(p)) continue;
                string source = ClassifyOrSource(job.Source, p);
                if ((fsi.Attributes & FileAttributes.Directory) != 0)
                    EnqueueDir(new DirJob(p, job.Root, source, job.CleanupEligible));
                else
                    await _fileChannel.Writer.WriteAsync(new FileJob(p, job.Root, source, job.CleanupEligible), ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ScannerDiagnostics.Error($"Enumeration error in {job.Path}: {ex.Message}");
        }
    }

    private async Task ReadWorker(CancellationToken ct)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(_readBufferBytes);
        bool[] contentHits = new bool[_matcher.Keywords.Length];

        try
        {
            await foreach (var job in _fileChannel.Reader.ReadAllAsync(ct))
            {
                if (ct.IsCancellationRequested) break;

                Array.Clear(contentHits, 0, contentHits.Length);

                try { await ProcessFileAsync(job, ct, buffer, contentHits).ConfigureAwait(false); }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                catch (Exception ex) { ScannerDiagnostics.Error($"Read error on {job.Path}: {ex.Message}"); }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task ParserWorker(CancellationToken ct)
    {
        try
        {
            await foreach (var job in _parseChannel.Reader.ReadAllAsync())
            {
                try
                {
                    if (!ct.IsCancellationRequested)
                    {
                        var parsed = ForensicParserEngine.TryParse(job.Path, job.Info, job.PrefixBuffer, job.PrefixLength, ct);
                        if (parsed != null)
                            EmitParsedArtifact(job.Source, job.Path, parsed, job.CleanupEligible, ct);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    ScannerDiagnostics.Error($"Parser error on {job.Path}: {ex.Message}");
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(job.PrefixBuffer);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task ProcessFileAsync(FileJob job, CancellationToken ct, byte[] buffer, bool[] contentHitsForFile)
    {
        if (IsExcluded(job.Path)) return;
        if (!_seenFiles.TryAdd(job.Path, 0)) return;
        if (ct.IsCancellationRequested) return;

        string path = job.Path, source = job.Source, name = PathUtil.FileName(path);

        CheckText(source, "FILE-NAME", path, name, job.CleanupEligible);
        CheckText(source, "FILE-PATH", path, path, job.CleanupEligible);

        FileInfo? info = null;
        long fileSize = 0;
        try
        {
            info = new FileInfo(path);
            fileSize = Math.Max(0, info.Length);
        }
        catch { }

        bool parserCandidateByPath = ForensicParserEngine.MightParseByPath(path, info);
        bool rawContentAllowed = ShouldScanRawContent(path, info, fileSize);
        if (!rawContentAllowed && !parserCandidateByPath)
            return;

        byte[]? prefixBuffer = null;
        int prefixLength = 0;

        try
        {
            if (!ct.IsCancellationRequested)
            {
                var throttle = _readThrottles.GetOrAdd(Path.GetPathRoot(path) ?? "", _ => new SemaphoreSlim(_maxReadsPerVolume));
                await throttle.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    int bufferSize = _readBufferBytes;
                    using var fs = PrivilegedFile.OpenRead(path, bufferSize, PrivilegeHelper.IsBackupPrivilegeEnabled);

                        int remainingContentKeywords = _matcher.ByteSearchableKeywordCount;
                        int byteMatcherState = 0;
                        long fileOffset = 0;
                        long rawBytesRemaining = rawContentAllowed
                            ? (_deepContentScan ? long.MaxValue : Math.Min(_maxContentScanBytes, fileSize > 0 ? fileSize : _maxContentScanBytes))
                            : 0;

                        bool parserCandidateKnown = parserCandidateByPath;
                        bool parserCandidate = parserCandidateByPath;
                        long prefixTarget = parserCandidate
                            ? Math.Min(ParserPrefixBytes, fileSize > 0 ? fileSize : ParserPrefixBytes)
                            : 0;
                        bool needRawContentScan = rawContentAllowed && _matcher.HasBytePatterns && remainingContentKeywords > 0 && rawBytesRemaining > 0;
                        bool needParserProbe = !parserCandidateKnown && rawContentAllowed;
                        bool needParserPrefix = parserCandidate && prefixLength < prefixTarget;

                        while (!ct.IsCancellationRequested && (needRawContentScan || needParserProbe || needParserPrefix))
                        {
                            int read = await fs.ReadAsync(buffer.AsMemory(0, bufferSize), ct).ConfigureAwait(false);
                            if (read == 0) break;

                            if (!parserCandidateKnown)
                            {
                                parserCandidate = ForensicParserEngine.MightParse(path, info, buffer.AsSpan(0, read));
                                parserCandidateKnown = true;
                                prefixTarget = parserCandidate
                                    ? Math.Min(ParserPrefixBytes, fileSize > 0 ? fileSize : ParserPrefixBytes)
                                    : 0;
                            }

                            if (parserCandidate && prefixLength < prefixTarget)
                            {
                                int targetLength = (int)Math.Min(ParserPrefixBytes, prefixTarget);
                                prefixBuffer ??= ArrayPool<byte>.Shared.Rent(ParserPrefixBytes);
                                int copy = Math.Min(read, targetLength - prefixLength);
                                Buffer.BlockCopy(buffer, 0, prefixBuffer, prefixLength, copy);
                                prefixLength += copy;
                            }

                            if (needRawContentScan)
                            {
                                int rawReadLength = (int)Math.Min(read, Math.Max(0, rawBytesRemaining));
                                long chunkOffset = fileOffset;
                                if (rawReadLength > 0)
                                {
                                    _matcher.SearchBytesUnique(
                                        buffer.AsMemory(0, rawReadLength),
                                        ref byteMatcherState,
                                        contentHitsForFile,
                                        ref remainingContentKeywords,
                                        hit =>
                                        {
                                            _matches.HandleMatch(
                                                source,
                                                "FILE-CONTENT",
                                                hit.Keyword.Text,
                                                path,
                                                evidenceFactory: () => EvidenceFormatter.Binary(buffer, rawReadLength, hit, chunkOffset),
                                                cleanupEligible: job.CleanupEligible);
                                            return !ct.IsCancellationRequested;
                                        });

                                    rawBytesRemaining -= rawReadLength;
                                }
                            }

                            fileOffset += read;

                            needRawContentScan = rawContentAllowed && _matcher.HasBytePatterns && remainingContentKeywords > 0 && rawBytesRemaining > 0;
                            needParserProbe = !parserCandidateKnown;
                            needParserPrefix = parserCandidate && prefixLength < prefixTarget;
                        }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { ScannerDiagnostics.Error($"Content read error on {path}: {ex.Message}"); }
                finally { throttle.Release(); }
            }

            if (!ct.IsCancellationRequested && prefixLength > 0 && prefixBuffer != null && File.Exists(path))
            {
                info ??= new FileInfo(path);
                await _parseChannel.Writer.WriteAsync(new ParseJob(path, source, info, prefixBuffer, prefixLength, job.CleanupEligible), ct).ConfigureAwait(false);
                prefixBuffer = null;
                prefixLength = 0;
            }
        }
        finally
        {
            if (prefixBuffer != null)
                ArrayPool<byte>.Shared.Return(prefixBuffer);
        }
    }

    private void EmitParsedArtifact(string source, string path, ParsedArtifact parsed, bool cleanupEligible, CancellationToken ct)
    {
        string parsedSource = source + "/Parser:" + parsed.Kind;
        CheckText(parsedSource, "ARTIFACT-TYPE", path, parsed.Kind + " " + parsed.Summary, cleanupEligible);
        foreach (var metadata in parsed.Metadata)
        {
            if (ct.IsCancellationRequested) return;
            CheckText(parsedSource, "ARTIFACT-METADATA", path, metadata.Key + " = " + metadata.Value, cleanupEligible);
        }

        foreach (var text in parsed.SearchableText)
        {
            if (ct.IsCancellationRequested) return;
            CheckText(parsedSource, "ARTIFACT-DECODED-TEXT", path, text, cleanupEligible);
        }
    }

    private bool CheckText(string source, string kind, string location, string text, bool cleanupEligible)
    {
        if (string.IsNullOrEmpty(text) || _matchToken.IsCancellationRequested)
            return false;

        bool matched = false;
        int firstKeyword = -1;
        HashSet<int>? moreKeywords = null;

        foreach (var hit in _matcher.SearchDecodedText(text))
        {
            int idx = hit.Keyword.Index;

            if (idx == firstKeyword)
                continue;

            if (firstKeyword < 0)
            {
                firstKeyword = idx;
            }
            else
            {
                moreKeywords ??= new HashSet<int> { firstKeyword };
                if (!moreKeywords.Add(idx))
                    continue;
            }

            matched = true;
            _matches.HandleMatch(
                source,
                kind,
                hit.Keyword.Text,
                location,
                evidenceFactory: () => EvidenceFormatter.Text(text, hit.Index, hit.MatchLength),
                cleanupEligible: cleanupEligible);
        }

        return matched;
    }

    private string ClassifyOrSource(string source, string path) =>
        ArtifactCatalog.ClassifyFilePath(path) is { Length: > 0 } a ? "Artifact:" + a : source;

    private bool ShouldScanRawContent(string path, FileInfo? info, long fileSize)
    {
        if (!_matcher.HasBytePatterns)
            return false;

        if (_deepContentScan)
            return true;

        if (fileSize <= 0)
            return false;

        string extension = "";
        try { extension = (info?.Extension ?? Path.GetExtension(path)).ToLowerInvariant(); } catch { }

        if (SmartSkipRawContentExtensions.Contains(extension))
            return false;

        return fileSize <= _maxContentScanBytes;
    }

    private bool IsExcluded(string path)
    {
        if (_excludedRoots.Length == 0)
            return false;

        string? normalized = NormalizeFileSystemPath(path);
        return normalized != null && _excludedRoots.Any(root => IsSameOrChildPath(normalized, root));
    }

    private static int NormalizeCount(int? value, int defaultValue, int min, int max)
    {
        return Math.Clamp(value ?? defaultValue, min, max);
    }

    private static long NormalizeLong(long? value, long defaultValue, long min, long max)
    {
        return Math.Clamp(value ?? defaultValue, min, max);
    }

    private static string[] NormalizeExcludedRoots(IEnumerable<string>? roots)
    {
        if (roots == null)
            return Array.Empty<string>();

        return roots
            .Select(NormalizeFileSystemPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path!.Length)
            .Select(path => path!)
            .ToArray();
    }

    private static string? NormalizeFileSystemPath(string path)
    {
        try
        {
            string fullPath = Path.GetFullPath(path);
            string root = Path.GetPathRoot(fullPath) ?? "";
            if (string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase))
                return fullPath.TrimEnd('/');

            return fullPath.TrimEnd('\\', '/');
        }
        catch
        {
            return null;
        }
    }

    private static bool IsSameOrChildPath(string path, string directory)
    {
        if (string.Equals(path, directory, StringComparison.OrdinalIgnoreCase))
            return true;

        string prefix = directory.EndsWith('\\') || directory.EndsWith('/')
            ? directory
            : directory + Path.DirectorySeparatorChar;

        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSpecialFileRoot(string path)
    {
        string p = path.Replace('/', '\\');
        return p.EndsWith("\\$MFT", StringComparison.OrdinalIgnoreCase) ||
               p.EndsWith("\\$LogFile", StringComparison.OrdinalIgnoreCase) ||
               p.Contains("\\$Extend\\$UsnJrnl:", StringComparison.OrdinalIgnoreCase);
    }
}

// Job types
