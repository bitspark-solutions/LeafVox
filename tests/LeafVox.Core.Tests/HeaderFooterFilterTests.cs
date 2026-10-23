namespace LeafVox.Core.Tests;

public class HeaderFooterFilterTests
{
    private static Line L(string t, double top) => new(t, top, new[] { new Box(50, top, 400, 12) });

    [Fact]
    public void Removes_Repeated_Edge_Lines()
    {
        var pages = Enumerable.Range(1, 5).Select(i => new PageText(i, 842, new[]
        {
            L("The Great Leaf Book", 830),        // header band
            L("Body line one for page.", 600),
            L("Body line two continues.", 580),
            L("Confidential", 20),               // footer band
        })).ToList();

        var filtered = HeaderFooterFilter.Apply(pages);
        foreach (var p in filtered)
        {
            Assert.DoesNotContain("The Great Leaf Book", p.Lines.Select(l => l.Text));
            Assert.DoesNotContain("Confidential", p.Lines.Select(l => l.Text));
            Assert.Contains(p.Lines, l => l.Text == "Body line one for page.");
        }
    }

    [Fact]
    public void Keeps_Body_Repeats_And_Few_Page_Docs()
    {
        var pages = new[]
        {
            new PageText(1, 842, new[] { L("Never mind.", 600), L("Never mind.", 580) }),
            new PageText(2, 842, new[] { L("Never mind.", 600) }),
        };
        var filtered = HeaderFooterFilter.Apply(pages);
        Assert.Equal(2, filtered[0].Lines.Count);
    }
}
