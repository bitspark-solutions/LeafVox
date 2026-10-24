using PDFtoImage;
using SkiaSharp;

// Spike D: prove PDFtoImage (pdfium+SkiaSharp, MIT, cross-platform natives) renders real pages.
var pdf = Path.Combine("..", "SpikePdf", "spike.pdf");
if (!File.Exists(pdf)) pdf = "spike.pdf";
if (!File.Exists(pdf))
{
    Console.WriteLine("run SpikePdf first to produce spike.pdf");
    return 1;
}

byte[] doc = File.ReadAllBytes(pdf);
int fail = 0;
void Check(string name, bool ok, string detail = "")
{
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {name}{(detail.Length > 0 ? ": " + detail : "")}");
    if (!ok) fail++;
}

Check("page count", Conversion.GetPageCount(doc, null) == 2, $"{Conversion.GetPageCount(doc, null)} pages");

var sizes = Conversion.GetPageSizes(doc, null);
Check("page sizes", sizes.Count == 2 && Math.Abs(sizes[0].Height - 842) < 1, $"{sizes[0].Width}x{sizes[0].Height} pt");

var sw = System.Diagnostics.Stopwatch.StartNew();
using var bmp = Conversion.ToImage(doc, 0, null, new RenderOptions { Dpi = 150 });
sw.Stop();
Check("render dims", bmp.Width > 300 && bmp.Height > 300, $"{bmp.Width}x{bmp.Height} px in {sw.ElapsedMilliseconds} ms");

// text line sits at y=780pt (PDF bottom origin) → 62pt from top → ~129 px at 150 dpi
int ink = 0;
for (int x = 0; x < bmp.Width; x++)
{
    var c = bmp.GetPixel(x, 125);
    if (c.Red < 200 || c.Green < 200 || c.Blue < 200) ink++;
}
Check("pixels painted", ink > 50, $"{ink} dark pixels on the text row");

using var bmp2 = Conversion.ToImage(doc, 1, null, new RenderOptions { Dpi = 100 });
Check("second page renders", bmp2.Width > 100, $"{bmp2.Width}x{bmp2.Height}");

Console.WriteLine(fail == 0 ? "SPIKE D RESULT: PASS" : $"SPIKE D RESULT: {fail} FAILURES");
return fail == 0 ? 0 : 1;
