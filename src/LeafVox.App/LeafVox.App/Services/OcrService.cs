using LeafVox.Core;
using LeafVox.Tts;

namespace LeafVox.App.Services;

/// <summary>
/// OCR for scanned pages: downloads the small Apache-2.0 eng.traineddata on first use
/// and turns a rendered page PNG into spine Lines with PDF-point boxes.
/// Windows ships Tesseract natives via NuGet; other OSes need a system libtesseract.
/// </summary>
public sealed class OcrService
{
    private const string TrainedDataUrl =
        "https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main/eng.traineddata";

    public static string TessDataDir => Path.Combine(Paths.Data, "tessdata");

    public async Task EnsureLanguageAsync(CancellationToken ct = default)
    {
        var file = Path.Combine(TessDataDir, "eng.traineddata");
        if (File.Exists(file)) return;
        Directory.CreateDirectory(TessDataDir);
        using var http = new HttpClient();
        var bytes = await http.GetByteArrayAsync(TrainedDataUrl, ct).ConfigureAwait(false);
        var tmp = file + ".part";
        await File.WriteAllBytesAsync(tmp, bytes, ct).ConfigureAwait(false);
        File.Move(tmp, file, overwrite: true);
    }

    /// <summary>Runs OCR on an already-rendered page; returns lines in PDF point space.</summary>
    public IReadOnlyList<Line> OcrPngLines(byte[] png, double pageHeightPt, int dpi)
    {
        double k = 72.0 / dpi;
        var lines = new List<Line>();
        using var api = new global::Tesseract.TesseractEngine(TessDataDir, "eng");
        api.DefaultPageSegMode = global::Tesseract.PageSegMode.Auto;
        using var img = global::Tesseract.Pix.LoadFromMemory(png);
        using var res = api.Process(img);
        using var it = res.GetIterator();
        if (it is null) return lines;
        do
        {
            var text = it.GetText(global::Tesseract.PageIteratorLevel.TextLine)?.Trim();
            if (string.IsNullOrEmpty(text)) continue;
            if (!it.TryGetBoundingBox(global::Tesseract.PageIteratorLevel.TextLine, out var r)) continue;
            double left = r.X1 * k;
            double topPt = pageHeightPt - r.Y1 * k;
            double w = (r.X2 - r.X1) * k;
            double h = (r.Y2 - r.Y1) * k;
            lines.Add(new Line(Normalize(text), topPt, new[] { new Box(left, topPt - h, w, h) }));
        }
        while (it.Next(global::Tesseract.PageIteratorLevel.TextLine));
        return lines;
    }

    private static string Normalize(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        char? prev = null;
        foreach (var c in s)
        {
            if (c == ' ' && prev == ' ') continue;
            sb.Append(c);
            prev = c;
        }
        return sb.ToString().Trim();
    }
}
