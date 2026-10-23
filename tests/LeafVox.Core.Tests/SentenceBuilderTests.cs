namespace LeafVox.Core.Tests;

public class SentenceBuilderTests
{
    [Fact]
    public void JoinLines_Unhyphenates_CrossLineBreak()
    {
        var joined = SentenceBuilder.JoinLines(new[] { "This report covers read-", "ing large books end to end." });
        Assert.Equal("This report covers reading large books end to end.", joined);
    }

    [Fact]
    public void JoinLines_Keeps_SpaceJoin_ForNormalLines()
    {
        var joined = SentenceBuilder.JoinLines(new[] { "Alpha beta", "gamma delta." });
        Assert.Equal("Alpha beta gamma delta.", joined);
    }

    [Fact]
    public void Split_TwoSentences()
    {
        var s = SentenceBuilder.Split("LeafVox reads any PDF aloud. The first sentence is complete here.");
        Assert.Equal(2, s.Count);
        Assert.Equal("LeafVox reads any PDF aloud.", s[0]);
    }

    [Fact]
    public void Split_Respects_Abbreviations()
    {
        var s = SentenceBuilder.Split("Dr. Smith went to Washington. He arrived at 5 p.m. Then he left.");
        Assert.Equal(2, s.Count);
        Assert.Equal("Dr. Smith went to Washington.", s[0]);
        Assert.Equal("He arrived at 5 p.m. Then he left.", s[1]);
    }

    [Fact]
    public void Split_Respects_Decimals()
    {
        var s = SentenceBuilder.Split("The ratio was 3.5 but the total was twelve.");
        Assert.Single(s);
    }

    [Fact]
    public void Split_Exclamations_And_Questions()
    {
        var s = SentenceBuilder.Split("Wow! Can you hear me? Yes, loud and clear.");
        Assert.Equal(3, s.Count);
    }

    [Fact]
    public void Split_Keeps_Quoted_Endings()
    {
        var s = SentenceBuilder.Split("He said \"Come here.\" Then she ran.");
        Assert.Equal(2, s.Count);
        Assert.Equal("He said \"Come here.\"", s[0]);
    }
}
