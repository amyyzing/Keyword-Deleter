namespace Scanner.Core;

internal sealed class KeywordMatcher
{
    private readonly AhoCorasick _byteMatcher;
    private readonly AhoCorasick _textMatcher;
    private readonly SearchValues<byte>? _byteStartAnchors;

    public PreparedKeyword[] Keywords { get; }
    public bool HasBytePatterns => _byteMatcher.HasPatterns;
    public int ByteSearchableKeywordCount { get; }

    private KeywordMatcher(PreparedKeyword[] k, AhoCorasick bm, AhoCorasick tm, int byteSearchableKeywordCount, SearchValues<byte>? byteStartAnchors)
    {
        Keywords = k;
        _byteMatcher = bm;
        _textMatcher = tm;
        ByteSearchableKeywordCount = byteSearchableKeywordCount;
        _byteStartAnchors = byteStartAnchors;
    }

    public static KeywordMatcher Build(PreparedKeyword[] k)
    {
        var bm = new AhoCorasick();
        var tm = new AhoCorasick();
        var byteStartAnchors = new HashSet<byte>();
        int byteSearchableKeywordCount = 0;

        foreach (var kw in k)
        {
            if (kw.UpperText.Length > 0)
                tm.Add(kw.UpperText.Select(c => (int)c), new AhoOutput(kw, kw.UpperText.Length, BytePatternKind.AsciiOrUtf8));

            if (!kw.IsAscii)
                continue;

            byteSearchableKeywordCount++;
            bm.Add(kw.UpperUtf8.Select(b => (int)b), new AhoOutput(kw, kw.UpperUtf8.Length, BytePatternKind.AsciiOrUtf8));
            bm.Add(ToUtf16Pattern(kw.UpperUtf8, true), new AhoOutput(kw, kw.UpperUtf8.Length * 2, BytePatternKind.Utf16LittleEndian));
            bm.Add(ToUtf16Pattern(kw.UpperUtf8, false), new AhoOutput(kw, kw.UpperUtf8.Length * 2, BytePatternKind.Utf16BigEndian));

            if (kw.UpperUtf8.Length > 0)
            {
                AddAnchorVariants(byteStartAnchors, kw.UpperUtf8[0]);
                byteStartAnchors.Add(0);
            }
        }

        bm.Build(true);
        tm.Build(false);
        SearchValues<byte>? anchors = byteStartAnchors.Count is > 0 and < 192
            ? SearchValues.Create(byteStartAnchors.ToArray())
            : null;
        return new KeywordMatcher(k, bm, tm, byteSearchableKeywordCount, anchors);
    }

    public void SearchBytesUnique(
        ReadOnlyMemory<byte> data,
        ref int state,
        KeywordHitTracker keywordHits,
        ref int remainingKeywords,
        Func<ByteSearchHit, bool> onNewKeyword)
    {
        if (data.Length == 0 || remainingKeywords <= 0 || !_byteMatcher.HasPatterns)
            return;

        if (state == 0 && _byteStartAnchors != null && data.Span.IndexOfAny(_byteStartAnchors) < 0)
            return;

        _byteMatcher.SearchBytesUnique(data.Span, ref state, keywordHits, ref remainingKeywords, onNewKeyword);
    }

    public IEnumerable<TextSearchHit> SearchDecodedText(string text)
    {
        if (!_textMatcher.HasPatterns)
            yield break;

        foreach (var match in _textMatcher.SearchText(text))
            yield return new TextSearchHit(match.Output.Keyword, match.StartIndex, match.Output.PatternLength);
    }

    private static int[] ToUtf16Pattern(byte[] a, bool le)
    {
        var p = new int[a.Length * 2];
        for (int i = 0; i < a.Length; i++)
        {
            int pos = i * 2;
            if (le)
            {
                p[pos] = a[i];
                p[pos + 1] = 0;
            }
            else
            {
                p[pos] = 0;
                p[pos + 1] = a[i];
            }
        }
        return p;
    }

    private static void AddAnchorVariants(HashSet<byte> anchors, byte b)
    {
        byte upper = ByteMatcher.UpperAscii(b);
        anchors.Add(upper);

        if (upper is >= (byte)'A' and <= (byte)'Z')
            anchors.Add((byte)(upper + 32));
        else
            anchors.Add(b);
    }

    private sealed class AhoCorasick
    {
        private readonly List<Node> _nodes = new() { new Node() };
        private int[]? _byteTransitionsFlat;
        private int[]? _asciiTransitionsFlat;
        private bool _built;

        public bool HasPatterns { get; private set; }

        public void Add(IEnumerable<int> symbols, AhoOutput output)
        {
            if (_built) throw new InvalidOperationException();
            int node = 0;
            foreach (int s in symbols)
            {
                if (!_nodes[node].Next.TryGetValue(s, out int next))
                {
                    next = _nodes.Count;
                    _nodes[node].Next[s] = next;
                    _nodes.Add(new Node());
                }
                node = next;
            }
            _nodes[node].Outputs.Add(output);
            HasPatterns = true;
        }

