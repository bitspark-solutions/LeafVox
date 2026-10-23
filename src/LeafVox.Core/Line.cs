namespace LeafVox.Core;

/// <summary>One visual text line on a page, ordered top to bottom.</summary>
public sealed record Line(string Text, double Top, IReadOnlyList<Box> Boxes);
