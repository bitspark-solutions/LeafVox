using PDFtoImage;
using SkiaSharp;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using LeafVox.App.Services;

// Spike E: prove the OCR fallback recovers speech-ready text from a page that is ONLY pixels.
// 1) build a known PDF, 2) rasterize it with pdfium, 3) OCR the raster, 4) compare.

var builder = new PdfDocumentBuilder();
var font = builder.AddStandard14Font(Standard14Font.Helvetica);
var page = builder.AddPage(595, 842);
page.AddText("The quick brown fox jumps over the lazy dog. LeafVox reads every scanned page.", 14, new PdfPoint(50, 780), font);
page.AddText("A second sentence provides extra words for reliable line detection.", 14, new PdfPoint(50, 750), font);
byte[] doc = builder.Build();

using var bmp = Conversion.ToImage(doc, 0, null, new RenderOptions { Dpi = 200 });
using var img = SKImage.FromBitmap(bmp);
using var data = img.Encode(SKEncodedImageFormat.Png, 100);
var png = data.ToArray();
Console.WriteLine($"page image {bmp.Width}x{bmp.Height} px ({png.LongLength} bytes PNG)");

int fail = 0;
void Check(string name, bool ok, string detail = "")
{
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {name}{(detail.Length > 0 ? ": " + detail : "")}");
    if (!ok) fail++;
}

var ocr = new OcrService();
try
{
    await ocr.EnsureLanguageAsync();
    Check("traineddata downloaded", File.Exists(Path.Combine(OcrService.TessDataDir, "eng.traineddata")));

    var sw = System.Diagnostics.Stopwatch.StartNew();
    var lines = ocr.OcrPngLines(png, 842, 200);
    sw.Stop();
    Check("ocr produced lines", lines.Count >= 2, $"{lines.Count} lines in {sw.ElapsedMilliseconds} ms");
    var text = string.Join(" ", lines.Select(l => l.Text));
    Console.WriteLine($"  OCR text: {text}");
    Check("keyword round-trip", text.Contains("quick") && text.Contains("LeafVox"), $"got: {text}");
    Check("boxes sane", lines.All(l => l.Boxes.Count > 0 && l.Boxes[0].Width > 5 && l.Top > 0 && l.Top <= 842),
        string.Join(" | ", lines.Take(2).Select(l => $"top={l.Top:F0} boxW={l.Boxes[0].Width:F0}")));
}
catch (Exception ex)
{
    Check("no exception", false, ex.GetType().Name + ": " + ex.Message);
}

Console.WriteLine(fail == 0 ? "SPIKE E RESULT: PASS" : $"SPIKE E RESULT: {fail} FAILURES");
return fail == 0 ? 0 : 1;
