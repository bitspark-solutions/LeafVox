using System.Web;

namespace LeafVox.Tts.Tests;

public class VoiceCatalogTests
{
    [Fact]
    public void Catalog_Has_Unique_Ids_And_Https_Urls()
    {
        Assert.Equal(VoiceCatalog.All.Count, VoiceCatalog.All.Select(v => v.Id).Distinct().Count());
        Assert.All(VoiceCatalog.All, v => Assert.StartsWith("https://", v.ArchiveUrl));
        Assert.All(VoiceCatalog.All, v => Assert.True(v.ArchiveBytes > 0));
    }

    [Fact]
    public void Default_Is_Lessac()
        => Assert.Equal("en_US-lessac-medium", VoiceCatalog.Default.Id);

    [Fact]
    public void Find_Works_And_Returns_Null_For_Unknown()
    {
        Assert.NotNull(VoiceCatalog.Find("kokoro-en-v0_19"));
        Assert.Null(VoiceCatalog.Find("nope"));
    }
}
