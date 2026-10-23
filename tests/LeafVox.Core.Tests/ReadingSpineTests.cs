namespace LeafVox.Core.Tests;

public class ReadingSpineTests : IDisposable
{
    private readonly List<string> _files = new();
    public void Dispose() { foreach (var f in _files) try { File.Delete(f); } catch { } }

    private PdfBook Open(string path) { _files.Add(path); return PdfBook.Open(path); }

    [Fact]
    public void Simple_Document_Ordered_Across_Pages()
    {
        var path = TestPdf.Build(
            p => p.Line("LeafVox reads any PDF aloud.").Line("The first sentence is complete here."),
            p => p.Line("Page two begins the sequel. It ends right here."));
        using var book = Open(path);
        var spine = ReadingSpine.Build(book);

        Assert.Equal(4, spine.Sentences.Count);
        Assert.Equal("LeafVox reads any PDF aloud.", spine.Sentences[0].Text);
        Assert.Equal(1, spine.Sentences[0].PageNumber);
        Assert.Equal(2, spine.Sentences[2].PageNumber);
        Assert.Equal(0, spine.IndexForPage(1));
        Assert.Equal(2, spine.IndexForPage(2));
    }

    [Fact]
    public void Hyphenated_Line_Becomes_One_Sentence()
    {
        var path = TestPdf.Build(p => p
            .Line("This report covers read-")
            .Line("ing large books end to end."));
        using var book = Open(path);
        var spine = ReadingSpine.Build(book);
        Assert.Single(spine.Sentences);
        Assert.Equal("This report covers reading large books end to end.", spine.Sentences[0].Text);
    }

    [Fact]
    public void Multi_Column_Page_Reads_Columns_In_Order()
    {
        var path = TestPdf.Build(p => p
            .Line("First column sentence one.", 50, 800)
            .Line("First column ends here.", 50, 782)
            .Line("Second column starts now.", 320, 800)
            .Line("Second column ends here.", 320, 782));
        using var book = Open(path);
        var spine = ReadingSpine.Build(book);
        Assert.True(spine.Sentences.Count >= 3, $"got {spine.Sentences.Count}");
        Assert.StartsWith("First column sentence one.", spine.Sentences[0].Text);
        Assert.True(spine.Sentences.Any(s => s.Text.StartsWith("Second column")));
        var firstIdx = spine.Sentences.First(s => s.Text.StartsWith("First column ends")).Index;
        var secondIdx = spine.Sentences.First(s => s.Text.StartsWith("Second column starts")).Index;
        Assert.True(firstIdx < secondIdx, "left column must finish before right column starts");
    }

    [Fact]
    public void Headers_And_Footers_Dropped_From_Spine()
    {
        Action<TestPdfPageBuilder> Page(int n) => p => p
            .Line("The Great Leaf Book", 50, 830)
            .Line($"Body sentence number {n} tells the tale.", 50, 600)
            .Line("Confidential", 50, 20);
        var path = TestPdf.Build(Page(1), Page(2), Page(3), Page(4));
        using var book = Open(path);
        var spine = ReadingSpine.Build(book);
        Assert.Equal(4, spine.Sentences.Count);
        Assert.All(spine.Sentences, s =>
        {
            Assert.DoesNotContain("The Great Leaf Book", s.Text);
            Assert.DoesNotContain("Confidential", s.Text);
        });
    }

    [Fact]
    public void Empty_Page_Flagged_Scanned()
    {
        var path = TestPdf.Build(p => p.Line("Real text lives here."), p => { });
        using var book = Open(path);
        var spine = ReadingSpine.Build(book);
        Assert.True(spine.PageIsScanned(2));
        Assert.False(spine.PageIsScanned(1));
    }

    [Fact]
    public void IndexNearest_Picks_Clicked_Line()
    {
        var path = TestPdf.Build(p => p
            .Line("Alpha line one.", 50, 800)
            .Line("Bravo line two.", 50, 700)
            .Line("Charlie line three.", 50, 600));
        using var book = Open(path);
        var spine = ReadingSpine.Build(book);
        Assert.Equal(1, spine.IndexNearest(1, 705));   // near Bravo
        Assert.Equal(2, spine.IndexNearest(1, 600));   // Charlie
    }

    [Fact]
    public void Junk_File_Throws_Corrupt()
    {
        var path = Path.Combine(Path.GetTempPath(), $"leafvox-junk-{Guid.NewGuid():N}.pdf");
        File.WriteAllText(path, "this is not a pdf");
        _files.Add(path);
        Assert.Throws<PdfCorruptException>(() => PdfBook.Open(path));
    }
}
