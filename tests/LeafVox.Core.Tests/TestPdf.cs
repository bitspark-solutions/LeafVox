using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace LeafVox.Core.Tests;

/// <summary>Builds deterministic in-memory PDFs for tests.</summary>
public static class TestPdf
{
    public static string Build(Action<TestPdfPageBuilder> page1, params Action<TestPdfPageBuilder>[] morePages)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var p = builder.AddPage(595, 842);
        page1(new TestPdfPageBuilder(p, font));
        foreach (var extra in morePages)
        {
            var px = builder.AddPage(595, 842);
            extra(new TestPdfPageBuilder(px, font));
        }
        var path = Path.Combine(Path.GetTempPath(), $"leafvox-test-{Guid.NewGuid():N}.pdf");
        File.WriteAllBytes(path, builder.Build());
        return path;
    }
}
