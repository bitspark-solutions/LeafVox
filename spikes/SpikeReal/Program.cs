using System.Diagnostics;
using LeafVox.App.Services;
using LeafVox.Core;
using PDFtoImage;
using SkiaSharp;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

// Spike F: torture-test the reading spine on REAL documents (arXiv papers, a MIT Press book)
// and run the full scanned→OCR→spine flow end to end. Documents live outside the repo
// (free downloads); this spike is run manually, not part of the per-commit gate.
var dir = args.Length > 0 ? args[0] : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "leafvox-testdocs");
if (!Directory.Exists(dir)) { Console.WriteLine($"missing corpus dir: {dir}"); return 2; }

int fail = 0;
void Check(string name, bool ok, string detail = "")
{
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {name}{(detail.Length > 0 ? ": " + detail : "")}");
    if (!ok) fail++;
}

foreach (var path in Directory.EnumerateFiles(dir, "*.pdf").Order())
{
    var sw = Stopwatch.StartNew();
    try
    {
        using var book = PdfBook.Open(path);
        var spine = ReadingSpine.Build(book);
        sw.Stop();
        var name = Path.GetFileName(path);
        var sentences = spine.Sentences;
        int Wc(LeafVox.Core.Sentence s) => s.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        double avgWords = sentences.Average(Wc);
        int maxW = sentences.Max(Wc);
        double perPage = (double)sentences.Count / book.NumberOfPages;
        Console.WriteLine($"\n== {name}: {book.NumberOfPages} pages → {sentences.Count} lines in {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"   avg {avgWords:F1} words | max {maxW} | scanned {spine.ScannedPages.Count}");
        var sample = sentences[Math.Min(40, sentences.Count - 1)].Text;
        Console.WriteLine("   sample: " + sample[..Math.Min(110, sample.Length)]);

        Check($"{name} parse time", sw.ElapsedMilliseconds < 60_000, $"{sw.ElapsedMilliseconds} ms");
        Check($"{name} density", sentences.Count >= book.NumberOfPages * 8,
            $"{perPage:F0} per page");
        Check($"{name} avg sentence size", avgWords is >= 3 and <= 45, $"{avgWords:F1}");
        Check($"{name} no mega-sentence", maxW <= 120, $"max {maxW}");
        var monotonic = sentences.Zip(sentences.Skip(1), (a, b) => b.PageNumber >= a.PageNumber).All(x => x);
        Check($"{name} page order monotonic", monotonic);
    }
    catch (Exception ex)
    {
        Check($"{Path.GetFileName(path)} opens", false, ex.GetType().Name + ": " + ex.Message);
    }
}

// ---- scanned → OCR → spine, end to end on a synthetic scan ----
Console.WriteLine("\n== synthetic scanned book ==");
{
    var b = new PdfDocumentBuilder();
    var font = b.AddStandard14Font(Standard14Font.Helvetica);
    var textPage = b.AddPage(595, 842);
    textPage.AddText("Chapter one begins with a sentence about foxes. It jumps over dogs repeatedly, as is tradition.", 12, new PdfPoint(50, 700), font);
    var docBytes = b.Build();
    byte[] scan;
    using (var img0 = Conversion.ToImage(docBytes, 0, null, new RenderOptions { Dpi = 150 }))
    {
        var builder2 = new PdfDocumentBuilder();
        var page = builder2.AddPage(595, 842);
        page.AddPng(img0.Encode(SKEncodedImageFormat.Png, 100).ToArray(), new PdfRectangle(0, 0, 595, 842));
        scan = builder2.Build();
    }
    var scanPath = Path.Combine(Path.GetTempPath(), "leafvox-scan-test.pdf");
    File.WriteAllBytes(scanPath, scan);

    using (var book = PdfBook.Open(scanPath))
    {
        var spine = ReadingSpine.Build(book);
        Check("scan detected", spine.ScannedPages.Contains(1) && spine.Sentences.Count == 0);

        var ocr = new OcrService();
        await ocr.EnsureLanguageAsync();
        var renderer = new PageRenderer(scan);
        var png = renderer.RenderPngBytes(1, 300);
        var lines = ocr.OcrPngLines(png, renderer.PageHeightPt(1), 300);
        var overrides = new Dictionary<int, IReadOnlyList<Line>> { [1] = lines };
        var fixedSpine = ReadingSpine.Build(book, overrides);
        var text = string.Join(" ", fixedSpine.Sentences.Select(s => s.Text));
        Console.WriteLine($"   recovered: {text}");
        Check("scan rescued by OCR", fixedSpine.Sentences.Count >= 1 && text.Contains("foxes"),
            $"{fixedSpine.Sentences.Count} sentences");
        Check("boxes on recovered text", fixedSpine.Sentences.All(s => s.Boxes.Count > 0));
        renderer.Dispose();
    }
    File.Delete(scanPath);
}

Console.WriteLine(fail == 0 ? "\nSPIKE F RESULT: PASS" : $"\nSPIKE F RESULT: {fail} FAILURES");
return fail == 0 ? 0 : 1;
