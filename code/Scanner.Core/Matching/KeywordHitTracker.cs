namespace Scanner.Core;

internal sealed class KeywordHitTracker
{
    private readonly int[] _seenEpochs;
    private int _epoch;

    public KeywordHitTracker(int keywordCount)
    {
        _seenEpochs = new int[Math.Max(0, keywordCount)];
        _epoch = 1;
    }

    public void Reset()
    {
        if (_seenEpochs.Length == 0)
            return;

        _epoch++;
        if (_epoch != int.MaxValue)
            return;

        Array.Clear(_seenEpochs, 0, _seenEpochs.Length);
        _epoch = 1;
    }

    public bool MarkIfNew(int keywordIndex)
    {
        if ((uint)keywordIndex >= (uint)_seenEpochs.Length)
            return false;

        if (_seenEpochs[keywordIndex] == _epoch)
            return false;

        _seenEpochs[keywordIndex] = _epoch;
        return true;
    }
}
