namespace Scanner.Core;

internal sealed class RegistryKeywordScanner
{
    private readonly KeywordMatcher _matcher;
    private readonly IMatchSink _matches;
    private readonly CancellationToken _matchToken;
    private readonly bool _fullRegistryScan;

    public RegistryKeywordScanner(KeywordMatcher matcher, IMatchSink matches, CancellationToken matchToken, bool fullRegistryScan = false)
    {
        _matcher = matcher;
        _matches = matches;
        _matchToken = matchToken;
        _fullRegistryScan = fullRegistryScan;
    }

    public async Task RunAsync(int maxDegreeOfParallelism, CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _matchToken);
        var ct = linked.Token;

        await Parallel.ForEachAsync(
            RegistryRoots(),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, maxDegreeOfParallelism),
                CancellationToken = ct
            },
            (root, token) =>
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(root.Hive, root.View);
                    var binarySeen = new bool[_matcher.Keywords.Length];
                    ScanKey(baseKey, root.Display, root.SubKey, token, binarySeen);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { ScannerDiagnostics.Error($"Registry root error: {root.Display} - {ex.Message}"); }

                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);
    }

    private void ScanKey(RegistryKey baseKey, string displayRoot, string sub, CancellationToken ct, bool[] binarySeen)
    {
        RegistryKey? key = null;
        try
        {
            key = string.IsNullOrEmpty(sub) ? baseKey : baseKey.OpenSubKey(sub!, RegistryKeyPermissionCheck.ReadSubTree);
            if (key == null) return;
        }
        catch { return; }

        try
        {
            string fullPath = string.IsNullOrEmpty(sub) ? displayRoot : displayRoot + "\\" + sub;
            string source = ArtifactCatalog.ClassifyRegistryPath(fullPath) is { Length: > 0 } a ? "Artifact:" + a : "Registry";
            bool isUserAssistCountKey = IsUserAssistCountKey(fullPath);

            if (!ct.IsCancellationRequested)
                CheckText(source, "REG-KEY", fullPath, fullPath);

            if (ct.IsCancellationRequested) return;

            foreach (string valName in key.GetValueNames())
            {
                if (ct.IsCancellationRequested) break;
                string vName = string.IsNullOrEmpty(valName) ? "(Default)" : valName;
                string valuePath = fullPath + "\\" + vName;

                CheckText(source, "REG-VALUE-NAME", valuePath, vName);
                if (isUserAssistCountKey)
                {
                    string decodedName = Rot13(vName);
                    if (!string.Equals(decodedName, vName, StringComparison.Ordinal))
                        CheckText(source, "REG-VALUE-DECODED-NAME", valuePath, decodedName);
                }

                object? val = null;
                try { val = key.GetValue(valName); } catch { }
                if (val == null) continue;

                string text = RegistryValueToText(val);
                CheckText(source, "REG-VALUE-DATA", valuePath, text);

                if (val is byte[] bin && bin.Length > 0 && _matcher.HasBytePatterns)
                {
                    Array.Clear(binarySeen, 0, binarySeen.Length);
                    int remaining = _matcher.ByteSearchableKeywordCount;
                    int state = 0;

                    _matcher.SearchBytesUnique(
                        bin.AsMemory(),
                        ref state,
                        binarySeen,
                        ref remaining,
                        hit =>
                        {
                            _matches.HandleMatch(
                                source,
                                "REG-BINARY-DATA",
                                hit.Keyword.Text,
                                valuePath,
                                evidenceFactory: () => EvidenceFormatter.Binary(bin, bin.Length, hit));

                            return !ct.IsCancellationRequested;
                        });
                }
            }

            if (ct.IsCancellationRequested) return;

            foreach (string child in key.GetSubKeyNames())
            {
                if (ct.IsCancellationRequested) break;
                ScanKey(baseKey, displayRoot, string.IsNullOrEmpty(sub) ? child : sub + "\\" + child, ct, binarySeen);
            }
        }
        finally { key?.Dispose(); }
    }

    private bool CheckText(string source, string kind, string location, string text)
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
                evidenceFactory: () => EvidenceFormatter.Text(text, hit.Index, hit.MatchLength));
        }

        return matched;
    }

    private static string RegistryValueToText(object value) => value switch
    {
        string s => s,
        string[] arr => string.Join(";", arr),
        byte[] b => ByteMatcher.BinaryPreview(b, 768),
        int i => i.ToString(),
        long l => l.ToString(),
        _ => value.ToString() ?? ""
    };

    private static bool IsUserAssistCountKey(string fullPath)
    {
        string p = fullPath.Replace('/', '\\');
        return p.Contains("\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\UserAssist\\", StringComparison.OrdinalIgnoreCase) &&
               p.EndsWith("\\Count", StringComparison.OrdinalIgnoreCase);
    }

    private static string Rot13(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var chars = text.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];
            if (c is >= 'a' and <= 'z')
                chars[i] = (char)('a' + ((c - 'a' + 13) % 26));
            else if (c is >= 'A' and <= 'Z')
                chars[i] = (char)('A' + ((c - 'A' + 13) % 26));
        }

        return new string(chars);
    }

    private IEnumerable<(string Display, RegistryHive Hive, RegistryView View, string SubKey)> RegistryRoots()
    {
        bool is64 = Environment.Is64BitOperatingSystem;
        var nativeView = is64 ? RegistryView.Registry64 : RegistryView.Default;

        if (_fullRegistryScan)
        {
            yield return ("HKCU", RegistryHive.CurrentUser, nativeView, "");
            yield return ("HKLM", RegistryHive.LocalMachine, nativeView, "");
            yield return ("HKU", RegistryHive.Users, nativeView, "");
            yield return ("HKCR", RegistryHive.ClassesRoot, nativeView, "");
            if (is64)
            {
                yield return ("HKCU32", RegistryHive.CurrentUser, RegistryView.Registry32, "");
                yield return ("HKLM32", RegistryHive.LocalMachine, RegistryView.Registry32, "");
                yield return ("HKCR32", RegistryHive.ClassesRoot, RegistryView.Registry32, "");
            }

            yield break;
        }

        foreach (string subKey in TargetedCurrentUserSubKeys())
            yield return ("HKCU", RegistryHive.CurrentUser, nativeView, subKey);

        foreach (string subKey in TargetedLocalMachineSubKeys())
            yield return ("HKLM", RegistryHive.LocalMachine, nativeView, subKey);

        if (is64)
        {
            foreach (string subKey in TargetedCurrentUserSubKeys())
                yield return ("HKCU32", RegistryHive.CurrentUser, RegistryView.Registry32, subKey);

            foreach (string subKey in TargetedLocalMachineSubKeys())
                yield return ("HKLM32", RegistryHive.LocalMachine, RegistryView.Registry32, subKey);
        }
    }

    private static IEnumerable<string> TargetedCurrentUserSubKeys()
    {
        yield return @"Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist";
        yield return @"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU";
        yield return @"Software\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs";
        yield return @"Software\Microsoft\Windows\CurrentVersion\Explorer\TypedPaths";
        yield return @"Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSavePidlMRU";
        yield return @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts";
        yield return @"Software\Microsoft\Windows\Shell\BagMRU";
        yield return @"Software\Microsoft\Windows\Shell\Bags";
        yield return @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\BagMRU";
        yield return @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags";
        yield return @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache";
        yield return @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags";
        yield return @"Software\Microsoft\Windows\CurrentVersion\Run";
        yield return @"Software\Microsoft\Windows\CurrentVersion\RunOnce";
        yield return @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
        yield return @"Software\Microsoft\Terminal Server Client";
        yield return @"Software\Microsoft\Command Processor";
        yield return @"Software\Microsoft\PowerShell";
        yield return @"Software\OpenSSH";
    }

    private static IEnumerable<string> TargetedLocalMachineSubKeys()
    {
        yield return @"Software\Microsoft\Windows\CurrentVersion\Run";
        yield return @"Software\Microsoft\Windows\CurrentVersion\RunOnce";
        yield return @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
        yield return @"Software\Microsoft\Windows\CurrentVersion\App Paths";
        yield return @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags";
        yield return @"Software\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";
        yield return @"Software\Microsoft\Windows Defender\Exclusions";
        yield return @"System\CurrentControlSet\Services\bam";
        yield return @"System\CurrentControlSet\Services\dam";
        yield return @"System\CurrentControlSet\Control\Session Manager\AppCompatCache";
        yield return @"System\CurrentControlSet\Control\Session Manager\Environment";
        yield return @"System\CurrentControlSet\Control\ComputerName";
        yield return @"System\CurrentControlSet\Enum\USBSTOR";
        yield return @"System\MountedDevices";
    }
}

// Artifact Catalog (full)
