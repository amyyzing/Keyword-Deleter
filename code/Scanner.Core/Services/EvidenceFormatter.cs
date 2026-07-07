namespace Scanner.Core;

internal static class EvidenceFormatter
{
    public static string Text(string text, int index, int matchLength)
    {
        const int radius = 90;
        int start = Math.Max(0, index - radius);
        int end = Math.Min(text.Length, index + matchLength + radius);
        string snippet = text[start..end];
        if (start > 0) snippet = "..." + snippet;
        if (end < text.Length) snippet += "...";
        return "text=\"" + Clean(snippet, 240) + "\"";
    }

    public static string Binary(ReadOnlySpan<byte> data, int dataLength, ByteSearchHit hit, long chunkOffset = 0)
    {
        const int radius = 80;
        int safeLength = Math.Clamp(dataLength, 0, data.Length);
        if (safeLength == 0)
            return $"offset=0x{chunkOffset + hit.Offset:X}; encoding={hit.Kind}; preview=\"\"";

        int safeOffset = Math.Clamp(hit.Offset, 0, Math.Max(0, safeLength - 1));
        int start = Math.Max(0, safeOffset - radius);
        int length = Math.Min(safeLength - start, hit.MatchByteLength + radius * 2);
        var slice = data.Slice(start, length);

        string preview = hit.Kind switch
        {
            BytePatternKind.Utf16LittleEndian => DecodeBinaryPreview(slice, Encoding.Unicode),
            BytePatternKind.Utf16BigEndian => DecodeBinaryPreview(slice, Encoding.BigEndianUnicode),
            _ => ByteMatcher.BinaryPreview(slice, length)
        };

        long absoluteOffset = chunkOffset + hit.Offset;
        return $"offset=0x{absoluteOffset:X}; encoding={hit.Kind}; preview=\"" + Clean(preview, 240) + "\"";
    }

    private static string DecodeBinaryPreview(ReadOnlySpan<byte> data, Encoding encoding)
    {
        int length = data.Length;
        if ((length & 1) != 0)
            length--;

        if (length <= 0)
            return "";

        try { return encoding.GetString(data[..length]); }
        catch { return ByteMatcher.BinaryPreview(data[..Math.Max(0, length)], Math.Max(0, length)); }
    }

    private static string Clean(string text, int max)
    {
        var sb = new StringBuilder(Math.Min(text.Length, max));
        bool lastSpace = false;
        foreach (char c in text)
        {
            if (sb.Length >= max) break;
            char outChar = char.IsControl(c) ? ' ' : c;
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

        return sb.ToString().Trim().Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
