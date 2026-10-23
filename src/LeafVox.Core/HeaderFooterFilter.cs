namespace LeafVox.Core;

/// <summary>
/// Detects lines that repeat near the top/bottom edge across most pages
/// (running headers/footers, book folios) so they can be dropped from the spine.
/// </summary>
public static class HeaderFooterFilter
{
    public static IReadOnlyList<PageText> Apply(IReadOnlyList<PageText> pages)
    {
        if (pages.Count < 3) return pages; // too few pages to be confident

        var headerCandidates = Collect(pages, isHeader: true);
        var footerCandidates = Collect(pages, isHeader: false);

        var result = new List<PageText>(pages.Count);
        foreach (var page in pages)
        {
            if (page.Height <= 0 || page.Lines.Count == 0) { result.Add(page); continue; }
            var topBand = page.Height * 0.12;
            var bottomBand = page.Height * 0.12;

            var kept = page.Lines.Where(l =>
            {
                var key = Key(l.Text);
                if (l.Top >= page.Height - topBand && headerCandidates.Contains(key)) return false;
                if (l.Top <= bottomBand && footerCandidates.Contains(key)) return false;
                return true;
            }).ToList();

            result.Add(new PageText(page.PageNumber, page.Height, kept));
        }
        return result;
    }

    private static HashSet<string> Collect(IReadOnlyList<PageText> pages, bool isHeader)
    {
        var counts = new Dictionary<string, int>();
        int inBand = 0;
        foreach (var page in pages)
        {
            if (page.Height <= 0 || page.Lines.Count == 0) continue;
            inBand++;
            var band = page.Height * 0.12;
            foreach (var l in page.Lines)
            {
                bool edge = isHeader ? l.Top >= page.Height - band : l.Top <= band;
                if (!edge) continue;
                var key = Key(l.Text);
                if (key.Length == 0) continue;
                counts[key] = counts.TryGetValue(key, out var c) ? c + 1 : 1;
            }
        }
        int threshold = Math.Max(2, (int)(inBand * 0.6));
        return counts.Where(kv => kv.Value >= threshold).Select(kv => kv.Key).ToHashSet();
    }

    /// <summary>Normalization that also folds digits so "Page 12"/"Page 13" match as one folio pattern.</summary>
    private static string Key(string text)
    {
        var chars = text.ToLowerInvariant().Select(c => char.IsDigit(c) ? '#' : c).ToArray();
        return new string(chars);
    }
}
