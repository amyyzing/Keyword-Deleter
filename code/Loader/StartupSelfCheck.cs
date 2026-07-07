using Microsoft.Win32;
using Scanner.Core;

namespace Loader;

internal static class StartupSelfCheck
{
    public static async Task RunAsync()
    {
        Console.WriteLine("Startup check...");

        var failures = new List<string>();
        var warnings = new List<string>();

        CheckRuntime(failures);
        CheckRegistry(warnings);
        await CheckScannerAndCleanupAsync(failures).ConfigureAwait(false);

        foreach (string warning in warnings)
            WriteStatus("WARN", warning, ConsoleColor.Yellow);

        if (failures.Count > 0)
        {
            foreach (string failure in failures)
                WriteStatus("FAIL", failure, ConsoleColor.Red);

            throw new InvalidOperationException("Startup check failed. The scanner did not start because a required check failed.");
        }

        WriteStatus("OK", "Scanner, temp file access, cleanup delete path, and basic registry access checked.", ConsoleColor.Green);
        Console.WriteLine();
    }

    private static void CheckRuntime(List<string> failures)
    {
        if (!Environment.Is64BitProcess)
            failures.Add("Process is not running as 64-bit.");

        try
        {
            string temp = Path.GetTempPath();
            if (string.IsNullOrWhiteSpace(temp) || !Directory.Exists(temp))
                failures.Add("Temp folder is not available.");
        }
        catch (Exception ex)
        {
            failures.Add("Temp folder check failed: " + ex.Message);
        }
    }

    private static void CheckRegistry(List<string> warnings)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey("Software", writable: false);
            if (key == null)
                warnings.Add("HKCU\\Software could not be opened. Registry scan may report fewer results.");
        }
        catch (Exception ex)
        {
            warnings.Add("Registry read check failed: " + ex.Message);
        }
    }

    private static async Task CheckScannerAndCleanupAsync(List<string> failures)
    {
        string root = Path.Combine(Path.GetTempPath(), "loader-self-check-" + Guid.NewGuid().ToString("N"));
        string keyword = "loader-self-check-" + Guid.NewGuid().ToString("N");
        string file = Path.Combine(root, keyword + ".txt");

        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(file, keyword);

            var options = new ScanOptions
            {
                SkipElevation = true,
                SkipRegistry = true,
                DirectoryEnumWorkers = 1,
                FileReadWorkers = 1,
                ParserWorkers = 1,
                MaxReadsPerVolume = 1,
                ReadBufferBytes = 64 * 1024
            };
            options.Keywords.Add(keyword);
            options.Roots.Add(root);

            var payload = await new ScannerService().RunAsync(options).ConfigureAwait(false);
            if (!payload.Findings.Any(f => f.Location.Equals(file, StringComparison.OrdinalIgnoreCase)))
            {
                failures.Add("Self-check file was not detected by the scanner.");
                return;
            }

            int cleanupTargets = FindingCleanupService.CountTargets(payload.Findings);
            if (cleanupTargets != 1)
            {
                failures.Add("Self-check finding did not produce exactly one cleanup target.");
                return;
            }

            var cleanup = FindingCleanupService.DeleteFindings(payload.Findings);
            if (cleanup.DeletedCount != 1 || File.Exists(file))
            {
                string details = cleanup.Items.Count > 0
                    ? string.Join("; ", cleanup.Items.Select(i => $"{i.Status}: {i.Message}"))
                    : "No cleanup item details.";

                failures.Add("Self-check cleanup could not delete its disposable test file. " + details);
            }
        }
        catch (Exception ex)
        {
            failures.Add("Scanner/delete self-check failed: " + ex.Message);
        }
        finally
        {
            try
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
            catch
            {
                // A leftover temp self-check folder is not worth blocking startup after the real check result is known.
            }
        }
    }

    private static void WriteStatus(string status, string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write($"[{status}] ");
        Console.ResetColor();
        Console.WriteLine(message);
    }
}
