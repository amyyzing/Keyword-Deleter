namespace Scanner.Core;

internal static class ArtifactCatalog
{
    public static IEnumerable<ArtifactRoot> GetArtifactRoots() => GetArtifactRoots(ArtifactCatalogEnvironment.Current());

    internal static IReadOnlyList<ArtifactRoot> GetArtifactRoots(ArtifactCatalogEnvironment environment)
    {
        var items = new List<ArtifactRoot>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string name, string path, ArtifactTargetMode mode, bool cleanupEligible, bool requireExists = true)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return;

                string normalized = NormalizeForDedupe(path);
                if (!seen.Add(mode + "|" + normalized))
                    return;

                bool isFile = mode == ArtifactTargetMode.File;
                if (!requireExists)
                {
                    items.Add(new ArtifactRoot(name, path, isFile, cleanupEligible));
                    return;
                }

                if (isFile && File.Exists(path))
                    items.Add(new ArtifactRoot(name, path, true, cleanupEligible));
                else if (!isFile && Directory.Exists(path))
                    items.Add(new ArtifactRoot(name, path, false, cleanupEligible));
            }
            catch { }
        }

        void AddDefinition(ArtifactTargetDefinition definition, IReadOnlyDictionary<string, string> values)
        {
            string path = ResolveTemplate(definition.Template, values);
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (!definition.ExpandWildcards && !ContainsWildcard(path))
            {
                Add(definition.Name, path, definition.Mode, definition.CleanupEligible, definition.RequireExists);
                return;
            }

            foreach (string expanded in ExpandWildcardPath(path))
                Add(definition.Name, expanded, definition.Mode, definition.CleanupEligible, requireExists: true);
        }

        void AddRegistryHiveFamily(string name, string path, bool cleanupEligible)
        {
            Add(name, path, ArtifactTargetMode.File, cleanupEligible);
            Add(name + "-LOG1", path + ".LOG1", ArtifactTargetMode.File, cleanupEligible);
            Add(name + "-LOG2", path + ".LOG2", ArtifactTargetMode.File, cleanupEligible);

            try
            {
                string? dir = Path.GetDirectoryName(path);
                string file = Path.GetFileName(path);
                if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(file) || !Directory.Exists(dir))
                    return;

                foreach (string sidecar in Directory.EnumerateFiles(dir, file + "*.regtrans-ms"))
                    Add(name + "-RegTrans", sidecar, ArtifactTargetMode.File, cleanupEligible);
            }
            catch { }
        }

        void AddRegistryHiveSet(string category, string directory, IEnumerable<string> hives)
        {
            foreach (string hive in hives)
                AddRegistryHiveFamily(category + "-" + hive, Path.Combine(directory, hive), cleanupEligible: false);
        }

        void AddNtfsMetadataRoots(string driveRoot)
        {
            try
            {
                string root = driveRoot.TrimEnd('\\', '/');
                Add("NTFS-MFT", Path.Combine(root + "\\", "$MFT"), ArtifactTargetMode.File, cleanupEligible: false, requireExists: false);
                Add("NTFS-LogFile", Path.Combine(root + "\\", "$LogFile"), ArtifactTargetMode.File, cleanupEligible: false, requireExists: false);
                Add("NTFS-USNJournal", Path.Combine(root + "\\", "$Extend", "$UsnJrnl:$J"), ArtifactTargetMode.File, cleanupEligible: false, requireExists: false);
            }
            catch { }
        }

        var machineValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Windows"] = environment.Windows,
            ["SystemDrive"] = environment.SystemDrive,
            ["ProgramData"] = environment.ProgramData,
            ["ProgramFiles"] = environment.ProgramFiles,
            ["ProgramFilesX86"] = environment.ProgramFilesX86
        };

        foreach (var definition in MachineTargets)
            AddDefinition(definition, machineValues);

        AddRegistryHiveSet(
            "SystemHive",
            Path.Combine(environment.Windows, "System32", "Config"),
            new[] { "SYSTEM", "SOFTWARE", "SAM", "SECURITY", "DEFAULT", "COMPONENTS" });
        AddRegistryHiveSet(
            "SystemHive-RegBack",
            Path.Combine(environment.Windows, "System32", "Config", "RegBack"),
            new[] { "SYSTEM", "SOFTWARE", "SAM", "SECURITY", "DEFAULT", "COMPONENTS" });
        AddRegistryHiveFamily("AmcacheHive", Path.Combine(environment.Windows, "AppCompat", "Programs", "Amcache.hve"), cleanupEligible: false);

        foreach (string driveRoot in environment.FixedDriveRoots)
        {
            try
            {
                Add("RecycleBin", Path.Combine(driveRoot, "$Recycle.Bin"), ArtifactTargetMode.Directory, cleanupEligible: false);
                AddNtfsMetadataRoots(driveRoot);
            }
            catch { }
        }

        foreach (var profile in SafeEnumerateDirectories(environment.UsersRoot))
        {
            string name = Path.GetFileName(profile);
            if (name.Equals("Public", StringComparison.OrdinalIgnoreCase) || name.Equals("Default", StringComparison.OrdinalIgnoreCase))
                continue;

            string roaming = Path.Combine(profile, "AppData", "Roaming");
            string local = Path.Combine(profile, "AppData", "Local");
            string localLow = Path.Combine(profile, "AppData", "LocalLow");

            var userValues = new Dictionary<string, string>(machineValues, StringComparer.OrdinalIgnoreCase)
            {
                ["UserProfile"] = profile,
                ["Roaming"] = roaming,
                ["Local"] = local,
                ["LocalLow"] = localLow,
                ["Documents"] = Path.Combine(profile, "Documents")
            };

            AddRegistryHiveFamily("NTUSER-DAT", Path.Combine(profile, "NTUSER.DAT"), cleanupEligible: false);
            AddRegistryHiveFamily("UsrClassDat-ShellbagsHive", Path.Combine(local, "Microsoft", "Windows", "UsrClass.dat"), cleanupEligible: false);

            foreach (var definition in UserTargets)
                AddDefinition(definition, userValues);

            string packages = Path.Combine(local, "Packages");
            foreach (string package in SafeEnumerateDirectories(packages))
                AddRegistryHiveFamily("PackageSettingsHive", Path.Combine(package, "Settings", "settings.dat"), cleanupEligible: false);

            string cdp = Path.Combine(local, "ConnectedDevicesPlatform");
            foreach (var dir in SafeEnumerateDirectories(cdp))
                Add("ActivitiesCache", Path.Combine(dir, "ActivitiesCache.db"), ArtifactTargetMode.File, cleanupEligible: false);
        }

        return items;
    }

    private static readonly ArtifactTargetDefinition[] MachineTargets =
    [
        new("Execution", "Prefetch", "{Windows}\\Prefetch", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("EventLogs", "EventLogs", "{Windows}\\System32\\winevt\\Logs", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("EventLogs", "LegacyEventLogs", "{Windows}\\System32\\Config\\*.evt", ArtifactTargetMode.File, CleanupEligible: false, ExpandWildcards: true),
        new("Execution", "AppCompatPrograms", "{Windows}\\AppCompat\\Programs", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("Execution", "RecentFileCache", "{Windows}\\AppCompat\\Programs\\RecentFileCache.bcf", ArtifactTargetMode.File, CleanupEligible: false),
        new("Execution", "AppCompatPCA-Windows", "{Windows}\\AppCompat\\PCA", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("Execution", "AppCompatPCA-ProgramData", "{ProgramData}\\Microsoft\\Windows\\AppCompat\\PCA", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("Execution", "SDB-AppPatch", "{Windows}\\AppPatch", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("Execution", "SDB-Custom", "{Windows}\\AppPatch\\Custom", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("Execution", "SDB-CustomSDB", "{Windows}\\AppPatch\\CustomSDB", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("SRUM", "SRUM", "{Windows}\\System32\\sru\\SRUDB.dat", ArtifactTargetMode.File, CleanupEligible: false),
        new("ScheduledTasks", "ScheduledTasks", "{Windows}\\System32\\Tasks", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("WMI", "WMIRepository", "{Windows}\\System32\\wbem\\Repository", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("Temp", "WindowsTemp", "{Windows}\\Temp", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Search", "WindowsSearch", "{ProgramData}\\Microsoft\\Search\\Data\\Applications\\Windows", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("WER", "WER-ProgramData", "{ProgramData}\\Microsoft\\Windows\\WER", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("Defender", "DefenderSupport", "{ProgramData}\\Microsoft\\Windows Defender\\Support", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("Defender", "DefenderDetectionHistory", "{ProgramData}\\Microsoft\\Windows Defender\\Scans\\History\\Service\\DetectionHistory", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("Defender", "DefenderResults", "{ProgramData}\\Microsoft\\Windows Defender\\Scans\\History\\Results", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("Defender", "DefenderQuarantine", "{ProgramData}\\Microsoft\\Windows Defender\\Quarantine", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("Startup", "ProgramDataStartup", "{ProgramData}\\Microsoft\\Windows\\Start Menu\\Programs\\Startup", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Installer", "ProgramDataPackageCache", "{ProgramData}\\Package Cache", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("BITS", "BITS-Qmgr", "{ProgramData}\\Microsoft\\Network\\Downloader\\qmgr*.dat", ArtifactTargetMode.File, CleanupEligible: false, ExpandWildcards: true),
        new("Firewall", "WindowsFirewallLogs", "{Windows}\\System32\\LogFiles\\Firewall", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("SetupAPI", "SetupAPILogs", "{Windows}\\INF\\setupapi*.log", ArtifactTargetMode.File, CleanupEligible: false, ExpandWildcards: true),
        new("CrashDumps", "WindowsMinidumps", "{Windows}\\Minidump", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("OpenSSH", "OpenSSH-ProgramData", "{ProgramData}\\ssh", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("OpenSSH", "OpenSSH-Windows", "{Windows}\\System32\\OpenSSH", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("RemoteAdmin", "AnyDesk-ProgramData", "{ProgramData}\\AnyDesk", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("RemoteAdmin", "TeamViewer-ProgramData", "{ProgramData}\\TeamViewer", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("RemoteAdmin", "ScreenConnect-ProgramData", "{ProgramData}\\ScreenConnect Client", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("RemoteAdmin", "RustDesk-ProgramData", "{ProgramData}\\RustDesk", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("RemoteAdmin", "Splashtop-ProgramData", "{ProgramData}\\Splashtop", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("RemoteAdmin", "Tailscale-ProgramData", "{ProgramData}\\Tailscale", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("VPN", "OpenVPN-ProgramData", "{ProgramData}\\OpenVPN", ArtifactTargetMode.Directory, CleanupEligible: false)
    ];

    private static readonly ArtifactTargetDefinition[] UserTargets =
    [
        new("Recent", "RecentFiles", "{Roaming}\\Microsoft\\Windows\\Recent", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Recent", "JumpLists-AutomaticDestinations", "{Roaming}\\Microsoft\\Windows\\Recent\\AutomaticDestinations", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Recent", "JumpLists-CustomDestinations", "{Roaming}\\Microsoft\\Windows\\Recent\\CustomDestinations", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Startup", "UserStartup", "{Roaming}\\Microsoft\\Windows\\Start Menu\\Programs\\Startup", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("PowerShell", "PowerShellHistory", "{Roaming}\\Microsoft\\Windows\\PowerShell\\PSReadLine\\ConsoleHost_history.txt", ArtifactTargetMode.File, CleanupEligible: false),
        new("PowerShell", "PowerShellTranscripts-Documents", "{Documents}\\PowerShell_transcript*", ArtifactTargetMode.File, CleanupEligible: false, ExpandWildcards: true),
        new("PowerShell", "PowerShellTranscripts-WindowsPowerShell", "{Documents}\\WindowsPowerShell\\Transcripts", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("PowerShell", "PowerShellTranscripts-PowerShell", "{Documents}\\PowerShell\\Transcripts", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("PowerShell", "PowerShellProfile-WindowsPowerShell", "{Documents}\\WindowsPowerShell\\*.ps1", ArtifactTargetMode.File, CleanupEligible: false, ExpandWildcards: true),
        new("PowerShell", "PowerShellProfile-PowerShell", "{Documents}\\PowerShell\\*.ps1", ArtifactTargetMode.File, CleanupEligible: false, ExpandWildcards: true),
        new("ActivityHistory", "ConnectedDevicesPlatform-ActivityHistory", "{Local}\\ConnectedDevicesPlatform", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("Explorer", "ExplorerCache", "{Local}\\Microsoft\\Windows\\Explorer", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("WebCache", "WebCache", "{Local}\\Microsoft\\Windows\\WebCache", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("WebCache", "INetCache", "{Local}\\Microsoft\\Windows\\INetCache", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("WER", "WER-User", "{Local}\\Microsoft\\Windows\\WER", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("CrashDumps", "CrashDumps", "{Local}\\CrashDumps", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("Temp", "UserTemp", "{Local}\\Temp", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Browser", "ChromeUserData", "{Local}\\Google\\Chrome\\User Data", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Browser", "EdgeUserData", "{Local}\\Microsoft\\Edge\\User Data", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Browser", "FirefoxProfiles", "{Roaming}\\Mozilla\\Firefox\\Profiles", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Browser", "BraveUserData", "{Local}\\BraveSoftware\\Brave-Browser\\User Data", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Browser", "OperaStable", "{Roaming}\\Opera Software\\Opera Stable", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Browser", "OperaGXStable", "{Roaming}\\Opera Software\\Opera GX Stable", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Browser", "VivaldiUserData", "{Local}\\Vivaldi\\User Data", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Browser", "ChromiumUserData", "{Local}\\Chromium\\User Data", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Browser", "ZenProfiles", "{Roaming}\\zen\\Profiles", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Browser", "ArcUserData", "{Local}\\Packages\\TheBrowserCompany.Arc_*\\LocalCache\\Local\\Arc\\User Data", ArtifactTargetMode.Directory, CleanupEligible: true, ExpandWildcards: true),
        new("Messaging", "Discord", "{Roaming}\\discord", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Messaging", "DiscordCanary", "{Roaming}\\discordcanary", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Messaging", "DiscordPTB", "{Roaming}\\discordptb", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Messaging", "Slack", "{Roaming}\\Slack", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Messaging", "TelegramDesktop", "{Roaming}\\Telegram Desktop", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Messaging", "Signal", "{Roaming}\\Signal", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Sync", "OneDriveLogs", "{Local}\\Microsoft\\OneDrive\\logs", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("Sync", "OneDriveSettings", "{Local}\\Microsoft\\OneDrive\\settings", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("Sync", "GoogleDrive", "{Local}\\Google\\DriveFS", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Sync", "Dropbox", "{Roaming}\\Dropbox", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("RDP", "RDPCache", "{Local}\\Microsoft\\Terminal Server Client\\Cache", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("RDP", "DefaultRdp", "{Documents}\\Default.rdp", ArtifactTargetMode.File, CleanupEligible: false),
        new("OpenSSH", "OpenSSH-User", "{UserProfile}\\.ssh", ArtifactTargetMode.Directory, CleanupEligible: false),
        new("RemoteAdmin", "AnyDesk-User", "{Roaming}\\AnyDesk", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("RemoteAdmin", "TeamViewer-User", "{Roaming}\\TeamViewer", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("RemoteAdmin", "RustDesk-User", "{Roaming}\\RustDesk", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("RemoteAdmin", "ScreenConnect-User", "{Local}\\ScreenConnect Client", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("RemoteAdmin", "Splashtop-User", "{Roaming}\\Splashtop", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("RemoteAdmin", "VNC-RealVNC", "{Roaming}\\RealVNC", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("RemoteAdmin", "VNC-UltraVNC", "{Roaming}\\UltraVNC", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("RemoteAdmin", "VNC-TightVNC", "{Roaming}\\TightVNC", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("RemoteAdmin", "WinSCP-Roaming", "{Roaming}\\WinSCP.ini", ArtifactTargetMode.File, CleanupEligible: true),
        new("VPN", "OpenVPN-User", "{UserProfile}\\OpenVPN\\config", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("VPN", "Tailscale-User", "{Local}\\Tailscale", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Sync", "RcloneConfig", "{Roaming}\\rclone\\rclone.conf", ArtifactTargetMode.File, CleanupEligible: true),
        new("Games", "RobloxLogs", "{Local}\\Roblox\\logs", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("Games", "MinecraftLogs", "{Roaming}\\.minecraft\\logs", ArtifactTargetMode.Directory, CleanupEligible: true),
        new("UserData", "LocalLow", "{LocalLow}", ArtifactTargetMode.Directory, CleanupEligible: true)
    ];

    private static IEnumerable<string> SafeEnumerateDirectories(string path)
    {
        try { return Directory.Exists(path) ? Directory.EnumerateDirectories(path).ToArray() : Array.Empty<string>(); }
        catch { return Array.Empty<string>(); }
    }

    private static string ResolveTemplate(string template, IReadOnlyDictionary<string, string> values)
    {
        string path = template;
        foreach (var (key, value) in values)
            path = path.Replace("{" + key + "}", value ?? "", StringComparison.OrdinalIgnoreCase);

        return path;
    }

    private static string NormalizeForDedupe(string path)
    {
        try { return Path.GetFullPath(path).TrimEnd('\\', '/'); }
        catch { return path.TrimEnd('\\', '/'); }
    }

    private static bool ContainsWildcard(string path) => path.IndexOfAny(['*', '?']) >= 0;

    private static IEnumerable<string> ExpandWildcardPath(string path)
    {
        string normalized = path.Replace('/', '\\');
        string root = Path.GetPathRoot(normalized) ?? "";
        if (string.IsNullOrWhiteSpace(root))
            yield break;

        var candidates = new List<string> { root.TrimEnd('\\') + "\\" };
        foreach (string part in normalized[root.Length..].Split('\\', StringSplitOptions.RemoveEmptyEntries))
        {
            var next = new List<string>();
            bool wildcard = ContainsWildcard(part);
            foreach (string candidate in candidates)
            {
                try
                {
                    if (wildcard)
                    {
                        string directory = candidate.TrimEnd('\\');
                        if (Directory.Exists(directory))
                            next.AddRange(Directory.EnumerateFileSystemEntries(directory, part));
                    }
                    else
                    {
                        next.Add(Path.Combine(candidate, part));
                    }
                }
                catch { }
            }

            candidates = next;
            if (candidates.Count == 0)
                yield break;
        }

        foreach (string candidate in candidates)
            yield return candidate;
    }

    public static string ClassifyFilePath(string path)
    {
        string p = path.Replace('/', '\\').ToLowerInvariant();
        if (p.Contains("\\windows\\prefetch\\")) return "Prefetch-Execution";
        if (p.EndsWith("\\$mft")) return "NTFS-MFT";
        if (p.EndsWith("\\$logfile")) return "NTFS-LogFile";
        if (p.Contains("\\$extend\\$usnjrnl:$j")) return "NTFS-USNJournal";
        if (p.Contains("\\system32\\winevt\\logs\\") || p.EndsWith(".evtx")) return "EventLog-EVTX";
        if (p.Contains("\\system32\\config\\regback\\")) return "SystemHive-RegBack";
        if (p.Contains("\\system32\\config\\") && IsSystemHivePath(p)) return "SystemHive";
        if (p.EndsWith("\\amcache.hve")) return "Amcache-Execution";
        if (p.EndsWith("\\amcache.hve.log1") || p.EndsWith("\\amcache.hve.log2") || p.Contains("\\amcache.hve") && p.EndsWith(".regtrans-ms"))
            return "Amcache-TransactionLog";
        if (p.Contains("\\appcompat\\pca\\")) return "AppCompat-PCA";
        if (p.EndsWith("\\recentfilecache.bcf")) return "RecentFileCache";
        if (p.Contains("\\appcompat\\programs\\")) return "AppCompat-Execution";
        if (p.Contains("\\apppatch\\")) return "SDB-AppPatch";
        if (p.Contains("\\microsoft\\network\\downloader\\qmgr")) return "BITS-Qmgr";
        if (p.EndsWith("\\srudb.dat")) return "SRUM-NetworkAppUsage";
        if (p.Contains("\\system32\\tasks\\")) return "ScheduledTask";
        if (p.Contains("\\wbem\\repository\\")) return "WMIRepository";
        if (p.Contains("\\recent\\automaticdestinations\\")) return "JumpList-AutomaticDestinations";
        if (p.Contains("\\recent\\customdestinations\\")) return "JumpList-CustomDestinations";
        if (p.Contains("\\microsoft\\windows\\recent\\")) return "RecentFiles";
        if (p.EndsWith("\\consolehost_history.txt")) return "PowerShellHistory";
        if (p.Contains("\\powershell\\transcripts\\") || p.Contains("\\windowspowershell\\transcripts\\")) return "PowerShellTranscripts";
        if (p.Contains("\\connecteddevicesplatform\\") || p.EndsWith("\\activitiescache.db")) return "ActivityHistory-Timeline";
        if (p.Contains("\\packages\\") && p.Contains("\\settings\\settings.dat")) return "PackageSettingsHive";
        if (p.EndsWith("\\usrclass.dat")) return "UsrClassDat-ShellbagsHive";
        if (p.EndsWith("\\usrclass.dat.log1") || p.EndsWith("\\usrclass.dat.log2") || p.Contains("\\usrclass.dat") && p.EndsWith(".regtrans-ms"))
            return "UsrClassDat-TransactionLog";
        if (p.EndsWith("\\ntuser.dat")) return "NTUSER-DAT";
        if (p.EndsWith("\\ntuser.dat.log1") || p.EndsWith("\\ntuser.dat.log2") || p.Contains("\\ntuser.dat") && p.EndsWith(".regtrans-ms"))
            return "NTUSER-DAT-TransactionLog";
        if (p.Contains("\\microsoft\\windows\\explorer\\")) return "ExplorerCache-Thumbcache-Iconcache";
        if (p.Contains("\\webcache\\")) return "WebCache";
        if (p.Contains("\\inetcache\\")) return "INetCache";
        if (p.Contains("\\crashdumps\\") || p.Contains("\\minidump\\")) return "CrashDumps";
        if (p.Contains("\\windows defender\\support\\")) return "DefenderSupport";
        if (p.Contains("\\windows defender\\scans\\history\\service\\detectionhistory\\")) return "DefenderDetectionHistory";
        if (p.Contains("\\windows defender\\scans\\history\\results\\")) return "DefenderResults";
        if (p.Contains("\\windows defender\\quarantine\\")) return "DefenderQuarantine";
        if (p.Contains("\\logfiles\\firewall\\")) return "WindowsFirewall";
        if (p.Contains("\\inf\\setupapi") && p.EndsWith(".log")) return "SetupAPI";
        if (p.Contains("\\microsoft\\search\\data\\applications\\windows\\")) return "WindowsSearchIndex";
        if (p.Contains("\\$recycle.bin\\")) return "RecycleBin";
        if (p.Contains("\\bravesoftware\\brave-browser\\user data\\")) return "BrowserProfile-Brave";
        if (p.Contains("\\opera software\\")) return "BrowserProfile-Opera";
        if (p.Contains("\\vivaldi\\user data\\")) return "BrowserProfile-Vivaldi";
        if (p.Contains("\\chromium\\user data\\")) return "BrowserProfile-Chromium";
        if (p.Contains("\\zen\\profiles\\")) return "BrowserProfile-Zen";
        if (p.Contains("\\thebrowsercompany.arc_") && p.Contains("\\arc\\user data\\")) return "BrowserProfile-Arc";
        if (p.Contains("\\user data\\") && (p.Contains("\\chrome\\") || p.Contains("\\edge\\"))) return "BrowserProfile-ChromeEdge";
        if (p.Contains("\\firefox\\profiles\\")) return "BrowserProfile-Firefox";
        if (p.Contains("\\discord")) return "Discord";
        if (p.Contains("\\slack\\")) return "Slack";
        if (p.Contains("\\telegram desktop\\")) return "Telegram";
        if (p.Contains("\\signal\\")) return "Signal";
        if (p.Contains("\\onedrive\\logs\\") || p.Contains("\\onedrive\\settings\\")) return "OneDrive";
        if (p.Contains("\\google\\drivefs\\")) return "GoogleDrive";
        if (p.Contains("\\dropbox\\")) return "Dropbox";
        if (p.Contains("\\terminal server client\\cache\\")) return "RDPCache";
        if (p.EndsWith("\\default.rdp")) return "RDP-DefaultFile";
        if (p.Contains("\\.ssh\\")) return "OpenSSH";
        if (p.Contains("\\anydesk\\")) return "RemoteAdmin-AnyDesk";
        if (p.Contains("\\teamviewer\\")) return "RemoteAdmin-TeamViewer";
        if (p.Contains("\\screenconnect")) return "RemoteAdmin-ScreenConnect";
        if (p.Contains("\\rustdesk\\")) return "RemoteAdmin-RustDesk";
        if (p.Contains("\\splashtop\\")) return "RemoteAdmin-Splashtop";
        if (p.Contains("\\realvnc\\") || p.Contains("\\ultravnc\\") || p.Contains("\\tightvnc\\")) return "RemoteAdmin-VNC";
        if (p.EndsWith("\\winscp.ini")) return "WinSCP";
        if (p.Contains("\\openvpn\\")) return "OpenVPN";
        if (p.Contains("\\tailscale\\")) return "Tailscale";
        if (p.EndsWith("\\rclone.conf")) return "RcloneConfig";
        if (p.Contains("\\roblox\\logs\\")) return "RobloxLogs";
        if (p.Contains("\\.minecraft\\logs\\")) return "MinecraftLogs";
        if (p.Contains("\\windows\\temp\\") || p.Contains("\\appdata\\local\\temp\\")) return "Temp";
        return "";
    }

    public static string ClassifyRegistryPath(string path)
    {
        string p = path.ToLowerInvariant();
        if (p.Contains("\\software\\classes\\local settings\\software\\microsoft\\windows\\shell\\bagmru") ||
            p.Contains("\\software\\classes\\local settings\\software\\microsoft\\windows\\shell\\bags"))
            return "Shellbags";
        if (p.Contains("\\software\\microsoft\\windows\\currentversion\\explorer\\userassist"))
            return "UserAssist-Execution";
        if (p.Contains("\\software\\microsoft\\windows\\currentversion\\explorer\\runmru"))
            return "RunMRU";
        if (p.Contains("\\software\\microsoft\\windows\\currentversion\\explorer\\recentdocs"))
            return "RecentDocs";
        if (p.Contains("\\software\\microsoft\\windows\\currentversion\\explorer\\typedpaths"))
            return "TypedPaths";
        if (p.Contains("\\software\\microsoft\\windows\\currentversion\\explorer\\comdlg32\\opensavepidlmru"))
            return "OpenSavePidlMRU";
        if (p.Contains("\\software\\classes\\local settings\\software\\microsoft\\windows\\shell\\muicache"))
            return "MUICache";
        if (p.Contains("\\software\\microsoft\\windows nt\\currentversion\\appcompatflags"))
            return "AppCompatFlags";
        if (p.Contains("\\system\\currentcontrolset\\services\\bam") ||
            p.Contains("\\system\\currentcontrolset\\services\\dam"))
            return "BAM-DAM-Execution";
        if (p.Contains("\\system\\currentcontrolset\\control\\session manager\\appcompatcache"))
            return "ShimCache-AppCompatCache";
        if (p.Contains("\\software\\microsoft\\windows\\currentversion\\run"))
            return "RunKey-Persistence";
        if (p.Contains("\\system\\currentcontrolset\\services"))
            return "Services-Drivers";
        if (p.Contains("\\software\\microsoft\\windows\\currentversion\\uninstall"))
            return "Uninstall-InstalledPrograms";
        if (p.Contains("\\software\\microsoft\\windows\\currentversion\\explorer\\mountpoints2"))
            return "MountPoints2";
        if (p.Contains("\\system\\mounteddevices"))
            return "MountedDevices";
        if (p.Contains("\\enum\\usbstor"))
            return "USBSTOR";
        if (p.Contains("\\software\\microsoft\\windows\\currentversion\\explorer\\fileexts"))
            return "FileExts-OpenWith";
        if (p.Contains("\\software\\microsoft\\windows\\shell\\bags") ||
            p.Contains("\\software\\microsoft\\windows\\shell\\bagmru"))
            return "Shellbags";
        if (p.Contains("\\software\\microsoft\\command processor"))
            return "CommandProcessor";
        if (p.Contains("\\software\\microsoft\\powershell"))
            return "PowerShell";
        if (p.Contains("\\software\\openssh"))
            return "OpenSSH";
        if (p.Contains("\\software\\microsoft\\windows defender\\exclusions"))
            return "DefenderExclusions";
        if (p.Contains("\\software\\microsoft\\windows nt\\currentversion\\image file execution options"))
            return "IFEO";
        if (p.Contains("\\software\\microsoft\\terminal server client"))
            return "RDP-TerminalServerClient";
        if (p.Contains("\\software\\classes\\applications"))
            return "RegisteredApplications";
        if (p.Contains("\\software\\microsoft\\windows\\currentversion\\app paths"))
            return "AppPaths";
        if (p.Contains("\\software\\microsoft\\windows\\currentversion\\internet settings\\zonemap"))
            return "InternetSettings-ZoneMap";
        if (p.Contains("\\software\\microsoft\\windows\\currentversion\\policies"))
            return "WindowsPolicies";
        return "";
    }

    private static bool IsSystemHivePath(string path)
    {
        string file = Path.GetFileName(path);
        if (file.EndsWith(".log1", StringComparison.OrdinalIgnoreCase) ||
            file.EndsWith(".log2", StringComparison.OrdinalIgnoreCase) ||
            file.EndsWith(".regtrans-ms", StringComparison.OrdinalIgnoreCase))
        {
            file = file.Split('.')[0];
        }

        return file is "system" or "software" or "sam" or "security" or "default" or "components";
    }

    internal sealed class ArtifactCatalogEnvironment
    {
        public string Windows { get; init; } = "";
        public string SystemDrive { get; init; } = "";
        public string UsersRoot { get; init; } = "";
        public string ProgramData { get; init; } = "";
        public string ProgramFiles { get; init; } = "";
        public string ProgramFilesX86 { get; init; } = "";
        public IReadOnlyList<string> FixedDriveRoots { get; init; } = Array.Empty<string>();

        public static ArtifactCatalogEnvironment Current()
        {
            string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string systemDrive = Path.GetPathRoot(win) ?? "C:\\";
            return new ArtifactCatalogEnvironment
            {
                Windows = win,
                SystemDrive = systemDrive.TrimEnd('\\', '/'),
                UsersRoot = Path.Combine(systemDrive, "Users"),
                ProgramData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                ProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                ProgramFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                FixedDriveRoots = DriveInfo.GetDrives()
                    .Where(d =>
                    {
                        try { return d.IsReady && d.DriveType == DriveType.Fixed; }
                        catch { return false; }
                    })
                    .Select(d => d.RootDirectory.FullName)
                    .ToArray()
            };
        }
    }

    private readonly record struct ArtifactTargetDefinition(
        string Category,
        string Name,
        string Template,
        ArtifactTargetMode Mode,
        bool CleanupEligible,
        bool RequireExists = true,
        bool ExpandWildcards = false);

    private enum ArtifactTargetMode
    {
        File,
        Directory
    }
}
