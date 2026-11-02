using System.Linq;
using UglyToad.PdfPig.Content;

namespace LeafVox.Core;

/// <summary>Y-band grouping fallback used when the layout-analysis pipeline misbehaves on a page.</summary>
public static class SimpleSegmenter
{
    public static IReadOnlyList<Line> ExtractLines(IReadOnlyList<UglyToad.PdfPig.Content.Word> pdfWords)
    {
        if (pdfWords.Count == 0) return Array.Empty<Line>();

        var words = pdfWords.Select(w => new LeafVox.Core.Word(
            w.Text,
            new Box(w.BoundingBox.Left, w.BoundingBox.Bottom, w.BoundingBox.Width, w.BoundingBox.Height)
        )).ToList();

        var medianHeight = words.Select(w => w.Box.Height).OrderBy(h => h).ToArray()[words.Count / 2];
        var tolerance = Math.Max(1.5, medianHeight * 0.45);

        var bands = words
            .GroupBy(w => Math.Round((w.Box.Bottom + w.Box.Height / 2.0) / tolerance))
            .OrderByDescending(g => g.Key)
            .Select(g =>
            {
                var ordered = g.OrderBy(w => w.Box.Left).ToArray();
                var text = TextNormalizer.Normalize(string.Join(" ", ordered.Select(w => w.Text.Trim())));
                var boxes = ordered
                    .Select(w => new Box(w.Box.Left, w.Box.Bottom, w.Box.Width, w.Box.Height))
                    .ToArray();
                return new Line(text, ordered.Max(w => w.Box.Top), boxes, ordered.Select(w => new LeafVox.Core.Word(w.Text, w.Box)).ToList());
            })
            .Where(l => l.Text.Length > 0)
            .ToArray();
        return bands;
    }
}
