using LeafVox.Core;

namespace LeafVox.Tts.Tests;

public class SpeechPipelineTests
{
    private static (SpeechPipeline pipe, FakeTtsEngine engine, FakeAudioSink sink) NewPipeline(int sentences = 5)
    {
        var engine = new FakeTtsEngine();
        var sink = new FakeAudioSink();
        var pipe = new SpeechPipeline(engine, sink);
        pipe.SetSpine(ReadingSpine.FromSentences(
            Enumerable.Range(0, sentences).Select(i => $"Sentence number {i}.").ToList()));
        return (pipe, engine, sink);
    }

    private static async Task<List<int>> CollectStarted(SpeechPipeline pipe, int count, int ms = 3000)
    {
        var tcs = new TaskCompletionSource<List<int>>();
        var seen = new List<int>();
        pipe.SentenceStarted += (_, i) =>
        {
            lock (seen)
            {
                seen.Add(i);
                if (seen.Count >= count) tcs.TrySetResult(seen.ToList());
            }
        };
        var done = await Task.WhenAny(tcs.Task, Task.Delay(ms));
        return done == tcs.Task ? tcs.Task.Result : throw new TimeoutException($"only saw {string.Join(",", seen)}");
    }

    [Fact]
    public async Task Plays_All_Sentences_In_Order_Then_Stops()
    {
        var (pipe, engine, sink) = NewPipeline(4);
        var finished = new TaskCompletionSource();
        pipe.Finished += (_, _) => finished.TrySetResult();
        var seen = CollectStarted(pipe, 4);
        pipe.Play(0);
        Assert.Equal(new[] { 0, 1, 2, 3 }, await seen);
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(SpeechState.Stopped, pipe.State);
        Assert.Equal(4, sink.Written.Count);
        Assert.Equal(4, engine.Synthesized.Count);
        await pipe.DisposeAsync();
    }

    [Fact]
    public async Task Seek_Events_Resume_At_Target()
    {
        var (pipe, _, _) = NewPipeline(8);
        var seen = CollectStarted(pipe, 3);
        pipe.Play(5);
        Assert.Equal(new[] { 5, 6, 7 }, await seen);
        pipe.Stop();
        await pipe.DisposeAsync();
    }

    [Fact]
    public async Task Long_Sentence_Produces_Multiple_Chunks_One_Event()
    {
        var engine = new FakeTtsEngine();
        var sink = new FakeAudioSink();
        var pipe = new SpeechPipeline(engine, sink);
        var longOne = string.Join(" ", Enumerable.Repeat("lorem", 60)); // > 120 chars
        pipe.SetSpine(ReadingSpine.FromSentences(new[] { longOne, "Short one." }));
        var seen = CollectStarted(pipe, 2);
        pipe.Play(0);
        Assert.Equal(new[] { 0, 1 }, await seen);
        Assert.True(engine.Synthesized.Count >= 3, "long sentence must be chunked for synthesis");
        pipe.Stop();
        await pipe.DisposeAsync();
    }

    [Fact]
    public async Task Pause_Resume_Routes_To_Sink()
    {
        var (pipe, _, sink) = NewPipeline(200); // long enough to still be playing
        var seen = CollectStarted(pipe, 1);
        pipe.Play(0);
        await seen;
        pipe.Pause();
        Assert.Equal(SpeechState.Paused, pipe.State);
        Assert.Equal(1, sink.PauseCount);
        pipe.Resume();
        Assert.Equal(SpeechState.Playing, pipe.State);
        Assert.Equal(1, sink.ResumeCount);
        pipe.Stop();
        await pipe.DisposeAsync();
    }

    [Fact]
    public async Task Speed_Is_Passed_To_Engine()
    {
        var (pipe, engine, _) = NewPipeline(2);
        pipe.Speed = 1.75f;
        var seen = CollectStarted(pipe, 1);
        pipe.Play(0);
        await seen;
        Assert.Contains(1.75f, engine.SpeedsSeen);
        pipe.Stop();
        await pipe.DisposeAsync();
    }

    [Fact]
    public async Task PlayOnce_Stops_After_One_Sentence()
    {
        var (pipe, engine, _) = NewPipeline(60);
        var seen = new List<int>();
        var done = new TaskCompletionSource();
        pipe.SentenceStarted += (_, i) => { lock (seen) seen.Add(i); };
        pipe.SentenceFinished += (_, i) => { if (i == 3) done.TrySetResult(); };
        pipe.PlayOnce(3);
        await done.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(150); // let any stray batches slip through if buggy
        pipe.Stop();
        lock (seen) Assert.Equal(new[] { 3 }, seen);
        Assert.True(engine.Synthesized.Count <= 3, "must not synthesize far ahead of a single line");
        await pipe.DisposeAsync();
    }

    [Fact]
    public async Task Engine_Failure_Raises_Failed_Event()
    {
        var (pipe, engine, _) = NewPipeline(2);
        engine.ThrowOnNext = true;
        var failed = new TaskCompletionSource<Exception>();
        pipe.Failed += (_, ex) => failed.TrySetResult(ex);
        pipe.Play(0);
        var ex = await failed.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal("synth boom", ex.Message);
        await pipe.DisposeAsync();
    }

    [Fact]
    public async Task Stop_Cancels_Future_Events()
    {
        var (pipe, engine, _) = NewPipeline(200);
        var seen = new List<int>();
        pipe.SentenceStarted += (_, i) => { lock (seen) seen.Add(i); };
        pipe.Play(0);
        await Task.Delay(120);
        pipe.Stop();
        int afterStop;
        lock (seen) afterStop = seen.Count;
        await Task.Delay(200);
        lock (seen) Assert.Equal(afterStop, seen.Count);
        Assert.Equal(SpeechState.Stopped, pipe.State);
        await pipe.DisposeAsync();
    }
}
