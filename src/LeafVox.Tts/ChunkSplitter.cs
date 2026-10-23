namespace LeafVox.Tts;

/// <summary>Hard-splits over-long sentences into synthesis-friendly pieces (word boundaries).</summary>
public static class ChunkSplitter
{
    public const int MaxChars = 120;

    public static IReadOnlyList<string> Split(string sentence)
    {
        if (sentence.Length <= MaxChars) return new[] { sentence };
        var parts = new List<string>();
        var rest = sentence.AsSpan();
        while (rest.Length > MaxChars)
        {
            int cut = -1;
            for (int i = MaxChars; i >= MaxChars / 2; i--)
            {
                if (rest[i - 1] == ' ' || rest[i - 1] == ',' || rest[i - 1] == ';') { cut = i - 1; break; }
            }
            if (cut <= 0) cut = MaxChars;
            parts.Add(rest[..cut].ToString().Trim());
            rest = rest[cut..].TrimStart();
        }
        if (rest.Length > 0) parts.Add(rest.ToString().Trim());
        return parts;
    }
}
