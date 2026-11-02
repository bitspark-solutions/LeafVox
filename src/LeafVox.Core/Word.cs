namespace LeafVox.Core;

/// <summary>One spoken word with its exact visual box on the page (PDF points, bottom-left origin).</summary>
public sealed record Word(string Text, Box Box);
