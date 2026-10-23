using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;

namespace LeafVox.Core;

/// <summary>Lazy handle over a PDF document. Parsing is per-page; nothing is loaded up-front.</summary>
public sealed class PdfBook : IDisposable
{
    private readonly PdfDocument _doc;

    private PdfBook(PdfDocument doc, string path)
    {
        _doc = doc;
        Path = path;
    }

    public string Path { get; }
    public int NumberOfPages => _doc.NumberOfPages;
    public string? Title => string.IsNullOrWhiteSpace(_doc.Information.Title) ? null : _doc.Information.Title;
    public string? Author => string.IsNullOrWhiteSpace(_doc.Information.Author) ? null : _doc.Information.Author;

    public static PdfBook Open(string path, string? password = null)
    {
        try
        {
            var options = new ParsingOptions { UseLenientParsing = true, SkipMissingFonts = true };
            if (password is not null) options.Password = password;
            return new PdfBook(PdfDocument.Open(path, options), path);
        }
        catch (Exception ex) when (IsPasswordProblem(ex))
        {
            throw new PdfLockedException(path);
        }
        catch (Exception ex)
        {
            throw new PdfCorruptException(path, ex);
        }
    }

    private static bool IsPasswordProblem(Exception ex)
    {
        for (Exception? e = ex; e is not null; e = e.InnerException)
        {
            if (e.GetType().Name.Contains("Password") || e.GetType().Name.Contains("Encrypt")) return true;
            if (e.Message.Contains("password", StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    /// <summary>Extracts ordered visual lines of one page (1-based). Never throws on odd pages — returns empty.</summary>
    public PageText ExtractPage(int pageNumber)
    {
        try
        {
            var page = _doc.GetPage(pageNumber);
            var lines = LineExtractor.ExtractLines(page);
            return new PageText(pageNumber, page.Height, lines);
        }
        catch (Exception)
        {
            return new PageText(pageNumber, 0, Array.Empty<Line>());
        }
    }

    public void Dispose() => _doc.Dispose();
}
