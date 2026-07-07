namespace Scanner.Core;

internal sealed class ParsedArtifact
{
    public string Kind { get; set; } = "";
    public string Summary { get; set; } = "";
    public List<KeyValuePair<string, string>> Metadata { get; } = new();
    public List<string> SearchableText { get; } = new();

    public ParsedArtifact(string kind, string summary) { Kind = kind; Summary = summary; }

    public void Add(string key, object? value)
    {
        if (value == null) return;
        string text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
        if (string.IsNullOrWhiteSpace(text)) return;
        if (text.Length > 4096) text = text[..4096] + "...";
        Metadata.Add(new KeyValuePair<string, string>(key, text));
    }

    public void AddText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        text = CleanText(text, 12000);
        if (text.Length == 0) return;
        SearchableText.Add(text);
    }

    public static string CleanText(string text, int max)
    {
        var sb = new StringBuilder(Math.Min(text.Length, max));
        bool lastSpace = false;
        foreach (char c in text)
        {
            if (sb.Length >= max) break;
            if (char.IsControl(c) && c != '\r' && c != '\n' && c != '\t') continue;
            char outChar = (c == '\r' || c == '\n' || c == '\t') ? ' ' : c;
            bool isSpace = char.IsWhiteSpace(outChar);
            if (isSpace)
            {
                if (lastSpace) continue;
                sb.Append(' ');
                lastSpace = true;
            }
            else
            {
                sb.Append(outChar);
                lastSpace = false;
            }
        }
        return sb.ToString().Trim();
    }
}
