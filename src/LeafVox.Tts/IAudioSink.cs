namespace LeafVox.Tts;

/// <summary>Playback seam (WASAPI/DirectSound on Windows; miniaudio etc. on other OSes).</summary>
public interface IAudioSink : IDisposable
{
    void Open(int sampleRate);
    void Write(PcmChunk chunk);
    void Pause();
    void Resume();
    void Stop();
}
