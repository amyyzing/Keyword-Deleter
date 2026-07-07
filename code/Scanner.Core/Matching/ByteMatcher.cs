namespace Scanner.Core;

internal static class ByteMatcher
{
    public static byte UpperAscii(byte b) => b >= 97 && b <= 122 ? (byte)(b - 32) : b;

    public static string BinaryPreview(ReadOnlySpan<byte> bytes, int max)
    {
        int len = Math.Min(bytes.Length, max);
        var sb = new StringBuilder(len);

        for (int i = 0; i < len; i++)
        {
            byte b = bytes[i];
            sb.Append(b is >= 32 and <= 126 ? (char)b : '.');
        }

        return sb.ToString();
    }

    public static string BinaryPreview(byte[] bytes, int max) =>
        BinaryPreview(bytes.AsSpan(), max);
}

// File Scanner with advanced locked file deletion and duplicate suppression
