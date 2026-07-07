namespace Scanner.Core;

internal readonly struct TextSearchHit
{
    public PreparedKeyword Keyword { get; }
    public int Index { get; }
    public int MatchLength { get; }

    public TextSearchHit(PreparedKeyword keyword, int index, int matchLength)
    {
        Keyword = keyword;
        Index = index;
        MatchLength = matchLength;
    }
}
