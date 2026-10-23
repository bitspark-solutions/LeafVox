using System.Diagnostics;
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
        pipe.SentenceStarted += (_, i) => { lock (seen) seen.Add(i); };
        pipe.SentenceFinished += (_, i) => { lock (seen) if (i == 1) all.TrySetResult(); };
        pipe.Play(0);
        await all.Task.WaitAsync(TimeSpan.FromSeconds(30));

        lock (seen) Assert.Contains(0, seen);
        Assert.Contains(1, seen);
        Assert.True(sink.Written.Count >= 2);
        Assert.All(sink.Written, c => Assert.True(c.Samples.Length > 1000, "audio should be seconds long"));
        pipe.Stop();
        engine.Unload();
        await pipe.DisposeAsync();
    }

    [Fact]
    public async Task Kokoro_Natural_Voice_Synthesizes_Real_Audio()
    {
        var kokoroDir = Path.Combine(Paths.Models, "kokoro-en-v0_19");
        if (!File.Exists(Path.Combine(kokoroDir, "voices.bin"))) return; // vacuous pass without the 320 MB model

        var engine = new SherpaOnnxEngine();
        var sink = new FakeAudioSink();
        var voice = VoiceCatalog.Find("kokoro-en-v0_19")!;

        var load = Stopwatch.StartNew();
        await engine.LoadAsync(voice, kokoroDir);
        load.Stop();
        Assert.True(engine.IsReady);

        var pipe = new SpeechPipeline(engine, sink);
        pipe.SetSpine(ReadingSpine.FromSentences(new[]
        {
            "LeafVox reads any P D F aloud. It is fast, it is free, and it never needs the internet.",
        }));
        var done = new TaskCompletionSource();
        pipe.SentenceFinished += (_, i) => { if (i == 0) done.TrySetResult(); };
        var sw = Stopwatch.StartNew();
        pipe.Play(0);
        await done.Task.WaitAsync(TimeSpan.FromSeconds(60));
        sw.Stop();

        double audioSec = sink.Written.Sum(c => c.DurationSeconds);
        Assert.True(audioSec > 4, $"expected seconds of audio, got {audioSec:F2}");
        Assert.True(sw.Elapsed.TotalSeconds / audioSec < 3.0,
            $"Kokoro must synthesize faster than 3x realtime on CPU (was {sw.Elapsed.TotalSeconds / audioSec:F2})");
        pipe.Stop();
        engine.Unload();
        await pipe.DisposeAsync();
    }
}
