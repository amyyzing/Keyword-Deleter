namespace Scanner.Core;

internal static class ForensicParserEngine
{
    private const int PrefixBytes = 8 * 1024 * 1024;
    private const int MaxStrings = 320;
    private const int MaxZipEntries = 300;
    private const int MaxTextEntryBytes = 1024 * 1024;

    public static ParsedArtifact? TryParse(string path, CancellationToken token)
    {
        if (token.IsCancellationRequested || string.IsNullOrWhiteSpace(path)) return null;
        FileInfo info;
        try { info = new FileInfo(path); } catch { return null; }
        return TryParse(path, info, null, 0, token);
    }

    public static bool MightParse(string path, FileInfo? info, ReadOnlySpan<byte> prefix)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (MightParseByPath(path, info))
            return true;

        return LooksLikePe(prefix) ||
               LooksLikeLnk(prefix) ||
               StartsWithAscii(prefix, "SCCA", offset: 4) ||
               StartsWithAscii(prefix, "ElfFile") ||
               StartsWithAscii(prefix, "regf") ||
               StartsWithAscii(prefix, "SQLite format 3") ||
               LooksLikeEse(prefix) ||
               LooksLikeOle(prefix) ||
               LooksLikeTextPrefix(prefix, "<") ||
               LooksLikeTextPrefix(prefix, "{") ||
               LooksLikeTextPrefix(prefix, "[") ||
               LooksMostlyText(prefix);
    }

    public static bool MightParseByPath(string path, FileInfo? info)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        string lowerPath = path.Replace('/', '\\').ToLowerInvariant();
        string ext = "";
        try { ext = (info?.Extension ?? Path.GetExtension(path)).ToLowerInvariant(); } catch { }

        bool taskHint = lowerPath.Contains("\\windows\\system32\\tasks\\") ||
                        lowerPath.Contains("\\system32\\tasks\\");
        bool logHint = lowerPath.Contains("\\logs\\", StringComparison.Ordinal) ||
                       lowerPath.Contains("\\logfiles\\", StringComparison.Ordinal) ||
                       lowerPath.Contains("consolehost_history.txt", StringComparison.Ordinal) ||
                       lowerPath.Contains("\\windows defender\\", StringComparison.Ordinal) ||
                       lowerPath.Contains("\\microsoft\\windows\\wer\\", StringComparison.Ordinal) ||
                       lowerPath.Contains("\\crashdumps\\", StringComparison.Ordinal);
        bool registryHiveNameHint = lowerPath.EndsWith("ntuser.dat", StringComparison.Ordinal) ||
                                    lowerPath.EndsWith("usrclass.dat", StringComparison.Ordinal) ||
                                    lowerPath.EndsWith("amcache.hve", StringComparison.Ordinal) ||
                                    lowerPath.EndsWith("\\system", StringComparison.Ordinal) ||
                                    lowerPath.EndsWith("\\software", StringComparison.Ordinal) ||
                                    lowerPath.EndsWith("\\sam", StringComparison.Ordinal) ||
                                    lowerPath.EndsWith("\\security", StringComparison.Ordinal) ||
                                    lowerPath.EndsWith("\\default", StringComparison.Ordinal) ||
                                    lowerPath.EndsWith("\\components", StringComparison.Ordinal) ||
                                    lowerPath.EndsWith(".hve", StringComparison.Ordinal);
        bool sqliteNameHint = lowerPath.EndsWith(".sqlite", StringComparison.Ordinal) ||
                              lowerPath.EndsWith(".sqlite3", StringComparison.Ordinal) ||
                              lowerPath.EndsWith(".db", StringComparison.Ordinal) ||
                              lowerPath.EndsWith("cookies", StringComparison.Ordinal) ||
                              lowerPath.EndsWith("history", StringComparison.Ordinal) ||
                              lowerPath.EndsWith("web data", StringComparison.Ordinal) ||
                              lowerPath.EndsWith("login data", StringComparison.Ordinal) ||
                              lowerPath.EndsWith("favicons", StringComparison.Ordinal) ||
                              lowerPath.EndsWith("top sites", StringComparison.Ordinal) ||
                              lowerPath.EndsWith("shortcuts", StringComparison.Ordinal) ||
                              lowerPath.EndsWith("places.sqlite", StringComparison.Ordinal) ||
                              lowerPath.EndsWith("activitiescache.db", StringComparison.Ordinal);
        bool eseNameHint = lowerPath.EndsWith("srudb.dat", StringComparison.Ordinal) ||
                           lowerPath.EndsWith("webcachev01.dat", StringComparison.Ordinal) ||
                           lowerPath.EndsWith("qmgr0.dat", StringComparison.Ordinal) ||
                           lowerPath.EndsWith("qmgr1.dat", StringComparison.Ordinal) ||
                           lowerPath.Contains("\\windows.edb", StringComparison.Ordinal);
        bool jumpListHint = lowerPath.EndsWith(".automaticdestinations-ms", StringComparison.Ordinal) ||
                            lowerPath.EndsWith(".customdestinations-ms", StringComparison.Ordinal);
        bool knownText = ext is ".txt" or ".log" or ".csv" or ".ini" or ".cfg" or ".conf" or ".url"
                         or ".ps1" or ".psm1" or ".psd1" or ".bat" or ".cmd" or ".vbs" or ".js"
                         or ".yml" or ".yaml" or ".html" or ".htm" or ".rdp" or ".ovpn";

        return IsZipLike(ext) ||
               ext is ".exe" or ".dll" or ".sys" or ".scr" or ".com" or ".cpl" or ".msi" ||
               ext == ".lnk" ||
               ext == ".pf" ||
               ext == ".evtx" ||
               registryHiveNameHint ||
               sqliteNameHint ||
               eseNameHint ||
               jumpListHint ||
               ext is ".automaticdestinations-ms" or ".customdestinations-ms" ||
               ext is ".ole" or ".doc" or ".xls" or ".ppt" ||
               taskHint ||
               ext == ".xml" ||
               ext is ".json" or ".jsonl" ||
               knownText ||
               logHint;
    }

    public static ParsedArtifact? TryParse(string path, FileInfo info, byte[]? prefixBuffer, int prefixLength, CancellationToken token)
    {
        if (token.IsCancellationRequested || string.IsNullOrWhiteSpace(path)) return null;
        try { info.Refresh(); } catch { }
        if (!info.Exists) return null;

        string lowerPath = path.Replace('/', '\\').ToLowerInvariant();
        string ext = info.Extension.ToLowerInvariant();

        byte[] prefix;
        if (prefixBuffer != null && prefixLength > 0)
        {
            int len = Math.Min(prefixLength, PrefixBytes);
            prefix = new byte[len];
            Buffer.BlockCopy(prefixBuffer, 0, prefix, 0, len);
        }
        else
        {
            prefix = ReadPrefix(path, PrefixBytes);
        }

        long length = 0;
        try { length = info.Length; } catch { }
        if (prefix.Length == 0 && length > 0) return null;

        ParsedArtifact? parsed = null;

        if (IsZipLike(ext)) parsed = TryParseZip(path, info, token);
        parsed ??= TryParseScheduledTaskOrXml(path, info, prefix);
        parsed ??= TryParseJson(path, info, prefix);
        parsed ??= TryParsePe(path, info, prefix);
        parsed ??= TryParseLnk(path, info, prefix);
        parsed ??= TryParsePrefetch(path, info, prefix);
        parsed ??= TryParseEvtx(path, info, prefix);
        parsed ??= TryParseRegistryHive(path, info, prefix);
        parsed ??= TryParseSqlite(path, info, prefix, lowerPath);
        parsed ??= TryParseEse(path, info, prefix, lowerPath);
        parsed ??= TryParseOleCompound(path, info, prefix, lowerPath);
        parsed ??= TryParseTextLike(path, info, prefix, ext, lowerPath);

        if (parsed == null) return null;

        AddCommonMetadata(parsed, info, lowerPath);
        AddKnownArtifactHints(parsed, lowerPath);
        return parsed;
    }

    private static void AddCommonMetadata(ParsedArtifact parsed, FileInfo info, string _)
    {
        parsed.Add("FileName", info.Name);
        parsed.Add("Extension", info.Extension);
        parsed.Add("Length", info.Length);
        parsed.Add("CreatedUtc", SafeTime(() => info.CreationTimeUtc));
        parsed.Add("ModifiedUtc", SafeTime(() => info.LastWriteTimeUtc));
        parsed.Add("AccessedUtc", SafeTime(() => info.LastAccessTimeUtc));
        parsed.Add("Attributes", Safe(() => info.Attributes.ToString()));
        parsed.Add("Directory", info.DirectoryName ?? "");
    }

    private static string SafeTime(Func<DateTime> getter) { try { return getter().ToString("O"); } catch { return ""; } }
    private static string Safe(Func<string> getter) { try { return getter(); } catch { return ""; } }

    private static void AddKnownArtifactHints(ParsedArtifact parsed, string lowerPath)
    {
        string artifact = ArtifactCatalog.ClassifyFilePath(lowerPath);
        if (!string.IsNullOrWhiteSpace(artifact)) parsed.Add("ArtifactHint", artifact);
        if (lowerPath.Contains("\\system32\\winevt\\logs\\")) parsed.Add("ArtifactFamily", "Windows Event Log");
        if (lowerPath.Contains("\\windows\\prefetch\\")) parsed.Add("ArtifactFamily", "Prefetch execution trace");
        if (lowerPath.Contains("\\appcompat\\programs\\")) parsed.Add("ArtifactFamily", "AppCompat/Amcache execution inventory");
        if (lowerPath.Contains("\\recent\\automaticdestinations\\") || lowerPath.Contains("\\recent\\customdestinations\\"))
            parsed.Add("ArtifactFamily", "Jump List recent-file artifact");
        if (lowerPath.Contains("\\system32\\tasks\\")) parsed.Add("ArtifactFamily", "Scheduled Task");
        if (lowerPath.Contains("\\google\\chrome\\user data\\") || lowerPath.Contains("\\microsoft\\edge\\user data\\"))
            parsed.Add("ArtifactFamily", "Chromium browser profile");
        if (lowerPath.Contains("\\mozilla\\firefox\\profiles\\")) parsed.Add("ArtifactFamily", "Firefox browser profile");
    }

    private static byte[] ReadPrefix(string path, int maxBytes)
    {
        try
        {
            using var fs = PrivilegedFile.OpenRead(path, 1024 * 1024, PrivilegeHelper.IsBackupPrivilegeEnabled);
            int toRead = (int)Math.Min(maxBytes, Math.Max(0, fs.Length));
            var buffer = new byte[toRead];
            int total = 0;
            while (total < buffer.Length)
            {
                int read = fs.Read(buffer, total, buffer.Length - total);
                if (read <= 0) break;
                total += read;
            }
            if (total == buffer.Length) return buffer;
            Array.Resize(ref buffer, total);
            return buffer;
        }
        catch { return Array.Empty<byte>(); }
    }

    public static bool IsZipLikePath(string path)
    {
        try { return IsZipLike(Path.GetExtension(path).ToLowerInvariant()); }
        catch { return false; }
    }

    private static bool IsZipLike(string ext) => ext is ".zip" or ".docx" or ".xlsx" or ".pptx" or ".jar" or ".nupkg" or ".vsix" or ".odt" or ".ods" or ".odp";

    private static bool LooksLikePe(ReadOnlySpan<byte> b)
    {
        if (b.Length < 0x40 || b[0] != (byte)'M' || b[1] != (byte)'Z')
            return false;

        int pe = ReadInt32(b, 0x3c);
        return pe > 0 &&
               pe + 3 < b.Length &&
               b[pe] == (byte)'P' &&
               b[pe + 1] == (byte)'E' &&
               b[pe + 2] == 0 &&
               b[pe + 3] == 0;
    }

    private static bool LooksLikeLnk(ReadOnlySpan<byte> b)
    {
        if (b.Length < 0x4c) return false;
        if (ReadUInt32(b, 0) != 0x4c) return false;
        ReadOnlySpan<byte> clsid = stackalloc byte[] { 0x01, 0x14, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 };
        return b.Slice(4, clsid.Length).SequenceEqual(clsid);
    }

    private static bool LooksLikeOle(ReadOnlySpan<byte> b) =>
        b.Length >= 8 &&
        b[0] == 0xD0 && b[1] == 0xCF && b[2] == 0x11 && b[3] == 0xE0 &&
        b[4] == 0xA1 && b[5] == 0xB1 && b[6] == 0x1A && b[7] == 0xE1;

    private static bool LooksLikeEse(ReadOnlySpan<byte> b)
    {
        if (b.Length <= 32)
            return false;

        int length = Math.Min(15, b.Length - 4);
        return Encoding.ASCII.GetString(b.Slice(4, length)).Contains("Standard Jet DB", StringComparison.OrdinalIgnoreCase);
    }

    private static ParsedArtifact? TryParseZip(string path, FileInfo info, CancellationToken token)
    {
        try
        {
            using var fs = PrivilegedFile.OpenRead(path, 1024 * 1024, PrivilegeHelper.IsBackupPrivilegeEnabled);
            using var zip = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: false);
            string kind = info.Extension.ToLowerInvariant() switch
            {
                ".docx" => "Office Open XML Word Document",
                ".xlsx" => "Office Open XML Excel Workbook",
                ".pptx" => "Office Open XML PowerPoint Deck",
                ".jar" => "Java Archive",
                ".nupkg" => "NuGet Package",
                ".vsix" => "VSIX Extension Package",
                _ => "ZIP Container"
            };
            var p = new ParsedArtifact(kind, "ZIP-style container with embedded entries");
            p.Add("EntryCount", zip.Entries.Count);

            int listed = 0;
            foreach (var entry in zip.Entries.Take(MaxZipEntries))
            {
                if (token.IsCancellationRequested) break;
                listed++;
                p.Add("ZipEntry" + listed.ToString("000"), entry.FullName + " size=" + entry.Length);

                string e = entry.FullName.ToLowerInvariant();
                if (entry.Length <= 0 || entry.Length > MaxTextEntryBytes) continue;
                if (!(LooksTextEntry(e) || e.EndsWith(".xml", StringComparison.Ordinal) || e.EndsWith(".rels", StringComparison.Ordinal))) continue;

                try
                {
                    using var stream = entry.Open();
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    string text = DecodeBestEffort(ms.ToArray());
                    if (e.EndsWith(".xml", StringComparison.Ordinal) || e.EndsWith(".rels", StringComparison.Ordinal))
                        text = ExtractXmlText(text);
                    p.AddText(entry.FullName + " " + text);
                }
                catch { }
            }
            return p;
        }
        catch { return null; }
    }

    private static bool LooksTextEntry(string entryName) =>
        entryName.EndsWith(".txt", StringComparison.Ordinal) ||
        entryName.EndsWith(".csv", StringComparison.Ordinal) ||
        entryName.EndsWith(".json", StringComparison.Ordinal) ||
        entryName.EndsWith(".xml", StringComparison.Ordinal) ||
        entryName.Contains("docprops/", StringComparison.Ordinal) ||
        entryName.Contains("sharedstrings", StringComparison.Ordinal) ||
        entryName.Contains("document.xml", StringComparison.Ordinal) ||
        entryName.Contains("slide", StringComparison.Ordinal);

    private static ParsedArtifact? TryParsePe(string path, FileInfo info, byte[] b)
    {
        if (b.Length < 0x40 || b[0] != (byte)'M' || b[1] != (byte)'Z') return null;
        int pe = ReadInt32(b, 0x3c);
        if (pe <= 0 || pe + 24 >= b.Length) return null;
        if (!(b[pe] == (byte)'P' && b[pe + 1] == (byte)'E' && b[pe + 2] == 0 && b[pe + 3] == 0)) return null;

        var p = new ParsedArtifact("Windows PE Executable", "Portable Executable image: EXE/DLL/SYS-style binary");
        ushort machine = ReadUInt16(b, pe + 4);
        ushort sections = ReadUInt16(b, pe + 6);
        uint timestamp = ReadUInt32(b, pe + 8);
        ushort optionalSize = ReadUInt16(b, pe + 20);
        p.Add("Machine", "0x" + machine.ToString("X4") + " " + MachineName(machine));
        p.Add("SectionCount", sections);
        if (timestamp > 0) { DateTime dt = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime; p.Add("LinkerTimestampUtc", dt.ToString("O")); }

        try
        {
            FileVersionInfo vi = FileVersionInfo.GetVersionInfo(path);
            p.Add("CompanyName", vi.CompanyName);
            p.Add("FileDescription", vi.FileDescription);
            p.Add("FileVersion", vi.FileVersion);
            p.Add("ProductName", vi.ProductName);
            p.Add("ProductVersion", vi.ProductVersion);
            p.Add("OriginalFilename", vi.OriginalFilename);
            p.Add("InternalName", vi.InternalName);
        }
        catch { }

        int sectionOffset = pe + 24 + optionalSize;
        for (int i = 0; i < sections && sectionOffset + 40 <= b.Length && i < 32; i++, sectionOffset += 40)
        {
            string name = ReadAsciiFixed(b, sectionOffset, 8).TrimEnd('\0', ' ');
            uint virtualSize = ReadUInt32(b, sectionOffset + 8);
            uint rawSize = ReadUInt32(b, sectionOffset + 16);
            p.Add("Section" + i, name + " virtual=" + virtualSize + " raw=" + rawSize);
        }
        AddExtractedStrings(p, b, "PEString");
        return p;
    }

    private static string MachineName(ushort machine) => machine switch
    {
        0x014c => "x86",
        0x8664 => "x64",
        0x01c0 => "ARM",
        0xaa64 => "ARM64",
        _ => "unknown"
    };

    private static ParsedArtifact? TryParseLnk(string path, FileInfo info, byte[] b)
    {
        if (!info.Extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase) && !LooksLikeLnk(b)) return null;
        if (!LooksLikeLnk(b)) return null;

        var p = new ParsedArtifact("Windows Shell Link", "LNK shortcut with shell-link header and embedded path strings");
        uint flags = ReadUInt32(b, 0x14);
        uint attrs = ReadUInt32(b, 0x18);
        p.Add("LinkFlags", "0x" + flags.ToString("X8"));
        p.Add("FileAttributes", "0x" + attrs.ToString("X8"));
        p.Add("TargetCreatedUtc", ReadFileTimeString(b, 0x1c));
        p.Add("TargetAccessedUtc", ReadFileTimeString(b, 0x24));
        p.Add("TargetModifiedUtc", ReadFileTimeString(b, 0x2c));
        p.Add("TargetFileSize", ReadUInt32(b, 0x34));
        p.Add("ShowCommand", ReadInt32(b, 0x3c));
        AddExtractedStrings(p, b, "LnkString");
        return p;
    }

    private static bool LooksLikeLnk(byte[] b)
    {
        if (b.Length < 0x4c) return false;
        if (ReadUInt32(b, 0) != 0x4c) return false;
        byte[] clsid = { 0x01, 0x14, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 };
        for (int i = 0; i < clsid.Length; i++) if (b[4 + i] != clsid[i]) return false;
        return true;
    }

    private static ParsedArtifact? TryParsePrefetch(string path, FileInfo info, byte[] b)
    {
        bool ext = info.Extension.Equals(".pf", StringComparison.OrdinalIgnoreCase);
        bool magic = b.Length > 8 && b[4] == (byte)'S' && b[5] == (byte)'C' && b[6] == (byte)'C' && b[7] == (byte)'A';
        if (!ext && !magic) return null;
        if (!magic) return null;

        var p = new ParsedArtifact("Windows Prefetch", "Prefetch execution trace with executable name and run metadata");
        p.Add("PrefetchVersion", ReadInt32(b, 0));
        p.Add("Signature", "SCCA");
        string exeName = ReadUtf16Fixed(b, 0x10, Math.Min(120, b.Length - 0x10));
        p.Add("ExecutableName", exeName);
        AddExtractedStrings(p, b, "PrefetchString");
        return p;
    }

    private static ParsedArtifact? TryParseEvtx(string path, FileInfo info, byte[] b)
    {
        bool ext = info.Extension.Equals(".evtx", StringComparison.OrdinalIgnoreCase);
        bool magic = StartsWithAscii(b, "ElfFile");
        if (!ext && !magic) return null;
        if (!magic) return null;

        var p = new ParsedArtifact("Windows Event Log EVTX", "EVTX log container; strings are extracted best-effort from binary XML chunks");
        p.Add("Signature", "ElfFile");
        if (b.Length >= 0x78) { p.Add("HeaderSize", ReadUInt32(b, 0x78)); p.Add("ChunkCountApprox", Math.Max(0, (info.Length - 4096) / 65536)); }
        AddExtractedStrings(p, b, "EvtxString");
        return p;
    }

    private static ParsedArtifact? TryParseRegistryHive(string path, FileInfo info, byte[] b)
    {
        string lower = path.ToLowerInvariant();
        bool nameHint = lower.EndsWith("ntuser.dat", StringComparison.Ordinal) ||
                        lower.EndsWith("usrclass.dat", StringComparison.Ordinal) ||
                        lower.EndsWith("amcache.hve", StringComparison.Ordinal) ||
                        lower.EndsWith("\\system", StringComparison.Ordinal) ||
                        lower.EndsWith("\\software", StringComparison.Ordinal) ||
                        lower.EndsWith("\\sam", StringComparison.Ordinal) ||
                        lower.EndsWith("\\security", StringComparison.Ordinal) ||
                        lower.EndsWith("\\default", StringComparison.Ordinal) ||
                        lower.EndsWith("\\components", StringComparison.Ordinal) ||
                        lower.EndsWith(".hve", StringComparison.Ordinal);
        bool magic = StartsWithAscii(b, "regf");
        if (!nameHint && !magic) return null;
        if (!magic) return null;

        var p = new ParsedArtifact("Windows Registry Hive", "Registry hive file; cells and values are string-extracted best-effort");
        p.Add("Signature", "regf");
        p.Add("PrimarySequence", ReadUInt32(b, 4));
        p.Add("SecondarySequence", ReadUInt32(b, 8));
        p.Add("LastWrittenUtc", ReadFileTimeString(b, 0x0c));
        p.Add("HivePathName", ReadUtf16Fixed(b, 0x30, Math.Min(512, Math.Max(0, b.Length - 0x30))));
        AddExtractedStrings(p, b, "HiveString");
        return p;
    }

    private static ParsedArtifact? TryParseSqlite(string path, FileInfo info, byte[] b, string lowerPath)
    {
        bool magic = StartsWithAscii(b, "SQLite format 3");
        bool nameHint = lowerPath.EndsWith(".sqlite", StringComparison.Ordinal) ||
                        lowerPath.EndsWith(".sqlite3", StringComparison.Ordinal) ||
                        lowerPath.EndsWith(".db", StringComparison.Ordinal) ||
                        lowerPath.EndsWith("cookies", StringComparison.Ordinal) ||
                        lowerPath.EndsWith("history", StringComparison.Ordinal) ||
                        lowerPath.EndsWith("web data", StringComparison.Ordinal) ||
                        lowerPath.EndsWith("login data", StringComparison.Ordinal) ||
                        lowerPath.EndsWith("favicons", StringComparison.Ordinal) ||
                        lowerPath.EndsWith("top sites", StringComparison.Ordinal) ||
                        lowerPath.EndsWith("shortcuts", StringComparison.Ordinal) ||
                        lowerPath.EndsWith("places.sqlite", StringComparison.Ordinal) ||
                        lowerPath.EndsWith("activitiescache.db", StringComparison.Ordinal);
        if (!nameHint && !magic) return null;
        if (!magic) return null;

        string kind = "SQLite Database";
        if (lowerPath.Contains("history")) kind = "Browser History SQLite Database";
        else if (lowerPath.Contains("cookies")) kind = "Browser Cookies SQLite Database";
        else if (lowerPath.Contains("web data")) kind = "Browser Web Data SQLite Database";
        else if (lowerPath.Contains("login data")) kind = "Browser Login Data SQLite Database";
        else if (lowerPath.Contains("places.sqlite")) kind = "Firefox Places SQLite Database";
        else if (lowerPath.Contains("activitiescache.db")) kind = "Windows Activity History SQLite Database";

        var p = new ParsedArtifact(kind, "SQLite database; schema and visible strings extracted best-effort");
        p.Add("Header", "SQLite format 3");
        if (b.Length >= 100)
        {
            ushort pageSize = ReadUInt16BigEndian(b, 16);
            uint schemaCookie = ReadUInt32BigEndian(b, 40);
            uint textEncoding = ReadUInt32BigEndian(b, 56);
            uint userVersion = ReadUInt32BigEndian(b, 60);
            uint appId = ReadUInt32BigEndian(b, 68);
            p.Add("PageSize", pageSize == 1 ? 65536 : pageSize);
            p.Add("SchemaCookie", schemaCookie);
            p.Add("TextEncoding", textEncoding switch { 1 => "UTF-8", 2 => "UTF-16le", 3 => "UTF-16be", _ => textEncoding.ToString() });
            p.Add("UserVersion", userVersion);
            p.Add("ApplicationId", "0x" + appId.ToString("X8"));
        }
        AddExtractedStrings(p, b, "SqliteString");
        return p;
    }

    private static ParsedArtifact? TryParseEse(string path, FileInfo info, byte[] b, string lowerPath)
    {
        bool jet = b.Length > 32 && Encoding.ASCII.GetString(b, 4, Math.Min(15, b.Length - 4)).Contains("Standard Jet DB", StringComparison.OrdinalIgnoreCase);
        bool nameHint = lowerPath.EndsWith("srudb.dat", StringComparison.Ordinal) ||
                        lowerPath.EndsWith("webcachev01.dat", StringComparison.Ordinal) ||
                        lowerPath.EndsWith("qmgr0.dat", StringComparison.Ordinal) ||
                        lowerPath.EndsWith("qmgr1.dat", StringComparison.Ordinal) ||
                        lowerPath.Contains("\\windows.edb", StringComparison.Ordinal);
        if (!jet && !nameHint) return null;
        if (!jet && b.Length < 4096) return null;

        var p = new ParsedArtifact("ESE/Jet Blue Database", "Extensible Storage Engine database such as SRUM, WebCache, or Windows Search");
        p.Add("DatabaseFamily", "ESE / Jet Blue");
        if (jet) p.Add("Header", "Standard Jet DB");
        AddExtractedStrings(p, b, "EseString");
        return p;
    }

    private static ParsedArtifact? TryParseOleCompound(string path, FileInfo info, byte[] b, string lowerPath)
    {
        bool ole = b.Length >= 8 && b[0] == 0xD0 && b[1] == 0xCF && b[2] == 0x11 && b[3] == 0xE0 &&
                   b[4] == 0xA1 && b[5] == 0xB1 && b[6] == 0x1A && b[7] == 0xE1;
        bool jump = lowerPath.EndsWith(".automaticdestinations-ms", StringComparison.Ordinal) ||
                    lowerPath.EndsWith(".customdestinations-ms", StringComparison.Ordinal);
        if (!ole && !jump) return null;
        if (!ole) return null;

        string kind = jump ? "Windows Jump List OLE Compound File" : "OLE Compound File";
        var p = new ParsedArtifact(kind, "Compound document container; stream names and embedded strings extracted best-effort");
        p.Add("Signature", "D0 CF 11 E0 A1 B1 1A E1");
        AddExtractedStrings(p, b, "OleString");
        return p;
    }

    private static ParsedArtifact? TryParseScheduledTaskOrXml(string path, FileInfo info, byte[] b)
    {
        string lower = path.Replace('/', '\\').ToLowerInvariant();
        string ext = info.Extension.ToLowerInvariant();
        bool taskHint = lower.Contains("\\windows\\system32\\tasks\\") || lower.Contains("\\system32\\tasks\\");
        if (!taskHint && ext != ".xml" && !LooksLikeTextPrefix(b, "<")) return null;

        string text = DecodeBestEffort(b);
        if (!text.TrimStart().StartsWith("<", StringComparison.Ordinal)) return null;
        try
        {
            var doc = XDocument.Parse(text, LoadOptions.None);
            string rootName = doc.Root?.Name.LocalName ?? "XML";
            bool isTask = taskHint || rootName.Equals("Task", StringComparison.OrdinalIgnoreCase);
            var p = new ParsedArtifact(isTask ? "Scheduled Task XML" : "XML Document",
                isTask ? "Scheduled task definition with triggers/actions/principals" : "XML document with parsed element text");
            p.Add("XmlRoot", rootName);
            if (doc.Root != null)
            {
                foreach (var attr in doc.Root.Attributes().Take(20)) p.Add("RootAttribute." + attr.Name.LocalName, attr.Value);
            }
            if (isTask)
            {
                AddXmlElementValues(p, doc, "Author", "TaskAuthor");
                AddXmlElementValues(p, doc, "URI", "TaskURI");
                AddXmlElementValues(p, doc, "UserId", "TaskUserId");
                AddXmlElementValues(p, doc, "LogonType", "TaskLogonType");
                AddXmlElementValues(p, doc, "Command", "TaskCommand");
                AddXmlElementValues(p, doc, "Arguments", "TaskArguments");
                AddXmlElementValues(p, doc, "WorkingDirectory", "TaskWorkingDirectory");
                AddXmlElementValues(p, doc, "StartBoundary", "TaskStartBoundary");
                AddXmlElementValues(p, doc, "Description", "TaskDescription");
            }
            p.AddText(ExtractXmlText(text));
            return p;
        }
        catch { return null; }
    }

    private static void AddXmlElementValues(ParsedArtifact p, XDocument doc, string localName, string key)
    {
        foreach (var e in doc.Descendants().Where(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase)).Take(20))
            p.Add(key, e.Value);
    }

    private static ParsedArtifact? TryParseJson(string path, FileInfo info, byte[] b)
    {
        string ext = info.Extension.ToLowerInvariant();
        if (ext != ".json" && ext != ".jsonl" && !LooksLikeTextPrefix(b, "{") && !LooksLikeTextPrefix(b, "["))
            return null;
        string text = DecodeBestEffort(b);
        string trimmed = text.TrimStart();
        if (!(trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal)))
            return null;

        try
        {
            using JsonDocument doc = JsonDocument.Parse(text);
            var p = new ParsedArtifact("JSON Document", "JSON data with flattened keys and scalar values");
            int count = 0;
            FlattenJson(doc.RootElement, "$", p, ref count, 512);
            p.AddText(text);
            return p;
        }
        catch { return null; }
    }

    private static void FlattenJson(JsonElement element, string path, ParsedArtifact p, ref int count, int limit)
    {
        if (count >= limit) return;
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    if (count >= limit) break;
                    FlattenJson(prop.Value, path + "." + prop.Name, p, ref count, limit);
                }
                break;
            case JsonValueKind.Array:
                int i = 0;
                foreach (var item in element.EnumerateArray())
                {
                    if (count >= limit) break;
                    FlattenJson(item, path + "[" + i++ + "]", p, ref count, limit);
                }
                break;
            case JsonValueKind.String:
                p.Add("Json" + path, element.GetString());
                count++;
                break;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                p.Add("Json" + path, element.ToString());
                count++;
                break;
        }
    }

    private static ParsedArtifact? TryParseTextLike(string path, FileInfo info, byte[] b, string ext, string lowerPath)
    {
        bool knownText = ext is ".txt" or ".log" or ".csv" or ".ini" or ".cfg" or ".conf" or ".url"
                         or ".ps1" or ".psm1" or ".psd1" or ".bat" or ".cmd" or ".vbs" or ".js"
                         or ".yml" or ".yaml" or ".html" or ".htm" or ".rdp" or ".ovpn";
        bool logHint = lowerPath.Contains("\\logs\\", StringComparison.Ordinal) ||
                       lowerPath.Contains("\\logfiles\\", StringComparison.Ordinal) ||
                       lowerPath.Contains("consolehost_history.txt", StringComparison.Ordinal) ||
                       lowerPath.Contains("\\windows defender\\", StringComparison.Ordinal) ||
                       lowerPath.Contains("\\microsoft\\windows\\wer\\", StringComparison.Ordinal) ||
                       lowerPath.Contains("\\crashdumps\\", StringComparison.Ordinal);
        if (!knownText && !logHint && !LooksMostlyText(b)) return null;

        string kind = ext switch
        {
            ".csv" => "CSV/Text Table",
            ".ini" or ".cfg" or ".conf" => "Configuration Text File",
            ".url" => "Internet Shortcut URL File",
            ".ps1" or ".psm1" => "PowerShell Script",
            ".bat" or ".cmd" => "Windows Batch Script",
            ".vbs" or ".js" => "Script File",
            ".log" => "Log File",
            _ => logHint ? "Application Log/Text Artifact" : "Plain Text File"
        };
        var p = new ParsedArtifact(kind, "Line-oriented text/config/log artifact");
        string text = DecodeBestEffort(b);
        p.Add("ApproxLineCountInPrefix", text.Count(c => c == '\n') + 1);
        if (ext == ".url" || ext == ".ini" || ext == ".cfg" || ext == ".conf")
        {
            foreach (string line in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Take(300))
            {
                int eq = line.IndexOf('=');
                if (eq > 0 && eq < 80) p.Add("Config." + line[..eq].Trim(), line[(eq + 1)..].Trim());
            }
        }
        p.AddText(text);
        return p;
    }

    private static void AddExtractedStrings(ParsedArtifact p, byte[] b, string keyPrefix)
    {
        int idx = 0;
        foreach (string s in ExtractStrings(b, MaxStrings))
        {
            idx++;
            p.Add(keyPrefix + idx.ToString("000"), s);
            if (idx >= 80) p.AddText(s);
        }
    }

    private static IEnumerable<string> ExtractStrings(byte[] b, int limit)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string s in ExtractAsciiStrings(b, 5))
        {
            string c = ParsedArtifact.CleanText(s, 4096);
            if (c.Length >= 5 && seen.Add(c)) { yield return c; if (seen.Count >= limit) yield break; }
        }
        foreach (string s in ExtractUtf16LeStrings(b, 4))
        {
            string c = ParsedArtifact.CleanText(s, 4096);
            if (c.Length >= 4 && seen.Add(c)) { yield return c; if (seen.Count >= limit) yield break; }
        }
    }

    private static IEnumerable<string> ExtractAsciiStrings(byte[] b, int minLen)
    {
        var sb = new StringBuilder();
        foreach (byte x in b)
        {
            if (x >= 32 && x <= 126) sb.Append((char)x);
            else { if (sb.Length >= minLen) yield return sb.ToString(); sb.Clear(); }
        }
        if (sb.Length >= minLen) yield return sb.ToString();
    }

    private static IEnumerable<string> ExtractUtf16LeStrings(byte[] b, int minLen)
    {
        var sb = new StringBuilder();
        for (int i = 0; i + 1 < b.Length; i += 2)
        {
            ushort ch = (ushort)(b[i] | (b[i + 1] << 8));
            if (ch >= 32 && ch < 0xD800 && !char.IsControl((char)ch)) sb.Append((char)ch);
            else { if (sb.Length >= minLen) yield return sb.ToString(); sb.Clear(); }
        }
        if (sb.Length >= minLen) yield return sb.ToString();
    }

    private static string DecodeBestEffort(byte[] b)
    {
        if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF)
            return Encoding.UTF8.GetString(b, 3, b.Length - 3);
        if (b.Length >= 2 && b[0] == 0xFF && b[1] == 0xFE)
            return Encoding.Unicode.GetString(b, 2, b.Length - 2);
        if (b.Length >= 2 && b[0] == 0xFE && b[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(b, 2, b.Length - 2);
        if (LooksUtf16Le(b)) return Encoding.Unicode.GetString(b);
        return Encoding.UTF8.GetString(b);
    }

    private static bool LooksUtf16Le(byte[] b)
    {
        int sample = Math.Min(b.Length, 4096);
        if (sample < 8) return false;
        int zeroOdd = 0;
        for (int i = 1; i < sample; i += 2) if (b[i] == 0) zeroOdd++;
        return zeroOdd > sample / 8;
    }

    private static bool LooksMostlyText(byte[] b)
    {
        int sample = Math.Min(b.Length, 8192);
        if (sample == 0) return false;
        int printable = 0, zero = 0;
        for (int i = 0; i < sample; i++)
        {
            byte x = b[i];
            if (x == 0) zero++;
            if (x == 9 || x == 10 || x == 13 || (x >= 32 && x <= 126) || x >= 0x80) printable++;
        }
        return zero < sample / 20 && printable > sample * 85 / 100;
    }

    private static bool LooksMostlyText(ReadOnlySpan<byte> b)
    {
        int sample = Math.Min(b.Length, 8192);
        if (sample == 0) return false;
        int printable = 0, zero = 0;
        for (int i = 0; i < sample; i++)
        {
            byte x = b[i];
            if (x == 0) zero++;
            if (x == 9 || x == 10 || x == 13 || (x >= 32 && x <= 126) || x >= 0x80) printable++;
        }
        return zero < sample / 20 && printable > sample * 85 / 100;
    }

    private static bool LooksLikeTextPrefix(byte[] b, string prefix)
    {
        if (b.Length == 0) return false;
        string text = DecodeBestEffort(b.Take(Math.Min(b.Length, 4096)).ToArray()).TrimStart();
        return text.StartsWith(prefix, StringComparison.Ordinal);
    }

    private static bool LooksLikeTextPrefix(ReadOnlySpan<byte> b, string prefix)
    {
        if (b.Length == 0) return false;
        int length = Math.Min(b.Length, 4096);
        string text = DecodeBestEffort(b[..length].ToArray()).TrimStart();
        return text.StartsWith(prefix, StringComparison.Ordinal);
    }

    private static string ExtractXmlText(string xml)
    {
        try { return string.Join(" ", XDocument.Parse(xml).DescendantNodes().OfType<XText>().Select(t => t.Value)); }
        catch { return ParsedArtifact.CleanText(xml.Replace('<', ' ').Replace('>', ' '), 12000); }
    }

    private static bool StartsWithAscii(byte[] b, string text)
    {
        if (b.Length < text.Length) return false;
        for (int i = 0; i < text.Length; i++) if (b[i] != (byte)text[i]) return false;
        return true;
    }

    private static bool StartsWithAscii(ReadOnlySpan<byte> b, string text, int offset = 0)
    {
        if (offset < 0 || b.Length - offset < text.Length) return false;
        for (int i = 0; i < text.Length; i++) if (b[offset + i] != (byte)text[i]) return false;
        return true;
    }

    private static string ReadAsciiFixed(byte[] b, int offset, int count)
    {
        if (offset < 0 || offset >= b.Length) return "";
        count = Math.Min(count, b.Length - offset);
        return Encoding.ASCII.GetString(b, offset, count);
    }

    private static string ReadUtf16Fixed(byte[] b, int offset, int count)
    {
        if (offset < 0 || offset >= b.Length || count <= 0) return "";
        count = Math.Min(count, b.Length - offset);
        if ((count & 1) == 1) count--;
        return Encoding.Unicode.GetString(b, offset, count).TrimEnd('\0', ' ');
    }

    private static ushort ReadUInt16(byte[] b, int o) =>
        o + 1 < b.Length ? (ushort)(b[o] | (b[o + 1] << 8)) : (ushort)0;

    private static ushort ReadUInt16BigEndian(byte[] b, int o) =>
        o + 1 < b.Length ? (ushort)((b[o] << 8) | b[o + 1]) : (ushort)0;

    private static int ReadInt32(byte[] b, int o) =>
        o + 3 < b.Length ? b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24) : 0;

    private static int ReadInt32(ReadOnlySpan<byte> b, int o) =>
        o + 3 < b.Length ? b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24) : 0;

    private static uint ReadUInt32(byte[] b, int o) => unchecked((uint)ReadInt32(b, o));

    private static uint ReadUInt32(ReadOnlySpan<byte> b, int o) => unchecked((uint)ReadInt32(b, o));

    private static uint ReadUInt32BigEndian(byte[] b, int o) =>
        o + 3 < b.Length ? unchecked((uint)((b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3])) : 0;

    private static string ReadFileTimeString(byte[] b, int offset)
    {
        if (offset + 7 >= b.Length) return "";
        long value = BitConverter.ToInt64(b, offset);
        if (value <= 0) return "";
        try { return DateTime.FromFileTimeUtc(value).ToString("O"); } catch { return ""; }
    }
}

// Utilities
