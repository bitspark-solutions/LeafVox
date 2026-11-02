using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace LeafVox.Core;

/// <summary>Page letters → visual lines in reading order, using the document-layout-analysis pipeline.</summary>
public static class LineExtractor
{
    public static IReadOnlyList<Line> ExtractLines(Page page)
    {
        var letters = DuplicateOverlappingTextProcessor.Get(page.Letters);
        var pdfWords = NearestNeighbourWordExtractor.Instance
            .GetWords(letters)
            .Where(w => !string.IsNullOrWhiteSpace(w.Text))
            .ToArray();

        if (pdfWords.Length == 0) return Array.Empty<Line>();

        try
        {
            var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(pdfWords);
            if (blocks.Count == 0) return SimpleSegmenter.ExtractLines(pdfWords);

            // Multi-column pages get an unsupervised reading-order pass; single blocks need none.
            if (blocks.Count > 1)
            {
                try { blocks = UnsupervisedReadingOrderDetector.Instance.Get(blocks).ToList(); }
                catch (Exception) { /* keep Docstrum order */ }
            }

            var lines = new List<Line>();
            foreach (var block in blocks)
            {
                foreach (var tl in block.TextLines)
                {
                    var text = TextNormalizer.Normalize(ReconstructWithSpaces(tl));
                    if (text.Length == 0) continue;
                    var b = tl.BoundingBox;
                    var lineWords = tl.Words.Select(w =>
                    {
                        var wb = w.BoundingBox;
                        return new Word(w.Text, new Box(wb.Left, wb.Bottom, wb.Width, wb.Height));
                    }).ToList();
                    lines.Add(new Line(text, b.Top,
                        new[] { new Box(b.Left, b.Bottom, b.Width, b.Height) }, lineWords));
                }
            }
            return lines;
        }
        catch (Exception)
        {
            return SimpleSegmenter.ExtractLines(pdfWords);
        }
    }

    /// <summary>
    /// Rebuilds a text line from letters, inserting spaces where the horizontal gap
    /// between glyphs is word-sized. Fixes PDFs whose fonts carry no space glyph
    /// (words run together as "inrespectandadmiration").
    /// </summary>
    internal static string ReconstructWithSpaces(TextLine line)
    {
        var letters = line.Words.SelectMany(w => w.Letters).ToArray();
        if (letters.Length == 0) return line.Text;
        var wordEnds = new HashSet<Letter>(line.Words.Select(w => w.Letters.Count > 0 ? w.Letters[^1] : null!).Where(x => x is not null)!);

        var sb = new System.Text.StringBuilder(line.Text.Length + 16);
        Letter? prev = null;
        foreach (var l in letters)
        {
            if (prev is not null)
            {
                double gapX = l.GlyphRectangle.Left - prev.GlyphRectangle.Right;
                bool explicitSpace = prev.Value.Length == 0 || l.Value.Length == 0
                    || prev.Value[^1] == ' ' || l.Value[0] == ' '
                    || prev.Value[^1] == '\u00a0' || l.Value[0] == '\u00a0';
                double size = Math.Max(1.0, Math.Min(prev.PointSize, l.PointSize));
                bool wordBoundary = wordEnds.Contains(prev);
                if (!explicitSpace && (wordBoundary || gapX > 0.22 * size)) sb.Append(' ');
            }
            sb.Append(l.Value);
            prev = l;
        }
        return sb.ToString();
    }
}
