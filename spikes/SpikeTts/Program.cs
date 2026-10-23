// Spike B: prove sherpa-onnx C# synthesizes a natural voice WAV fully offline on this machine.
using SherpaOnnx;

var modelDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "leafvox", "models", "vits-piper-en_US-lessac-medium");
if (!Directory.Exists(modelDir))
{
    Console.WriteLine($"MODEL MISSING at {modelDir}");
    return 2;
}

var config = new OfflineTtsConfig();
config.Model.Vits.Model = Path.Combine(modelDir, "en_US-lessac-medium.onnx");
config.Model.Vits.Tokens = Path.Combine(modelDir, "tokens.txt");
config.Model.Vits.DataDir = Path.Combine(modelDir, "espeak-ng-data");
config.Model.NumThreads = 2;
config.Model.Debug = 0;
config.Model.Provider = "cpu";

var sw = System.Diagnostics.Stopwatch.StartNew();
var tts = new OfflineTts(config);
Console.WriteLine($"model loaded in {sw.Elapsed.TotalMilliseconds:F0} ms, sampleRate={tts.SampleRate}");

var text = "LeafVox reads any P D F aloud. It is fast, it is free, and it never needs the internet.";
sw.Restart();
var audio = tts.Generate(text, 1.0f, 0);
var synthMs = sw.Elapsed.TotalMilliseconds;
var audioSec = (double)audio.Samples.Length / tts.SampleRate;
Console.WriteLine($"synth: {synthMs:F0} ms for {audioSec:F2} s audio, RTF={synthMs / 1000.0 / audioSec:F3} (lower=faster)");

var outPath = Path.Combine(Path.GetTempPath(), "leafvox-spike-b.wav");
if (!audio.SaveToWaveFile(outPath)) { Console.WriteLine("WAV save failed"); return 1; }
Console.WriteLine($"WAV written: {outPath} ({new FileInfo(outPath).Length} bytes)");

// Streaming callback path (the pipeline we'll actually use in Phase 2)
int chunks = 0;
float seenProgress = -1;
var cb = new OfflineTtsCallbackProgressWithArg((IntPtr samples, int n, float progress, IntPtr arg) =>
{
    chunks++;
    seenProgress = progress;
    return 1;
});
sw.Restart();
var audio2 = tts.GenerateWithConfig(text, new OfflineTtsGenerationConfig { Sid = 0, Speed = 1.5f }, cb);
Console.WriteLine($"streamed: {chunks} chunks, final progress={seenProgress:F2}, 1.5x speed synth {sw.Elapsed.TotalMilliseconds:F0} ms / {(double)audio2.Samples.Length / tts.SampleRate:F2} s");

Console.WriteLine("SPIKE B RESULT: PASS");
return 0;
