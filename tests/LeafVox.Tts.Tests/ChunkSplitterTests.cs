namespace LeafVox.Tts.Tests;

public class ChunkSplitterTests
{
    [Fact]
    public void Short_Sentence_Untouched()
    {
        var parts = ChunkSplitter.Split("Hello there, friend.");
        Assert.Single(parts);
    }

    [Fact]
    public void Long_Sentence_Splits_On_Word_Boundaries()
    {
        var sentence = string.Join(" ", Enumerable.Repeat("word", 40));
        var parts = ChunkSplitter.Split(sentence);
        Assert.True(parts.Count > 1);
        Assert.All(parts, p => Assert.True(p.Length <= ChunkSplitter.MaxChars || !p.Contains(' '), $"bad chunk: {p}"));
        Assert.Equal(sentence, string.Join(" ", parts));
    }

    [Fact]
    public void Prefers_Comma_Cuts()
    {
        var sentence = new string('a', 100) + ", " + new string('b', 100);
        var parts = ChunkSplitter.Split(sentence);
        Assert.Equal(2, parts.Count);
        Assert.EndsWith(",", parts[0]); // comma stays with the chunk it belongs to
    }
}
