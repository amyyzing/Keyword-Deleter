using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Scanner.Core;

public sealed class CleanupResult
{
    public int TargetCount { get; set; }
    public int DeletedCount { get; set; }
    public int ScheduledForRebootCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public IReadOnlyList<CleanupItem> Items { get; set; } = Array.Empty<CleanupItem>();
}

public sealed class CleanupItem
{
    public string TargetType { get; set; } = "";
    public string Location { get; set; } = "";
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
}

public static class FindingCleanupService
{
    private const string Deleted = "Deleted";
    private const string ScheduledForReboot = "ScheduledForReboot";
    private const string Skipped = "Skipped";
    private const string Failed = "Failed";
    private const int MoveFileDelayUntilReboot = 0x00000004;

    public static int CountTargets(IEnumerable<ScanFinding> findings) => BuildTargets(findings).Count;

    public static CleanupResult DeleteFindings(IEnumerable<ScanFinding> findings) =>
        DeleteFindings(findings, logPath: null);

    public static CleanupResult DeleteFindings(IEnumerable<ScanFinding> findings, string? logPath)
    {
        var targets = BuildTargets(findings)
            .OrderBy(t => t.Type switch
            {
                CleanupTargetType.Directory => 0,
                CleanupTargetType.RegistryValue => 1,
                CleanupTargetType.RegistryKey => 2,
                CleanupTargetType.File => 3,
                _ => 4
            })
            .ThenBy(t => t.Type == CleanupTargetType.Directory ? TargetDepth(t.Location) : 0)
            .ThenByDescending(t => t.Type == CleanupTargetType.RegistryKey ? TargetDepth(t.Location) : 0)
            .ToArray();

        string sessionId = NewSessionId();
        using var logger = CleanupLogWriter.Open(logPath);
        logger.Write("DELETE-SESSION-START", sessionId, "", "", "", $"Targets={targets.Length}");

        var items = new List<CleanupItem>(targets.Length);
        foreach (var target in targets)
        {
            logger.Write("DELETE-ATTEMPT", sessionId, target.Type.ToString(), target.Location, "", "Attempting deletion.");

            var item = DeleteTarget(target);
            items.Add(item);

            logger.Write("DELETE-RESULT", sessionId, item.TargetType, item.Location, item.Status, item.Message);

            if (item.Status == ScheduledForReboot)
                TrackPendingRebootDeletion(logPath, logger, sessionId, item);
        }

        var result = new CleanupResult
        {
            TargetCount = targets.Length,
            DeletedCount = items.Count(i => i.Status == Deleted),
            ScheduledForRebootCount = items.Count(i => i.Status == ScheduledForReboot),
            SkippedCount = items.Count(i => i.Status == Skipped),
            FailedCount = items.Count(i => i.Status == Failed),
            Items = items
        };

        logger.Write("DELETE-SESSION-END", sessionId, "", "", "",
            $"Targets={result.TargetCount}; Deleted={result.DeletedCount}; ScheduledForReboot={result.ScheduledForRebootCount}; Skipped={result.SkippedCount}; Failed={result.FailedCount}");

        return result;
    }

    public static void LogCleanupEvent(string logPath, string eventName, string message)
    {
        AppendLog(logPath, eventName, "", "", "", "", message);
    }

    public static void LogPendingRebootDeletionStatus(string logPath)
    {
        string pendingPath = PendingRebootPath(logPath);
        if (!File.Exists(pendingPath))
            return;

        var remaining = new List<PendingRebootDeletion>();

        foreach (string line in File.ReadAllLines(pendingPath, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            PendingRebootDeletion? pending;
            try
            {
                pending = JsonSerializer.Deserialize<PendingRebootDeletion>(line);
            }
            catch (Exception ex)
            {
                AppendLog(logPath, "POST-REBOOT-CHECK-ERROR", "", "", "", Failed, "Could not read pending reboot deletion entry: " + ex.Message);
                continue;
            }

            if (pending == null || string.IsNullOrWhiteSpace(pending.Location))
                continue;

            bool stillExists = PendingTargetExists(pending);
            string status = stillExists ? "StillPresentAfterRestart" : "DeletedAfterRestart";
            string message = stillExists
                ? "Target scheduled for deletion still exists after restart."
                : "Target scheduled for deletion is no longer present after restart.";

            AppendLog(logPath, "POST-REBOOT-CHECK", pending.SessionId, pending.TargetType, pending.Location, status, message);

            if (stillExists)
                remaining.Add(pending);
        }

        if (remaining.Count == 0)
        {
            try { File.Delete(pendingPath); } catch { }
            return;
        }

        File.WriteAllLines(pendingPath, remaining.Select(p => JsonSerializer.Serialize(p)), Encoding.UTF8);
    }

    public static void LogIncompletePreviousSession(string logPath)
    {
        if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath))
            return;

