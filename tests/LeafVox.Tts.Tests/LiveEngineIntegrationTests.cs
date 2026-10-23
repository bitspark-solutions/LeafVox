using LeafVox.Core;

namespace LeafVox.Tts.Tests;

/// <summary>
/// Exercises the REAL neural engine end-to-end (parse-free): spine -> pipeline -> sherpa-onnx -> PCM.
/// Runs only when the lessac voice has been downloaded to the local models folder (it ships that way
/// on the dev machine; silently vacuous elsewhere).
/// </summary>
public class LiveEngineIntegrationTests
{
    private static readonly string ModelDir =
        Path.Combine(Paths.Models, "vits-piper-en_US-lessac-medium");

    private static bool VoicePresent =>
        Directory.Exists(ModelDir) && Directory.EnumerateFiles(ModelDir, "*.onnx").Any();

    [Fact]
    public async Task Real_Voice_Synthesizes_Spine_Sentences()
    {
        if (!VoicePresent) return; // vacuous pass without the 60 MB voice file

        var engine = new SherpaOnnxEngine();
        var sink = new FakeAudioSink();
        var voice = VoiceCatalog.Find("en_US-lessac-medium")!;
        await engine.LoadAsync(voice, ModelDir);

        Assert.True(engine.IsReady);
        Assert.Equal(22050, engine.SampleRate);

        var pipe = new SpeechPipeline(engine, sink);
        pipe.SetSpine(ReadingSpine.FromSentences(new[]
        {
            "LeafVox reads any P D F aloud.",
            "It is fast, it is free, and it never needs the internet.",
        }));

        var seen = new List<int>();
        var all = new TaskCompletionSource();
        pipe.SentenceStarted += (_, i) => { lock (seen) seen.Add(i); if (seen.Count == 2) all.TrySetResult(); };
        pipe.Play(0);
        await all.Task.WaitAsync(TimeSpan.FromSeconds(30));

        lock (seen) Assert.Equal(new[] { 0, 1 }, seen);
        Assert.Equal(2, sink.Written.Count);
        Assert.All(sink.Written, c => Assert.True(c.Samples.Length > 1000, "audio should be seconds long"));
        pipe.Stop();
        engine.Unload();
        await pipe.DisposeAsync();
    }
}
