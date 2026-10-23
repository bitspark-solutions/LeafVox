// Spike A: prove PdfPig gives reading-ordered sentences + bounding boxes from a PDF.
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

static string JoinLines(IEnumerable<Word> words)
{
    // production pipeline (Phase 1) uses Docstrum blocks; spike tests PdfPig's native word order
    return string.Join(" ", words.Select(w => w.Text.Trim()).Where(t => t.Length > 0));
}

var text1 = "LeafVox reads any PDF aloud. The first sentence is complete here.";
var text2 = "This second page talks about read- ing documents at any speed, wherever you are.";

// Build a deterministic 2-page PDF in memory
var builder = new PdfDocumentBuilder();
var font = builder.AddStandard14Font(Standard14Font.Helvetica);
var p1 = builder.AddPage(595, 842);
p1.AddText(text1, 12, new PdfPoint(50, 780), font);
var p2 = builder.AddPage(595, 842);
// split page-2 text over two visual lines to exercise hyphen-join
p2.AddText("This second page talks about read-", 12, new PdfPoint(50, 780), font);
p2.AddText("ing documents at any speed, wherever you are.", 12, new PdfPoint(50, 760), font);
byte[] pdf = builder.Build();
File.WriteAllBytes("spike.pdf", pdf);

using var doc = PdfDocument.Open("spike.pdf");
Console.WriteLine($"pages={doc.NumberOfPages}");
int pass = 0, fail = 0;

void Check(string label, bool ok, string detail)
{
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {label}: {detail}");
    if (ok) pass++; else fail++;
}

// Page 1: words + bboxes
var page1 = doc.GetPage(1);
var words1 = page1.GetWords(NearestNeighbourWordExtractor.Instance).ToList();
Check("words extracted p1", words1.Count >= 10, $"{words1.Count} words");
Check("bboxes present", words1.All(w => w.BoundingBox.Width > 0), "all boxes have width");
var joined1 = JoinLines(words1);
Check("sentence p1 round-trip", joined1 == text1, $"got: {joined1}");

// Page 2: hyphen split across lines -> join should recover "reading"
var page2 = doc.GetPage(2);
var words2 = page2.GetWords(NearestNeighbourWordExtractor.Instance).ToList();
var expected2 = text2.Replace("- ", "");
var joined2 = JoinLines(words2).Replace("- ", "");
Check("hyphen join p2", joined2 == expected2, $"got: {joined2}");

// Sentence segmentation smoke (regex-free, naive): count terminal punctuation
var spine = new List<string>();
foreach (var (words, idx) in new[] { (words1, 1), (words2, 2) }.Select((x, i) => (x.Item1, i + 1)))
{
    var t = JoinLines(words);
    foreach (var s in t.Split(new[] { ". " }, StringSplitOptions.RemoveEmptyEntries))
        spine.Add($"p{idx}: {s}.");
}
Check("spine sentence count", spine.Count >= 3, $"{spine.Count} sentences");
foreach (var s in spine) Console.WriteLine($"  > {s}");

Console.WriteLine(fail == 0 ? $"SPIKE A RESULT: {pass}/{pass + fail} PASS" : $"SPIKE A RESULT: {fail} FAILURES");
return fail == 0 ? 0 : 1;
