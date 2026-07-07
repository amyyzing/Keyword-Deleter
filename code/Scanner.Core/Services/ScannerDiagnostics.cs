namespace Scanner.Core;

internal static class ScannerDiagnostics
{
    public static event Action<string>? MessageLogged;

    public static void Error(string message)
    {
        try
        {
            MessageLogged?.Invoke(message);
        }
        catch
        {
            // Diagnostics must never interrupt scanning.
        }
    }
}
