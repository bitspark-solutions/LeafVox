using UglyToad.PdfPig.Content;

namespace LeafVox.Core;

/// <summary>Y-band grouping fallback used when the layout-analysis pipeline misbehaves on a page.</summary>
public static class SimpleSegmenter
{
    public static IReadOnlyList<Line> ExtractLines(IReadOnlyList<Word> words)
    {
        if (words.Count == 0) return Array.Empty<Line>();
        var medianHeight = words.Select(w => w.BoundingBox.Height).OrderBy(h => h).ToArray()[words.Count / 2];
        var tolerance = Math.Max(1.5, medianHeight * 0.45);

        var bands = words
            .GroupBy(w => Math.Round(w.BoundingBox.Centroid.Y / tolerance))
            .OrderByDescending(g => g.Key)
            .Select(g =>
            {
                var ordered = g.OrderBy(w => w.BoundingBox.Left).ToArray();
                var text = TextNormalizer.Normalize(string.Join(" ", ordered.Select(w => w.Text.Trim())));
                var boxes = ordered
                    .Select(w => new Box(w.BoundingBox.Left, w.BoundingBox.Bottom, w.BoundingBox.Width, w.BoundingBox.Height))
                    .ToArray();
                return new Line(text, ordered.Max(w => w.BoundingBox.Top), boxes);
            })
            .Where(l => l.Text.Length > 0)
            .ToArray();
        return bands;
    }
}
