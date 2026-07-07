using Scanner.Core;

namespace Loader;

internal static class Loader
{
    private static async Task<int> Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("This tool only runs on Windows.");
            return 1;
        }

        if (args.Any(arg => arg.Equals("--help", StringComparison.OrdinalIgnoreCase)
            || arg.Equals("-h", StringComparison.OrdinalIgnoreCase)
            || arg.Equals("/?", StringComparison.OrdinalIgnoreCase)))
        {
            PrintUsage();
            return 0;
        }

        try
        {
            var options = ParseArgs(args);

            if (args.Any(arg => arg.Equals("--self-check", StringComparison.OrdinalIgnoreCase)))
            {
                await StartupSelfCheck.RunAsync().ConfigureAwait(false);
                return 0;
            }

            if (!options.SkipSelfCheck)
                await StartupSelfCheck.RunAsync().ConfigureAwait(false);

            if (options.Keywords.Count == 0)
                PromptForScan(options);

            if (options.Keywords.Count == 0)
            {
                Console.Error.WriteLine("At least one keyword is required.");
                PrintUsage();
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

    private static ScanOptions ParseArgs(string[] args)
    {
        var options = new ScanOptions
        {
            DeleteFound = false,
            DeleteWithoutPrompt = false
        };

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg.Equals("--kw", StringComparison.OrdinalIgnoreCase)
                || arg.Equals("--keyword", StringComparison.OrdinalIgnoreCase))
            {
                options.Keywords.Add(RequireValue(args, ref i, arg));
            }
            else if (arg.Equals("--root", StringComparison.OrdinalIgnoreCase))
            {
                options.Roots.Add(RequireValue(args, ref i, arg));
            }
            else if (arg.Equals("--delete", StringComparison.OrdinalIgnoreCase))
            {
                options.DeleteFound = true;
            }
            else if (arg.Equals("--yes", StringComparison.OrdinalIgnoreCase))
            {
                options.DeleteWithoutPrompt = true;
            }
            else if (arg.Equals("--skip-elevation", StringComparison.OrdinalIgnoreCase))
            {
                options.SkipElevation = true;
            }
            else if (arg.Equals("--skip-self-check", StringComparison.OrdinalIgnoreCase))
            {
                options.SkipSelfCheck = true;
            }
            else if (arg.Equals("--self-check", StringComparison.OrdinalIgnoreCase))
            {
                options.SkipSelfCheck = true;
            }
            else if (arg.Equals("--skip-registry", StringComparison.OrdinalIgnoreCase))
            {
                options.SkipRegistry = true;
            }
            else if (arg.Equals("--full-registry-scan", StringComparison.OrdinalIgnoreCase))
            {
                options.FullRegistryScan = true;
            }
            else if (arg.Equals("--file-workers", StringComparison.OrdinalIgnoreCase))
            {
                options.FileReadWorkers = ReadPositiveInt(args, ref i, arg);
            }
            else if (arg.Equals("--enum-workers", StringComparison.OrdinalIgnoreCase))
            {
                options.DirectoryEnumWorkers = ReadPositiveInt(args, ref i, arg);
            }
            else if (arg.Equals("--reads-per-volume", StringComparison.OrdinalIgnoreCase))
            {
                options.MaxReadsPerVolume = ReadPositiveInt(args, ref i, arg);
            }
            else if (arg.Equals("--parser-workers", StringComparison.OrdinalIgnoreCase))
            {
                options.ParserWorkers = ReadPositiveInt(args, ref i, arg);
            }
            else if (arg.Equals("--registry-workers", StringComparison.OrdinalIgnoreCase))
            {
                options.RegistryWorkers = ReadPositiveInt(args, ref i, arg);
            }
            else if (arg.Equals("--buffer-mb", StringComparison.OrdinalIgnoreCase))
            {
                int megabytes = ReadPositiveInt(args, ref i, arg);
                options.ReadBufferBytes = Math.Min(megabytes, 64) * 1024 * 1024;
            }
            else if (arg.Equals("--deep-content-scan", StringComparison.OrdinalIgnoreCase))
            {
                options.DeepContentScan = true;
            }
            else if (arg.Equals("--max-content-mb", StringComparison.OrdinalIgnoreCase))
            {
                int megabytes = ReadPositiveInt(args, ref i, arg);
                options.MaxContentScanBytes = Math.Min(megabytes, 16384L) * 1024L * 1024L;
            }
            else if (!arg.StartsWith("-", StringComparison.Ordinal))
            {
                options.Keywords.Add(arg);
            }
            else
            {
                throw new InvalidOperationException("Unknown option: " + arg);
            }
        }

        return options;
    }

    private static string RequireValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
            throw new InvalidOperationException(option + " requires a value.");

        return args[++index];
    }

    private static int ReadPositiveInt(string[] args, ref int index, string option)
    {
        string value = RequireValue(args, ref index, option);
        if (!int.TryParse(value, out int parsed) || parsed <= 0)
            throw new InvalidOperationException(option + " requires a positive integer.");

        return parsed;
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
            DeleteFindings(payload, targetCount, options.DeleteWithoutPrompt);
    }

    private static void DeleteFindings(ScanPayload payload, int targetCount, bool deleteWithoutPrompt)
    {
        if (targetCount == 0)
            return;

        if (!deleteWithoutPrompt)
        {
            Console.WriteLine();
            Console.Write($"Type DELETE to delete {targetCount} target(s): ");
            string? confirmation = Console.ReadLine();
            if (!string.Equals(confirmation, "DELETE", StringComparison.Ordinal))
            {
                Console.WriteLine("Delete cancelled.");
                return;
            }
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

    private static void PrintUsage()
    {
        Console.WriteLine("Loader Scanner");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  loader.exe --kw <keyword> [--delete] [--yes]");
        Console.WriteLine("  loader.exe <keyword>");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --kw, --keyword <value>     Keyword to scan for. Can be used more than once.");
        Console.WriteLine("  --root <path>               Scan an explicit file/folder root instead of all fixed drives.");
        Console.WriteLine("  --delete                    Delete cleanup targets after review confirmation.");
        Console.WriteLine("  --yes                       Skip DELETE confirmation when used with --delete.");
        Console.WriteLine("  --skip-elevation            Do not request scanner privileges.");
        Console.WriteLine("  --self-check                Run startup checks only, then exit.");
        Console.WriteLine("  --skip-self-check           Start without running the startup self-check.");
        Console.WriteLine("  --skip-registry             Skip registry scanning.");
        Console.WriteLine("  --full-registry-scan        Scan whole registry hives instead of targeted artifact keys.");
        Console.WriteLine("  --file-workers <count>      Override file read worker count.");
        Console.WriteLine("  --enum-workers <count>      Override directory enumeration worker count.");
        Console.WriteLine("  --reads-per-volume <count>  Override concurrent reads per volume.");
        Console.WriteLine("  --parser-workers <count>    Override artifact parser worker count.");
        Console.WriteLine("  --registry-workers <count>  Override registry root worker count.");
        Console.WriteLine("  --buffer-mb <count>         Override per-reader buffer size in MiB.");
        Console.WriteLine("  --max-content-mb <count>    Smart-mode max raw content bytes per file in MiB. Default: 256.");
        Console.WriteLine("  --deep-content-scan         Read raw content from every file like the older scanner path.");
        Console.WriteLine("  --help                      Show this help.");
        Console.WriteLine();
        Console.WriteLine("Running without arguments starts an interactive command-prompt flow.");
    }
}
