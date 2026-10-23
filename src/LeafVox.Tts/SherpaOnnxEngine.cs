using SherpaOnnx;

namespace LeafVox.Tts;

/// <summary>sherpa-onnx-backed engine (Piper/Mimic3 VITS family + Kokoro). CPU inference only.</summary>
public sealed class SherpaOnnxEngine : ITtsEngine
{
    private OfflineTts? _tts;
    private VoiceInfo? _voice;
    private readonly object _gate = new();

    public bool IsReady { get { lock (_gate) return _tts is not null; } }
    public int SampleRate { get { lock (_gate) return _tts?.SampleRate ?? 0; } }
    public string? ActiveVoiceId { get { lock (_gate) return _voice?.Id; } }

    public Task LoadAsync(VoiceInfo voice, string modelDir, CancellationToken ct = default)
        => Task.Run(() =>
        {
            var (model, tokens, dataDir, voices, lexicon) = LocateFiles(modelDir);
            var config = new OfflineTtsConfig();
            config.Model.NumThreads = Math.Max(2, Environment.ProcessorCount / 2);
            config.Model.Debug = 0;
            config.Model.Provider = "cpu";

            switch (voice.Kind)
            {
                case ModelKind.Kokoro:
                    config.Model.Kokoro.Model = model;
                    config.Model.Kokoro.Voices = voices ?? throw new LeafVoxTtsException($"voices.bin missing for {voice.Id}");
                    config.Model.Kokoro.Tokens = tokens;
                    config.Model.Kokoro.DataDir = dataDir;
                    if (lexicon is not null) config.Model.Kokoro.Lexicon = lexicon;
                    break;
                default: // Piper & Mimic3 are VITS checkpoints
                    config.Model.Vits.Model = model;
                    config.Model.Vits.Tokens = tokens;
                    config.Model.Vits.DataDir = dataDir;
                    if (lexicon is not null) config.Model.Vits.Lexicon = lexicon;
                    break;
            }

            var tts = new OfflineTts(config);
            lock (_gate)
            {
                _tts?.Dispose();
                _tts = tts;
                _voice = voice;
            }
        }, ct);

    public void Unload()
    {
        lock (_gate)
        {
            _tts?.Dispose();
            _tts = null;
            _voice = null;
        }
    }

    public Task<PcmChunk> SynthesizeAsync(string text, float speed, int speakerId, CancellationToken ct = default)
        => Task.Run(() =>
        {
            OfflineTts tts;
            lock (_gate)
            {
                tts = _tts ?? throw new LeafVoxTtsException("No voice loaded.");
            }
            var audio = tts.Generate(text, speed, speakerId);
            ct.ThrowIfCancellationRequested();
            return new PcmChunk(audio.Samples, tts.SampleRate);
        }, ct);

    /// <summary>Finds model files in an extracted archive folder, tolerant of the top-level dir name.</summary>
    private static (string Model, string Tokens, string DataDir, string? Voices, string? Lexicon) LocateFiles(string modelDir)
    {
        var root = Directory.Exists(modelDir) ? modelDir : Path.GetDirectoryName(modelDir)!;
        if (!Directory.Exists(root)) throw new LeafVoxTtsException($"Model directory not found: {modelDir}");
        var searchDir = Directory.Exists(Path.Combine(modelDir, "espeak-ng-data")) || File.Exists(FindOnnx(modelDir))
            ? modelDir
            : Directory.EnumerateDirectories(root).FirstOrDefault(d => Directory.Exists(Path.Combine(d, "espeak-ng-data")) || HasOnnx(d)) ?? modelDir;

        var model = FindOnnx(searchDir) ?? throw new LeafVoxTtsException($"No .onnx model found under {searchDir}");
        var tokens = Path.Combine(searchDir, "tokens.txt");
        var dataDir = Path.Combine(searchDir, "espeak-ng-data");
        var voices = File.Exists(Path.Combine(searchDir, "voices.bin")) ? Path.Combine(searchDir, "voices.bin") : null;
        string? lexicon = Directory.EnumerateFiles(searchDir, "lexicon*.txt").FirstOrDefault()
                          ?? (File.Exists(Path.Combine(searchDir, "lexicon.txt")) ? Path.Combine(searchDir, "lexicon.txt") : null);
        return (model, tokens, dataDir, voices, lexicon);
    }

    private static bool HasOnnx(string dir) => FindOnnx(dir) is not null;

    private static string? FindOnnx(string dir)
    {
        if (!Directory.Exists(dir)) return null;
        var onnx = Directory.EnumerateFiles(dir, "*.onnx")
            .Where(f => !Path.GetFileName(f).StartsWith("vocos", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Length)
            .ToList();
        if (onnx.Count > 0) return onnx[0];
        foreach (var sub in Directory.EnumerateDirectories(dir))
        {
            var found = FindOnnx(sub);
            if (found is not null) return found;
        }
        return null;
    }
}
