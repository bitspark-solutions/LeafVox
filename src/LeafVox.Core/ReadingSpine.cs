namespace LeafVox.Core;

/// <summary>
/// The ordered, speakable projection of a document: a flat list of sentences
/// that the player walks forward and backward. This is LeafVox's central model.
/// </summary>
public sealed class ReadingSpine
{
    private readonly List<Sentence> _sentences;
    private readonly Dictionary<int, int> _firstSentenceOnPage;
    private readonly HashSet<int> _scannedPages;

    private ReadingSpine(List<Sentence> sentences, Dictionary<int, int> firstOnPage, HashSet<int> scannedPages, int pageCount)
    {
        _sentences = sentences;
        _firstSentenceOnPage = firstOnPage;
        _scannedPages = scannedPages;
        PageCount = pageCount;
    }

    public IReadOnlyList<Sentence> Sentences => _sentences;
    public int PageCount { get; }
    public IReadOnlySet<int> ScannedPages => _scannedPages;
    public bool PageIsScanned(int pageNumber) => _scannedPages.Contains(pageNumber);

    /// <summary>Parse an entire document into a spine. Do this off the UI thread.</summary>
    public static ReadingSpine Build(PdfBook book, IReadOnlyDictionary<int, IReadOnlyList<Line>>? lineOverrides = null)
    {
        var raw = new List<PageText>(book.NumberOfPages);
        for (int p = 1; p <= book.NumberOfPages; p++)
            raw.Add(book.ExtractPage(p));

        if (lineOverrides is not null)
            for (int i = 0; i < raw.Count; i++)
                if (lineOverrides.TryGetValue(raw[i].PageNumber, out var lines) && lines.Count > 0)
                    raw[i] = new PageText(raw[i].PageNumber, raw[i].Height, lines);

        var pages = HeaderFooterFilter.Apply(raw);

        var sentences = new List<Sentence>();
        var firstOnPage = new Dictionary<int, int>();
        var scanned = new HashSet<int>();

        foreach (var page in pages)
        {
            if (page.LooksScanned)
            {
                if (page.Lines.Count == 0) scanned.Add(page.PageNumber);
                else
                {
                    // tiny text but non-empty (title page): still speakable lines
                }
            }
            firstOnPage.TryAdd(page.PageNumber, sentences.Count);

            foreach (var s in SplitPage(page))
            {
                sentences.Add(new Sentence(sentences.Count, page.PageNumber, s.Text, s.Boxes));
            }
        }

        return new ReadingSpine(sentences, firstOnPage, scanned, book.NumberOfPages);
    }

    private static IEnumerable<(string Text, IReadOnlyList<Box> Boxes)> SplitPage(PageText page)
    {
        if (page.Lines.Count == 0) yield break;
        var texts = page.Lines.Select(l => l.Text).ToArray();
        var joined = SentenceBuilder.JoinLines(texts);
        var sents = SentenceBuilder.Split(joined);
        // Map sentences back to boxes by matching their source lines in order.
        int lineCursor = 0;
        foreach (var s in sents)
        {
            var boxes = new List<Box>();
            int consumed = 0;
            while (lineCursor < page.Lines.Count && consumed < s.Length)
            {
                var line = page.Lines[lineCursor];
                boxes.AddRange(line.Boxes);
                consumed += line.Text.Length + 1;
                lineCursor++;
            }
            yield return (s, boxes.Count > 0 ? boxes : page.Lines[0].Boxes);
        }
    }

    /// <summary>Builds a spine directly from sentence texts (test/preview seam).</summary>
    public static ReadingSpine FromSentences(IReadOnlyList<string> texts)
    {
        var sentences = texts
            .Select((t, i) => new Sentence(i, 1, t, new[] { new Box(50, 800 - 18.0 * i, 400, 12) }))
            .ToList();
        return new ReadingSpine(sentences, new Dictionary<int, int> { [1] = 0 }, new HashSet<int>(), 1);
    }

    /// <summary>First sentence index that belongs to the given page (or the last sentence if none).</summary>
    public int IndexForPage(int pageNumber)
    {
        if (_firstSentenceOnPage.TryGetValue(pageNumber, out var exact) && exact < _sentences.Count) return exact;
        for (int p = pageNumber; p <= PageCount; p++)
            if (_firstSentenceOnPage.TryGetValue(p, out var i) && i < _sentences.Count) return i;
        return Math.Max(0, _sentences.Count - 1);
    }

    /// <summary>
    /// Sentence index nearest to a click point on a page (Y in PDF units, bottom origin).
    /// Used by "start reading from here".
    /// </summary>
    public int IndexNearest(int pageNumber, double yPdf)
    {
        Sentence? best = null;
        double bestDist = double.MaxValue;
        foreach (var s in _sentences)
        {
            if (s.PageNumber != pageNumber) continue;
            foreach (var b in s.Boxes)
            {
                double d = b.ContainsY(yPdf) ? 0 : Math.Min(Math.Abs(yPdf - b.Top), Math.Abs(yPdf - b.Bottom));
                if (d < bestDist) { bestDist = d; best = s; }
            }
        }
        return best?.Index ?? IndexForPage(pageNumber);
    }

    public int ClampIndex(int index) => Math.Clamp(index, 0, Math.Max(0, _sentences.Count - 1));
}
