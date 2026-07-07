namespace Scanner.Core;

internal readonly struct ByteSearchHit
{
    public PreparedKeyword Keyword { get; }
    public int Offset { get; }
    public BytePatternKind Kind { get; }
    public int MatchByteLength { get; }

    public ByteSearchHit(PreparedKeyword keyword, int offset, BytePatternKind kind, int matchByteLength)
    {
        Keyword = keyword;
        Offset = offset;
        Kind = kind;
        MatchByteLength = matchByteLength;
    }
}
