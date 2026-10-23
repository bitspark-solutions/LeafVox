namespace LeafVox.Core;

/// <summary>A speakable sentence plus the visual boxes it occupies.</summary>
public sealed record Sentence(int Index, int PageNumber, string Text, IReadOnlyList<Box> Boxes);
