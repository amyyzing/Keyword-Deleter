using Scanner.Core;

namespace Loader;

internal static class Loader
{
    private static async Task<int> Main()
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("This tool only runs on Windows.");
            return 1;
        }

        try
        {
            var options = CreateMaximumScanOptions();
            await StartupSelfCheck.RunAsync().ConfigureAwait(false);
            PromptForScan(options);

            if (options.Keywords.Count == 0)
            {
                Console.Error.WriteLine("At least one keyword is required.");
                return 1;
            }

            await RunScanAsync(options).ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("ERROR: " + ex.Message);
            Console.ResetColor();
            return 1;
        }
    }

    private static ScanOptions CreateMaximumScanOptions()
    {
        int processors = Math.Max(1, Environment.ProcessorCount);

        return new ScanOptions
        {
            DeleteFound = false,
            DeepContentScan = true,
            FullRegistryScan = true,
            LowImpact = false,
            DirectoryEnumWorkers = Math.Clamp(processors, 4, 16),
            FileReadWorkers = Math.Clamp(processors * 2, 16, 64),
            MaxReadsPerVolume = Math.Clamp(processors, 8, 24),
            ParserWorkers = Math.Clamp(processors, 2, 16),
            RegistryWorkers = 8,
            ReadBufferBytes = 1024 * 1024
        };
    }

    private static void PromptForScan(ScanOptions options)
    {
        Console.WriteLine("Loader Scanner");
        Console.WriteLine("Press Enter on optional prompts to use the default.");
        Console.WriteLine();

        if (ReadKeywordMode())
        {
            foreach (string keyword in ReadPromptKeywords())
                options.Keywords.Add(keyword);
        }
        else
        {
            string keyword = ReadText("Keyword", "");
            if (!string.IsNullOrWhiteSpace(keyword))
                options.Keywords.Add(keyword);
        }

        options.DeleteFound = ReadYesNo("Delete findings after scan", defaultValue: false);
    }

    private static async Task RunScanAsync(ScanOptions options)
    {
        Console.WriteLine();
        Console.WriteLine("Starting scan...");
        Console.WriteLine("Keywords: " + string.Join(", ", options.Keywords));
        if (options.Roots.Count > 0)
            Console.WriteLine("Roots: " + string.Join(", ", options.Roots));
        Console.WriteLine();

        var scanner = new ScannerService();
        int liveCount = 0;

        scanner.FindingFound += finding =>
        {
            liveCount++;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{liveCount}] {finding.Source} | {finding.Kind} | {finding.Keyword}");
            Console.ResetColor();
            Console.WriteLine("    " + finding.Location);
        };

        var payload = await scanner.RunAsync(options).ConfigureAwait(false);
        Console.WriteLine();
        Console.WriteLine($"Scan complete. Findings: {payload.Count}");

        int targetCount = FindingCleanupService.CountTargets(payload.Findings);
        Console.WriteLine("Cleanup targets: " + targetCount);

        if (options.DeleteFound)
            DeleteFindings(payload, targetCount);
    }

    private static void DeleteFindings(ScanPayload payload, int targetCount)
    {
        if (targetCount == 0)
            return;

        Console.WriteLine();
        Console.Write($"Type DELETE to delete {targetCount} target(s): ");
        string? confirmation = Console.ReadLine();
        if (!string.Equals(confirmation, "DELETE", StringComparison.Ordinal))
        {
            Console.WriteLine("Delete cancelled.");
            return;
        }

        var result = FindingCleanupService.DeleteFindings(payload.Findings);

        Console.WriteLine();
        Console.WriteLine("Delete result:");
        Console.WriteLine("  Deleted: " + result.DeletedCount);
        Console.WriteLine("  Scheduled for reboot: " + result.ScheduledForRebootCount);
        Console.WriteLine("  Skipped: " + result.SkippedCount);
        Console.WriteLine("  Failed: " + result.FailedCount);

        foreach (var item in result.Items)
        {
            Console.WriteLine($"  [{item.Status}] {item.TargetType}: {item.Location}");
            if (!string.IsNullOrWhiteSpace(item.Message))
                Console.WriteLine("      " + item.Message);
        }
    }

    private static IReadOnlyList<string> ReadPromptKeywords()
    {
        var keywords = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Console.WriteLine("Enter one keyword per line. Press Enter on a blank line when finished.");
        while (true)
        {
            string keyword = ReadText($"Keyword {keywords.Count + 1}", "");
            if (string.IsNullOrWhiteSpace(keyword))
                break;

            if (seen.Add(keyword))
                keywords.Add(keyword);
            else
                Console.WriteLine("That keyword is already in the list.");
        }

        return keywords;
    }

    private static bool ReadKeywordMode()
    {
        Console.WriteLine("Keyword mode:");
        Console.WriteLine("  [1] Single keyword");
        Console.WriteLine("  [2] Multiple keywords");

        while (true)
        {
            string value = ReadText("Choose keyword mode", "1");
            if (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("s", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("single", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (value.Equals("2", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("m", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("multiple", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            Console.WriteLine("Please choose 1 for single keyword or 2 for multiple keywords.");
        }
    }

    private static string ReadText(string prompt, string defaultValue)
    {
        string suffix = string.IsNullOrEmpty(defaultValue) ? "" : $" [{defaultValue}]";
        Console.Write(prompt + suffix + ": ");
        string? value = Console.ReadLine();
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
    }

    private static bool ReadYesNo(string prompt, bool defaultValue)
    {
        string suffix = defaultValue ? "Y/n" : "y/N";
        Console.Write($"{prompt} [{suffix}]: ");
        string? value = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return value.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase);
    }

}