        public void Build(bool fullByte)
        {
            if (_built) return;
            _built = true;

            var q = new Queue<int>();
            foreach (int child in _nodes[0].Next.Values)
            {
                _nodes[child].Fail = 0;
                q.Enqueue(child);
            }

            while (q.TryDequeue(out int cur))
            {
                foreach (var (sym, child) in _nodes[cur].Next)
                {
                    int fail = _nodes[cur].Fail;
                    while (fail != 0 && !_nodes[fail].Next.ContainsKey(sym))
                        fail = _nodes[fail].Fail;

                    _nodes[child].Fail = _nodes[fail].Next.TryGetValue(sym, out int ft) && ft != child ? ft : 0;

                    if (_nodes[_nodes[child].Fail].Outputs.Count > 0)
                        _nodes[child].Outputs.AddRange(_nodes[_nodes[child].Fail].Outputs);

                    q.Enqueue(child);
                }
            }

            if (fullByte)
                BuildByteTransitions();
            else
                BuildAsciiTransitions();
        }

        private void BuildByteTransitions()
        {
            _byteTransitionsFlat = new int[_nodes.Count * 256];

            for (int i = 0; i < _nodes.Count; i++)
            {
                int row = i << 8;
                for (int b = 0; b < 256; b++)
                {
                    int folded = ByteMatcher.UpperAscii((byte)b);
                    int state = i;

                    while (state != 0 && !_nodes[state].Next.ContainsKey(folded))
                        state = _nodes[state].Fail;

                    _byteTransitionsFlat[row + b] = _nodes[state].Next.TryGetValue(folded, out int n) ? n : 0;
                }
            }
        }

        private void BuildAsciiTransitions()
        {
            _asciiTransitionsFlat = new int[_nodes.Count * 128];

            for (int i = 0; i < _nodes.Count; i++)
            {
                int row = i << 7;
                for (int c = 0; c < 128; c++)
                {
                    int folded = c is >= 'a' and <= 'z' ? c - 32 : c;
                    int state = i;

                    while (state != 0 && !_nodes[state].Next.ContainsKey(folded))
                        state = _nodes[state].Fail;

                    _asciiTransitionsFlat[row + c] = _nodes[state].Next.TryGetValue(folded, out int n) ? n : 0;
                }
            }
        }

        public void SearchBytesUnique(
            ReadOnlySpan<byte> data,
            ref int state,
            KeywordHitTracker keywordHits,
            ref int remainingKeywords,
            Func<ByteSearchHit, bool> onNewKeyword)
        {
            var transitions = _byteTransitionsFlat;
            if (transitions == null || remainingKeywords <= 0) return;

            for (int i = 0; i < data.Length && remainingKeywords > 0; i++)
            {
                state = transitions[(state << 8) + data[i]];
                var outputs = _nodes[state].Outputs;

                for (int j = 0; j < outputs.Count; j++)
                {
                    var o = outputs[j];
                    int idx = o.Keyword.Index;
                    if (!keywordHits.MarkIfNew(idx))
                        continue;

                    remainingKeywords--;

                    var hit = new ByteSearchHit(o.Keyword, i - o.PatternLength + 1, o.ByteKind, o.PatternLength);
                    if (!onNewKeyword(hit))
                        return;

                    if (remainingKeywords <= 0)
                        return;
                }
            }
        }

        public IEnumerable<AhoMatch> SearchText(string text)
        {
            var asciiTransitions = _asciiTransitionsFlat;
            int state = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c < 128 && asciiTransitions != null)
                {
                    state = asciiTransitions[(state << 7) + c];
                }
                else
                {
                    int sym = char.ToUpperInvariant(c);
                    while (state != 0 && !_nodes[state].Next.ContainsKey(sym))
                        state = _nodes[state].Fail;

                    state = _nodes[state].Next.TryGetValue(sym, out int next) ? next : 0;
                }

                var outputs = _nodes[state].Outputs;
                for (int j = 0; j < outputs.Count; j++)
                {
                    var output = outputs[j];
                    yield return new AhoMatch(output, i - output.PatternLength + 1);
                }
            }
        }

        private sealed class Node
        {
            public Dictionary<int, int> Next { get; } = new();
            public int Fail { get; set; }
            public List<AhoOutput> Outputs { get; } = new();
        }
    }

    private readonly struct AhoOutput
    {
        public PreparedKeyword Keyword { get; }
        public int PatternLength { get; }
        public BytePatternKind ByteKind { get; }

        public AhoOutput(PreparedKeyword keyword, int patternLength, BytePatternKind byteKind)
        {
            Keyword = keyword;
            PatternLength = patternLength;
            ByteKind = byteKind;
        }
    }

    private readonly struct AhoMatch
    {
        public AhoOutput Output { get; }
        public int StartIndex { get; }

        public AhoMatch(AhoOutput output, int startIndex)
        {
            Output = output;
            StartIndex = startIndex;
        }
    }
}
