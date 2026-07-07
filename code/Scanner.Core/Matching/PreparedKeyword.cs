namespace Scanner.Core;

internal sealed class PreparedKeyword
{
    public int Index { get; init; }
    public string Text { get; init; } = "";
    public string UpperText { get; init; } = "";
    public bool IsAscii { get; init; }
    public byte[] UpperUtf8 { get; init; } = Array.Empty<byte>();
    public static PreparedKeyword Prepare(string text, int index) => new() { Index = index, Text = text, UpperText = text.ToUpperInvariant(), IsAscii = text.All(c => c <= 127), UpperUtf8 = Encoding.UTF8.GetBytes(text.ToUpperInvariant()) };
}
