namespace LeafVox.Core.Tests;

public class TextNormalizerTests
{
    [Theory]
    [InlineData("a  b", "a b")]
    [InlineData("\uFB01le", "file")]           // ligature fi
    [InlineData("soft\u00ADhyphen", "softhyphen")]
    [InlineData("a\tb\nc", "a b c")]
    [InlineData("\u201Chello\u201D", "\"hello\"")]
    [InlineData("well\u2013known", "well-known")]
    public void Normalize_Works(string raw, string expected)
        => Assert.Equal(expected, TextNormalizer.Normalize(raw));
}
