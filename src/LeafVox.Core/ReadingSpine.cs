using System.Text;

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

            var pageSentences = AssembleSentences(page);
            int idx = sentences.Count;
            foreach (var s in pageSentences)
            {
                sentences.Add(new Sentence(idx++, page.PageNumber, s.Text, s.Boxes, s.Words, s.Lines));
            }
        }

        return new ReadingSpine(sentences, firstOnPage, scanned, book.NumberOfPages);
    }

    private const int MaxSentenceWords = 400; // safety: never speak a 10-minute "sentence"

    /// <summary>
    /// Assembles a page's visual lines into speakable sentences. Wrapped lines flow into
    /// one sentence until a line ends with <c>.!?</c>. Hyphenation across lines
    /// (<c>read-\ning</c>) is rejoined as one word. Each returned sentence carries every
    /// word box that belongs to it for highlighting.
    /// </summary>
    private static List<(string Text, List<Box> Boxes, List<Word> Words, List<Line> Lines)> AssembleSentences(PageText page)
    {
        var result = new List<(string, List<Box>, List<Word>, List<Line>)>();
        var currentLines = new List<Line>();
        var currentWords = new List<Word>();
        var currentBoxes = new List<Box>();
        var currentText = new StringBuilder();

        void Flush()
        {
            if (currentLines.Count == 0) return;
            result.Add((currentText.ToString().Trim(),
                new List<Box>(currentBoxes),
                new List<Word>(currentWords),
                new List<Line>(currentLines)));
            currentLines.Clear();
            currentWords.Clear();
            currentBoxes.Clear();
            currentText.Clear();
        }

        foreach (var line in page.Lines)
        {
            if (currentLines.Count > 0)
            {
                var prev = currentText.ToString().TrimEnd();
                if (EndsSentence(prev) || currentText.Length > 0 && currentText.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= MaxSentenceWords)
                {
                    Flush();
                    currentText.Append(line.Text);
                }
                else if (prev.EndsWith('-'))
                {
                    // Hyphenation join across lines: drop the hyphen, append directly.
                    currentText.Length -= 1;
                    currentText.Append(line.Text);
                }
                else
                {
                    currentText.Append(' ').Append(line.Text);
                }
            }
            else
            {
                currentText.Append(line.Text);
            }

            currentLines.Add(line);
            currentWords.AddRange(line.Words);
            currentBoxes.AddRange(line.Boxes);
        }

        Flush();
        return result.Select(r => (r.Item1, r.Item2, r.Item3, r.Item4)).ToList();
    }

    /// <summary>True when the trimmed text ends with a real sentence terminator.</summary>
    private static bool EndsSentence(string text)
    {
        if (text.Length == 0) return false;
        char c = text[^1];
        return c == '.' || c == '!' || c == '?';
    }


    /// <summary>Builds a spine directly from sentence texts (test/preview seam).</summary>
    public static ReadingSpine FromSentences(IReadOnlyList<string> texts)
    {
        var sentences = texts
            .Select((t, i) => new Sentence(i, 1, t, new[] { new Box(50, 800 - 18.0 * i, 400, 12) }, Array.Empty<Word>(), Array.Empty<Line>()))
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
