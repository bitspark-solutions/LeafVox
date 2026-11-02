namespace LeafVox.Core;

/// <summary>A speakable sentence plus the visual boxes it occupies.</summary>
public sealed record Sentence(
    int Index,
    int PageNumber,
    string Text,
    IReadOnlyList<Box> Boxes,
    IReadOnlyList<Word> Words,
    IReadOnlyList<Line> Lines
)
{
    public Sentence(int index, int pageNumber, string text, IReadOnlyList<Box> boxes)
        : this(index, pageNumber, text, boxes, Array.Empty<Word>(), Array.Empty<Line>()) { }
}