        string? lastStartedSession = null;
        bool lastStartedSessionEnded = false;
        bool alreadyLoggedIncomplete = false;

        try
        {
            foreach (string line in File.ReadLines(logPath, Encoding.UTF8))
            {
                if (line.Contains("event=DELETE-SESSION-START", StringComparison.Ordinal))
                {
                    lastStartedSession = ExtractLogSession(line);
                    lastStartedSessionEnded = false;
                    alreadyLoggedIncomplete = false;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(lastStartedSession))
                    continue;

                if (!line.Contains("session=" + lastStartedSession, StringComparison.Ordinal))
                    continue;

                if (line.Contains("event=DELETE-SESSION-END", StringComparison.Ordinal))
                    lastStartedSessionEnded = true;
                else if (line.Contains("event=DELETE-PREVIOUS-SESSION-INCOMPLETE", StringComparison.Ordinal))
                    alreadyLoggedIncomplete = true;
            }
        }
        catch
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(lastStartedSession) && !lastStartedSessionEnded && !alreadyLoggedIncomplete)
        {
            AppendLog(logPath, "DELETE-PREVIOUS-SESSION-INCOMPLETE", lastStartedSession, "", "", Failed,
                "Previous delete session started but no DELETE-SESSION-END entry was logged.");
        }
    }

    private static CleanupItem DeleteTarget(CleanupTarget target)
    {
        try
        {
            if (TryGetProtectedCleanupReason(target, out string protectedReason))
                return Item(target, Skipped, protectedReason);

            return target.Type switch
            {
                CleanupTargetType.File => ForceDeleteFile(target),
                CleanupTargetType.Directory => ForceDeleteDirectory(target),
                CleanupTargetType.RegistryKey => ForceDeleteRegistryKey(target),
                CleanupTargetType.RegistryValue => ForceDeleteRegistryValue(target),
                _ => Item(target, Failed, "Unknown cleanup target type.")
            };
        }
        catch (Exception ex)
        {
            return Item(target, Failed, ex.GetType().Name + ": " + ex.Message);
        }
    }

    // ==================== FILE (aggressive) ====================
    private static CleanupItem ForceDeleteFile(CleanupTarget target)
    {
        string path = Path.GetFullPath(target.Location);

        if (!File.Exists(path))
            return Item(target, Skipped, "File not found.");

        try
        {
            ClearReadOnly(path);
            File.Delete(path);
            return File.Exists(path)
                ? Item(target, Failed, "File.Delete returned without an error, but the file still exists.")
                : Item(target, Deleted, "File deleted.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (TryCloseHandlesAndDelete(path, out string closeMsg))
                return Item(target, Deleted, "File deleted after closing open handles. " + closeMsg);

            if (TryScheduleDelete(path, out string scheduleError))
                return Item(target, ScheduledForReboot, "File locked; scheduled deletion on reboot. " + scheduleError);

            return Item(target, Failed,
                $"{ex.GetType().Name}: {ex.Message}. Handle closing failed: {closeMsg}. Reboot scheduling failed: {scheduleError}");
        }
    }

    private static bool TryCloseHandlesAndDelete(string filePath, out string message)
    {
        message = "";
        if (!EnablePrivilege("SeDebugPrivilege"))
        {
            message = "Failed to enable SeDebugPrivilege.";
            return false;
        }

        var targetPaths = BuildTargetPathSet(filePath);
        if (targetPaths.Count == 0)
        {
            message = "Could not resolve target path for handle matching.";
            return false;
        }

        if (!CloseAllHandlesToPath(targetPaths, out int closedCount, out string closeError))
        {
            message = string.IsNullOrWhiteSpace(closeError)
                ? "No open handles could be closed."
                : closeError;
            return false;
        }

        try
        {
            ClearReadOnly(filePath);
            File.Delete(filePath);
            if (!File.Exists(filePath))
            {
                message = $"Closed {closedCount} matching handle(s).";
                return true;
            }

            message = $"Closed {closedCount} matching handle(s), but the file still exists after File.Delete returned.";
            return false;
        }
        catch (Exception ex)
        {
            message = $"Closed {closedCount} matching handle(s), but delete still failed: {ex.Message}";
            return false;
        }
    }

    // ==================== DIRECTORY (aggressive) ====================
    private static CleanupItem ForceDeleteDirectory(CleanupTarget target)
    {
        string path = Path.GetFullPath(target.Location).TrimEnd('\\', '/');

        try
        {
            ClearReadOnlyRecursive(path);
        }
        catch
        {
            // Attribute cleanup is best-effort; deletion still gets a chance below.
        }

        try
        {
            if (!Directory.Exists(path))
                return Item(target, Skipped, "Directory not found.");

            Directory.Delete(path, recursive: true);
            return Directory.Exists(path)
                ? Item(target, Failed, "Directory.Delete returned without an error, but the directory still exists.")
                : Item(target, Deleted, "Directory deleted.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (TryScheduleDelete(path, out string scheduleError))
                return Item(target, ScheduledForReboot, "Directory locked; scheduled deletion on reboot. " + scheduleError);

            return Item(target, Failed, ex.GetType().Name + ": " + ex.Message + ". Reboot scheduling failed: " + scheduleError);
        }
    }

    // ==================== REGISTRY ====================
    private static CleanupItem ForceDeleteRegistryKey(CleanupTarget target)
    {
        if (!TryParseRegistryPath(target.Location, out var hive, out var view, out string subKey))
            return Item(target, Failed, "Unsupported registry path.");

        if (string.IsNullOrWhiteSpace(subKey))
            return Item(target, Skipped, "Refusing to delete a registry hive root.");

        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        using (var existing = baseKey.OpenSubKey(subKey))
        {
            if (existing == null)
                return Item(target, Skipped, "Registry key not found.");
        }

        baseKey.DeleteSubKeyTree(subKey, throwOnMissingSubKey: false);
        using (var stillExists = baseKey.OpenSubKey(subKey))
        {
            if (stillExists != null)
                return Item(target, Failed, "Registry key deletion returned without an error, but the key still exists.");
        }

        return Item(target, Deleted, "Registry key deleted.");
    }

    private static CleanupItem ForceDeleteRegistryValue(CleanupTarget target)
    {
        if (!TryParseRegistryPath(target.Location, out var hive, out var view, out string subPath))
            return Item(target, Failed, "Unsupported registry value path.");

        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        if (!TryResolveRegistryValuePath(baseKey, subPath, out string parentSubKey, out string valueName))
            return Item(target, Skipped, "Registry value not found.");

        using var parent = string.IsNullOrEmpty(parentSubKey)
            ? baseKey
            : baseKey.OpenSubKey(parentSubKey, writable: true);

        if (parent == null)
            return Item(target, Skipped, "Registry key not found.");

        object missingValue = new();
        if (ReferenceEquals(parent.GetValue(valueName, missingValue), missingValue))
            return Item(target, Skipped, "Registry value not found.");

        parent.DeleteValue(valueName, throwOnMissingValue: false);
        if (!ReferenceEquals(parent.GetValue(valueName, missingValue), missingValue))
            return Item(target, Failed, "Registry value deletion returned without an error, but the value still exists.");

        return Item(target, Deleted, "Registry value deleted.");
    }

    // ==================== TARGET SELECTION ====================
    private static List<CleanupTarget> BuildTargets(IEnumerable<ScanFinding> findings)
    {
        var targets = new Dictionary<string, CleanupTarget>(StringComparer.OrdinalIgnoreCase);
        foreach (var finding in findings)
        {
            if (!finding.CleanupEligible)
                continue;

            if (!TryCreateTarget(finding, out var target))
                continue;

            string key = $"{target.Type}|{target.Location}";
            targets.TryAdd(key, target);
        }

        return PruneCoveredFileSystemTargets(targets.Values);
    }

    private static List<CleanupTarget> PruneCoveredFileSystemTargets(IEnumerable<CleanupTarget> targets)
    {
        var targetList = targets.ToList();
        var keptDirectories = new List<(CleanupTarget Target, string Path)>();

        foreach (var target in targetList
            .Where(t => t.Type == CleanupTargetType.Directory)
            .Select(t => (Target: t, Path: NormalizeFileSystemTargetPath(t.Location)))
            .Where(t => t.Path != null)
            .OrderBy(t => t.Path!.Length))
        {
            string path = target.Path!;
            if (keptDirectories.Any(parent => IsSameOrChildPath(path, parent.Path)))
                continue;

            keptDirectories.Add((target.Target, path));
        }

        keptDirectories = keptDirectories
            .OrderBy(d => d.Path.Length)
            .ToList();

        var result = new List<CleanupTarget>(targetList.Count);
        foreach (var target in targetList)
        {
            if (target.Type == CleanupTargetType.Directory)
            {
                if (keptDirectories.Any(d => ReferenceEquals(d.Target, target)))
                    result.Add(target);

                continue;
            }

            if (target.Type == CleanupTargetType.File)
            {
                string? filePath = NormalizeFileSystemTargetPath(target.Location);
                if (filePath != null && keptDirectories.Any(d => IsSameOrChildPath(filePath, d.Path)))
                    continue;
            }

            result.Add(target);
        }

        return result;
    }

    private static bool TryCreateTarget(ScanFinding finding, out CleanupTarget target)
    {
        target = new CleanupTarget();
        if (string.IsNullOrWhiteSpace(finding.Location))
            return false;

        if (finding.Kind is "DIRECTORY-NAME" or "DIRECTORY-PATH")
        {
            target = new CleanupTarget(CleanupTargetType.Directory, finding.Location);
            return true;
        }

        if (finding.Kind.StartsWith("FILE-", StringComparison.OrdinalIgnoreCase) ||
            finding.Kind.StartsWith("ARTIFACT-", StringComparison.OrdinalIgnoreCase))
        {
            target = new CleanupTarget(CleanupTargetType.File, finding.Location);
            return true;
        }

        if (finding.Kind == "REG-KEY")
        {
            target = new CleanupTarget(CleanupTargetType.RegistryKey, finding.Location);
            return true;
        }

        if (finding.Kind is "REG-VALUE-NAME" or "REG-VALUE-DECODED-NAME" or "REG-VALUE-DATA" or "REG-BINARY-DATA")
        {
            target = new CleanupTarget(CleanupTargetType.RegistryValue, finding.Location);
            return true;
        }

        return false;
    }

    private static CleanupItem Item(CleanupTarget target, string status, string message) => new()
    {
        TargetType = target.Type.ToString(),
        Location = target.Location,
        Status = status,
        Message = message
    };

    // ==================== HELPERS ====================
    private static void ClearReadOnly(string path)
    {
        var attrs = File.GetAttributes(path);
        var cleared = attrs & ~FileAttributes.ReadOnly;
        if (cleared != attrs)
            File.SetAttributes(path, cleared);
    }

    private static void ClearReadOnlyRecursive(string directory)
    {
        if (!Directory.Exists(directory))
            return;

        var options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
            AttributesToSkip = 0
        };

        foreach (string file in Directory.EnumerateFiles(directory, "*", options))
            TryClearReadOnly(file);

        foreach (string dir in Directory.EnumerateDirectories(directory, "*", options))
            TryClearReadOnly(dir);

        TryClearReadOnly(directory);
    }

    private static void TryClearReadOnly(string path)
    {
        try { ClearReadOnly(path); } catch { }
    }

    private static bool TryScheduleDelete(string path, out string error)
    {
        if (MoveFileExW(path, null, MoveFileDelayUntilReboot))
        {
            error = "";
            return true;
        }

        error = new Win32Exception(Marshal.GetLastWin32Error()).Message;
        return false;
    }

    private static bool TryResolveRegistryValuePath(RegistryKey baseKey, string subPath, out string parentSubKey, out string valueName)
    {
        parentSubKey = "";
        valueName = "";

        if (string.IsNullOrWhiteSpace(subPath))
            return false;

        foreach (var split in EnumerateRegistryValuePathSplits(subPath))
        {
            RegistryKey? parent = null;
            bool disposeParent = false;
            try
            {
                parent = OpenRegistrySubKeyForRead(baseKey, split.ParentSubKey, out disposeParent);
            }
            catch
            {
                parent = null;
            }

            if (parent == null)
                continue;

            try
            {
                object missingValue = new();
                string candidateValueName = RegistryDisplayValueNameToActual(split.ValueName);
                if (ReferenceEquals(parent.GetValue(candidateValueName, missingValue), missingValue))
                    continue;

                parentSubKey = split.ParentSubKey;
                valueName = candidateValueName;
                return true;
            }
            catch
            {
                // Keep probing shorter parent-key prefixes. Registry value names can contain backslashes.
            }
            finally
            {
                if (disposeParent)
                    parent.Dispose();
            }
        }

        return false;
    }

    private static IEnumerable<(string ParentSubKey, string ValueName)> EnumerateRegistryValuePathSplits(string subPath)
    {
        for (int slash = subPath.LastIndexOf('\\'); slash >= 0; slash = subPath.LastIndexOf('\\', slash - 1))
            yield return (subPath[..slash], subPath[(slash + 1)..]);

        yield return ("", subPath);
    }

    private static RegistryKey? OpenRegistrySubKeyForRead(RegistryKey baseKey, string subKey, out bool disposeResult)
    {
        disposeResult = false;
        try
        {
            if (string.IsNullOrEmpty(subKey))
                return baseKey;

            disposeResult = true;
            return baseKey.OpenSubKey(subKey, writable: false);
        }
        catch
        {
            disposeResult = false;
            return null;
        }
    }

    private static string RegistryDisplayValueNameToActual(string valueName) =>
        valueName == "(Default)" ? "" : valueName;

    private static bool TryParseRegistryPath(string path, out RegistryHive hive, out RegistryView view, out string subKey)
    {
        hive = default;
        view = default;
        subKey = "";

        if (string.IsNullOrWhiteSpace(path))
            return false;

        int slash = path.IndexOf('\\');
        string root = slash < 0 ? path : path[..slash];
        subKey = slash < 0 ? "" : path[(slash + 1)..];
        RegistryView nativeView = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Default;
        RegistryView wow64View = Environment.Is64BitOperatingSystem ? RegistryView.Registry32 : RegistryView.Default;

        switch (root.ToUpperInvariant())
        {
            case "HKCU":
            case "HKEY_CURRENT_USER":
                hive = RegistryHive.CurrentUser;
                view = nativeView;
                return true;
            case "HKLM":
            case "HKEY_LOCAL_MACHINE":
                hive = RegistryHive.LocalMachine;
                view = nativeView;
                return true;
            case "HKU":
            case "HKEY_USERS":
                hive = RegistryHive.Users;
                view = nativeView;
                return true;
            case "HKCR":
            case "HKEY_CLASSES_ROOT":
                hive = RegistryHive.ClassesRoot;
                view = nativeView;
                return true;
            case "HKCU32":
                hive = RegistryHive.CurrentUser;
                view = wow64View;
                return true;
            case "HKLM32":
                hive = RegistryHive.LocalMachine;
                view = wow64View;
                return true;
            case "HKCR32":
                hive = RegistryHive.ClassesRoot;
                view = wow64View;
                return true;
            default:
                return false;
        }
    }

    private static int TargetDepth(string location) => location.Count(c => c == '\\' || c == '/');

    private static string? NormalizeFileSystemTargetPath(string location)
    {
        try
        {
            string fullPath = Path.GetFullPath(location);
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

    private static bool TryGetProtectedCleanupReason(CleanupTarget target, out string reason)
    {
        reason = "";
        if (target.Type is not (CleanupTargetType.File or CleanupTargetType.Directory))
            return false;

        string? path = NormalizeFileSystemTargetPath(target.Location);
        if (path == null)
            return false;

        if (IsNtfsMetadataPath(path))
        {
            reason = "Protected NTFS metadata path skipped.";
            return true;
        }

        foreach (string protectedRoot in GetProtectedFileSystemRoots())
        {
            if (!IsSameOrChildPath(path, protectedRoot))
                continue;

            reason = "Protected path skipped: " + protectedRoot;
            return true;
        }

        return false;
    }

    private static bool IsNtfsMetadataPath(string path)
    {
        string p = path.Replace('/', '\\');
        return p.EndsWith(":\\$MFT", StringComparison.OrdinalIgnoreCase) ||
               p.EndsWith(":\\$LogFile", StringComparison.OrdinalIgnoreCase) ||
               p.Contains(":\\$Extend\\", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> GetProtectedFileSystemRoots()
    {
        static string? NormalizeRoot(string? path) =>
            string.IsNullOrWhiteSpace(path) ? null : NormalizeFileSystemTargetPath(path);

        var roots = new[]
        {
            NormalizeRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)),
            NormalizeRoot(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)),
            NormalizeRoot(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)),
            NormalizeRoot(Environment.GetEnvironmentVariable("ProgramW6432")),
            NormalizeRoot(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Package Cache")),
            NormalizeRoot(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows Defender")),
            NormalizeRoot(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Search"))
        };

        return roots
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => path!);
    }

    // ==================== LOGGING ====================
    private static string NewSessionId() => DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N")[..8];

    private static void AppendLog(string? logPath, string eventName, string sessionId, string targetType, string location, string status, string message)
    {
        using var logger = CleanupLogWriter.Open(logPath);
        logger.Write(eventName, sessionId, targetType, location, status, message);
    }

    private static string FormatLogLine(string eventName, string sessionId, string targetType, string location, string status, string message) =>
        $"{DateTimeOffset.Now:O} | event={CleanLogValue(eventName)}" +
        $" | session={CleanLogValue(sessionId)}" +
        $" | targetType={CleanLogValue(targetType)}" +
        $" | status={CleanLogValue(status)}" +
        $" | location={CleanLogValue(location)}" +
        $" | message={CleanLogValue(message)}";

    private sealed class CleanupLogWriter : IDisposable
    {
        private readonly StreamWriter? _writer;

        private CleanupLogWriter(StreamWriter? writer)
        {
            _writer = writer;
        }

        public static CleanupLogWriter Open(string? logPath)
        {
            if (string.IsNullOrWhiteSpace(logPath))
                return new CleanupLogWriter(null);

            try
            {
                string fullPath = Path.GetFullPath(logPath);
                string? dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);

                var stream = new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                return new CleanupLogWriter(new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true });
            }
            catch
            {
                return new CleanupLogWriter(null);
            }
        }

        public void Write(string eventName, string sessionId, string targetType, string location, string status, string message)
        {
            if (_writer == null)
                return;

            try
            {
                _writer.WriteLine(FormatLogLine(eventName, sessionId, targetType, location, status, message));
            }
            catch
            {
                // Cleanup must not fail just because logging failed.
            }
        }

        public void Dispose()
        {
            try { _writer?.Dispose(); } catch { }
        }
    }

    private static string CleanLogValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal);
    }

    private static string? ExtractLogSession(string line)
    {
        const string marker = "session=";
        int start = line.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
            return null;

        start += marker.Length;
        int end = line.IndexOf(" |", start, StringComparison.Ordinal);
        if (end < 0)
            end = line.Length;

        string value = line[start..end].Trim();
        return value.Length == 0 ? null : value;
    }

    private static void TrackPendingRebootDeletion(string? logPath, CleanupLogWriter logger, string sessionId, CleanupItem item)
    {
        if (string.IsNullOrWhiteSpace(logPath))
            return;

        try
        {
            var pending = new PendingRebootDeletion
            {
                SessionId = sessionId,
                TargetType = item.TargetType,
                Location = item.Location,
                ScheduledUtc = DateTimeOffset.UtcNow
            };

            File.AppendAllText(PendingRebootPath(logPath), JsonSerializer.Serialize(pending) + Environment.NewLine, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            logger.Write("PENDING-REBOOT-TRACK-ERROR", sessionId, item.TargetType, item.Location, Failed, ex.Message);
        }
    }

    private static string PendingRebootPath(string logPath) => Path.Combine(Path.GetDirectoryName(Path.GetFullPath(logPath)) ?? "", "log.pending-reboot.jsonl");

    private static bool PendingTargetExists(PendingRebootDeletion pending)
    {
        try
        {
            return pending.TargetType switch
            {
                nameof(CleanupTargetType.File) => File.Exists(pending.Location),
                nameof(CleanupTargetType.Directory) => Directory.Exists(pending.Location),
                _ => false
            };
        }
        catch
        {
            return true;
        }
    }

    // ==================== HANDLE CLOSING ====================
    private static HashSet<string> BuildTargetPathSet(string path)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            NormalizePathForComparison(path)
        };

        using SafeFileHandle handle = CreateFileW(
            path,
            FILE_READ_ATTRIBUTES,
            FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_FLAG_BACKUP_SEMANTICS,
            IntPtr.Zero);

        if (!handle.IsInvalid)
        {
            foreach (string finalPath in TryGetFinalPathsForHandle(handle.DangerousGetHandle()))
                paths.Add(finalPath);
        }

        return paths;
    }

    private static string NormalizePathForComparison(string path)
    {
        string normalized = Path.GetFullPath(path).TrimEnd('\\', '/');
        if (normalized.StartsWith(@"\\?\UNC\", StringComparison.Ordinal))
            normalized = @"\\" + normalized[8..];
        else if (normalized.StartsWith(@"\\?\", StringComparison.Ordinal))
            normalized = normalized[4..];
        else if (normalized.StartsWith(@"\??\UNC\", StringComparison.Ordinal))
            normalized = @"\\" + normalized[8..];
        else if (normalized.StartsWith(@"\??\", StringComparison.Ordinal))
            normalized = normalized[4..];

        return normalized.ToUpperInvariant();
    }

    private static bool CloseAllHandlesToPath(IReadOnlySet<string> targetPaths, out int closedCount, out string error)
    {
        closedCount = 0;
        error = "";
        IntPtr buffer = IntPtr.Zero;

        try
        {
            if (!TryQuerySystemHandles(out buffer, out ulong handleCount, out error))
                return false;

            int handleSize = Marshal.SizeOf<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>();
            IntPtr handlePtr = IntPtr.Add(buffer, IntPtr.Size * 2);
            IntPtr currentProcess = GetCurrentProcess();

            for (ulong i = 0; i < handleCount; i++)
            {
                var handle = Marshal.PtrToStructure<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>(handlePtr);
                handlePtr = IntPtr.Add(handlePtr, handleSize);

                uint processId = unchecked((uint)handle.UniqueProcessId.ToInt64());
                if (processId == 0 || handle.HandleValue == IntPtr.Zero)
                    continue;

                IntPtr processHandle = OpenProcess(PROCESS_DUP_HANDLE, false, processId);
                if (processHandle == IntPtr.Zero)
                    continue;

                IntPtr duplicate = IntPtr.Zero;
                try
                {
                    if (!DuplicateHandle(processHandle, handle.HandleValue, currentProcess, out duplicate, 0, false, DUPLICATE_SAME_ACCESS) ||
                        duplicate == IntPtr.Zero)
                    {
                        continue;
                    }

                    var handlePaths = TryGetFinalPathsForHandle(duplicate);
                    if (!handlePaths.Any(targetPaths.Contains))
                        continue;

                    CloseHandle(duplicate);
                    duplicate = IntPtr.Zero;

                    if (DuplicateHandle(processHandle, handle.HandleValue, currentProcess, out IntPtr closedDuplicate, 0, false,
                            DUPLICATE_CLOSE_SOURCE | DUPLICATE_SAME_ACCESS))
                    {
                        if (closedDuplicate != IntPtr.Zero)
                            CloseHandle(closedDuplicate);

                        closedCount++;
                    }
                }
                finally
                {
                    if (duplicate != IntPtr.Zero)
                        CloseHandle(duplicate);

                    CloseHandle(processHandle);
                }
            }

            return closedCount > 0;
        }
        catch (Exception ex)
        {
            error = ex.GetType().Name + ": " + ex.Message;
            return closedCount > 0;
        }
        finally
        {
            if (buffer != IntPtr.Zero)
                Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool TryQuerySystemHandles(out IntPtr buffer, out ulong handleCount, out string error)
    {
        buffer = IntPtr.Zero;
        handleCount = 0;
        error = "";
        uint bufferSize = 0x10000;

        for (int attempt = 0; attempt < 16; attempt++)
        {
            buffer = Marshal.AllocHGlobal((int)bufferSize);
            int status = NtQuerySystemInformation(SystemExtendedHandleInformation, buffer, bufferSize, out uint returnLength);
            if (status == STATUS_SUCCESS)
            {
                handleCount = ReadUIntPtr(buffer);
                return true;
            }

            Marshal.FreeHGlobal(buffer);
            buffer = IntPtr.Zero;

            if (!IsBufferTooSmallStatus(status))
            {
                error = "NtQuerySystemInformation failed with NTSTATUS 0x" + status.ToString("X8") + ".";
                return false;
            }

            long nextSize = returnLength > bufferSize ? (long)returnLength + 0x1000 : bufferSize * 2L;
            if (nextSize <= bufferSize || nextSize > MaxHandleInfoBufferBytes)
            {
                error = "System handle table is too large to query.";
                return false;
            }

            bufferSize = (uint)nextSize;
        }

        error = "System handle table kept growing while being queried.";
        return false;
    }

    private static IReadOnlyList<string> TryGetFinalPathsForHandle(IntPtr handle)
    {
        if (GetFileType(handle) != FILE_TYPE_DISK)
            return Array.Empty<string>();

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (uint flags in FinalPathFlags)
        {
            string? path = TryGetFinalPathForHandle(handle, flags);
            if (!string.IsNullOrWhiteSpace(path))
                paths.Add(path);
        }

        return paths.ToArray();
    }

    private static string? TryGetFinalPathForHandle(IntPtr handle, uint flags)
    {
        int capacity = 1024;
        for (int attempt = 0; attempt < 4; attempt++)
        {
            var path = new StringBuilder(capacity);
            uint length = GetFinalPathNameByHandleW(handle, path, (uint)path.Capacity, flags);
            if (length == 0)
                return null;

            if (length < path.Capacity)
                return NormalizePathForComparison(path.ToString());

            capacity = Math.Min(32768, checked((int)length + 1));
        }

        return null;
    }

    private static bool IsBufferTooSmallStatus(int status) =>
        status is STATUS_INFO_LENGTH_MISMATCH or STATUS_BUFFER_OVERFLOW or STATUS_BUFFER_TOO_SMALL;

    private static ulong ReadUIntPtr(IntPtr ptr) =>
        IntPtr.Size == 8 ? unchecked((ulong)Marshal.ReadInt64(ptr)) : unchecked((uint)Marshal.ReadInt32(ptr));

    // ==================== P/INVOKE ====================
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool MoveFileExW(string existingFileName, string? newFileName, int flags);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LookupPrivilegeValue(string? lpSystemName, string lpName, out LUID lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges,
        ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("ntdll.dll")]
    private static extern int NtQuerySystemInformation(uint SystemInformationClass,
        IntPtr SystemInformation, uint SystemInformationLength, out uint ReturnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, IntPtr hSourceHandle,
        IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle, uint dwDesiredAccess,
        bool bInheritHandle, uint dwOptions);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetFileType(IntPtr hFile);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint GetFinalPathNameByHandleW(IntPtr hFile, StringBuilder lpszFilePath, uint cchFilePath, uint dwFlags);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFileW(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
        IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    private const int STATUS_SUCCESS = 0;
    private const int STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004);
    private const int STATUS_BUFFER_OVERFLOW = unchecked((int)0x80000005);
    private const int STATUS_BUFFER_TOO_SMALL = unchecked((int)0xC0000023);
    private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private const uint TOKEN_QUERY = 0x0008;
    private const uint SE_PRIVILEGE_ENABLED = 0x2;
    private const uint PROCESS_DUP_HANDLE = 0x0040;
    private const uint DUPLICATE_CLOSE_SOURCE = 0x00000001;
    private const uint DUPLICATE_SAME_ACCESS = 0x00000002;
    private const uint SystemExtendedHandleInformation = 64;
    private const uint FILE_TYPE_DISK = 0x0001;
    private const uint FILE_READ_ATTRIBUTES = 0x00000080;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint FILE_SHARE_DELETE = 0x00000004;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    private const uint FILE_NAME_NORMALIZED = 0x00000000;
    private const uint FILE_NAME_OPENED = 0x00000008;
    private const uint VOLUME_NAME_DOS = 0x00000000;
    private const uint VOLUME_NAME_GUID = 0x00000001;
    private const uint VOLUME_NAME_NT = 0x00000002;
    private const uint MaxHandleInfoBufferBytes = 256 * 1024 * 1024;
    private static readonly uint[] FinalPathFlags =
    [
        FILE_NAME_NORMALIZED | VOLUME_NAME_DOS,
        FILE_NAME_OPENED | VOLUME_NAME_DOS,
        FILE_NAME_NORMALIZED | VOLUME_NAME_GUID,
        FILE_NAME_OPENED | VOLUME_NAME_GUID,
        FILE_NAME_NORMALIZED | VOLUME_NAME_NT,
        FILE_NAME_OPENED | VOLUME_NAME_NT
    ];

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID Luid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
    {
        public IntPtr Object;
        public IntPtr UniqueProcessId;
        public IntPtr HandleValue;
        public uint GrantedAccess;
        public ushort CreatorBackTraceIndex;
        public ushort ObjectTypeIndex;
        public uint HandleAttributes;
        public uint Reserved;
    }

    private static bool EnablePrivilege(string privilege)
    {
        if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr hToken))
            return false;

        try
        {
            if (!LookupPrivilegeValue(null, privilege, out LUID luid))
                return false;

            TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = SE_PRIVILEGE_ENABLED
            };

            bool result = AdjustTokenPrivileges(hToken, false, ref tp, (uint)Marshal.SizeOf(tp), IntPtr.Zero, IntPtr.Zero);
            return result && Marshal.GetLastWin32Error() == 0;
        }
        finally
        {
            CloseHandle(hToken);
        }
    }

    private enum CleanupTargetType
    {
        File,
        Directory,
        RegistryKey,
        RegistryValue
    }

    private sealed class CleanupTarget
    {
        public CleanupTarget()
        {
        }

        public CleanupTarget(CleanupTargetType type, string location)
        {
            Type = type;
            Location = location;
        }

        public CleanupTargetType Type { get; set; }
        public string Location { get; set; } = "";
    }

    private sealed class PendingRebootDeletion
    {
        public string SessionId { get; set; } = "";
        public string TargetType { get; set; } = "";
        public string Location { get; set; } = "";
        public DateTimeOffset ScheduledUtc { get; set; }
    }
}
