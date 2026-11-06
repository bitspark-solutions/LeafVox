using Avalonia.Media.Imaging;
using PDFtoImage;
using SkiaSharp;

namespace LeafVox.App.Services;

/// <summary>
/// Stateless PDF page renderer (pdfium via PDFtoImage). Produces a fresh bitmap per call;
/// caching and lifetime are owned by the caller (PageVm keeps bitmaps alive per page).
/// </summary>
public sealed class PageRenderer : IDisposable
{
    private readonly byte[] _document;
    private readonly List<System.Drawing.SizeF> _pageSizesPt;

    public PageRenderer(byte[] documentBytes, string? password = null)
    {
        _document = documentBytes;
        PageCount = Conversion.GetPageCount(documentBytes, password);
        _pageSizesPt = Conversion.GetPageSizes(documentBytes, password).ToList();
    }

    public int PageCount { get; }
    public double PageWidthPt(int page1Based) => _pageSizesPt[page1Based - 1].Width;
    public double PageHeightPt(int page1Based) => _pageSizesPt[page1Based - 1].Height;

    /// <summary>1-based page → PNG bytes at arbitrary DPI (for OCR). Off-UI-thread safe.</summary>
    public byte[] RenderPngBytes(int page1Based, int dpi)
    {
        using var bmp = Conversion.ToImage(_document, page1Based - 1, null, new RenderOptions { Dpi = dpi });
        using var img = SKImage.FromBitmap(bmp);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>1-based page number → new bitmap at the given DPI. Must be called off the UI thread.</summary>
    public Bitmap Render(int page1Based, int dpi = 150)
    {
        using var bmp = Conversion.ToImage(_document, page1Based - 1, null, new RenderOptions { Dpi = dpi });
        using var img = SKImage.FromBitmap(bmp);
        using var data = img.Encode(SKEncodedImageFormat.Png, 90);
        var png = data.ToArray();

        using var ms = new MemoryStream(png);
        return new Bitmap(ms);
    }

    public void Dispose() { /* stateless: nothing cached to release */ }
}
