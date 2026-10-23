using NAudio.Wave;

namespace LeafVox.Tts;

/// <summary>Wraps a BufferedWaveProvider and counts bytes the output device actually pulled.</summary>
public sealed class CountingProvider : IWaveProvider
{
    private readonly BufferedWaveProvider _inner;
    private long _consumedBytes;

    public CountingProvider(WaveFormat waveFormat)
    {
        _inner = new BufferedWaveProvider(waveFormat);
    }

    public WaveFormat WaveFormat => _inner.WaveFormat;
    public BufferedWaveProvider Buffer => _inner;

    public int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public int Read(Span<byte> destination)
    {
        int n = _inner.Read(destination);
        if (n > 0) Interlocked.Add(ref _consumedBytes, n);
        return n;
    }

    public double ConsumedSeconds(int bytesPerSecond) =>
        bytesPerSecond <= 0 ? 0 : Interlocked.Read(ref _consumedBytes) / (double)bytesPerSecond;

    public void ResetCounter() => Interlocked.Exchange(ref _consumedBytes, 0);
}
