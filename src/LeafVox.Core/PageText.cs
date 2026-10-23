namespace LeafVox.Core;

/// <summary>All extracted lines of one page before sentence assembly.</summary>
public sealed record PageText(int PageNumber, double Height, IReadOnlyList<Line> Lines)
{
    public bool LooksScanned => Lines.Sum(l => l.Text.Length) < 5;
}
