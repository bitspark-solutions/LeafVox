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
        var words = NearestNeighbourWordExtractor.Instance
            .GetWords(letters)
            .Where(w => !string.IsNullOrWhiteSpace(w.Text))
            .ToArray();

        if (words.Length == 0) return Array.Empty<Line>();

        try
        {
            var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);
            if (blocks.Count == 0) return SimpleSegmenter.ExtractLines(words);

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
                    var text = TextNormalizer.Normalize(tl.Text);
                    if (text.Length == 0) continue;
                    var b = tl.BoundingBox;
                    lines.Add(new Line(text, b.Top,
                        new[] { new Box(b.Left, b.Bottom, b.Width, b.Height) }));
                }
            }
            return lines;
        }
        catch (Exception)
        {
            return SimpleSegmenter.ExtractLines(words);
        }
    }
}
