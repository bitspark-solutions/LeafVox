using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Writer;

namespace LeafVox.Core.Tests;

/// <summary>Fluent line placement for <see cref="TestPdf"/> fixtures.</summary>
public sealed class TestPdfPageBuilder
{
    private readonly PdfPageBuilder _page;
    private readonly PdfDocumentBuilder.AddedFont _font;
    private double _y = 800;

    public TestPdfPageBuilder(PdfPageBuilder page, PdfDocumentBuilder.AddedFont font)
    {
        _page = page;
        _font = font;
    }

    public TestPdfPageBuilder Line(string text, double x = 50, double? y = null, double size = 12)
    {
        _page.AddText(text, size, new PdfPoint(x, y ?? _y), _font);
        if (y is null) _y -= 18;
        return this;
    }
}
