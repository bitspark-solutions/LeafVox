namespace LeafVox.Tts;

/// <summary>Float32 mono PCM produced by an engine.</summary>
public sealed record PcmChunk(float[] Samples, int SampleRate)
{
    public double DurationSeconds => SampleRate <= 0 ? 0 : (double)Samples.Length / SampleRate;
}
