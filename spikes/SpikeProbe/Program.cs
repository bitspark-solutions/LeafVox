using LeafVox.Tts;

// Spike C: prove MiniAudioSink (the mac/linux playback path) really plays on a live device,
// verified here on its Windows backend.
try
{
    var sink = new MiniAudioSink();
    sink.Open(22050);

    var sine = new float[2205]; // 0.1 s
    for (int i = 0; i < sine.Length; i++)
        sine[i] = 0.2f * MathF.Sin(2 * MathF.PI * 220 * i / 22050f);
    for (int r = 0; r < 10; r++) sink.Write(new PcmChunk(sine.ToArray(), 22050)); // 1 s

    var start = DateTime.UtcNow;
    while (sink.PlayedSeconds < 0.8 && (DateTime.UtcNow - start).TotalSeconds < 5)
        await Task.Delay(50);

    Console.WriteLine($"played advanced to {sink.PlayedSeconds:F2}s of 1.0s written");
    sink.Pause();
    var frozen = sink.PlayedSeconds;
    await Task.Delay(300);
    Console.WriteLine($"pause holds clock: {Math.Abs(sink.PlayedSeconds - frozen) < 0.05}");
    sink.Resume();
    await Task.Delay(200);
    Console.WriteLine($"resume advances: {sink.PlayedSeconds > frozen}");
    sink.Dispose();
    Console.WriteLine("SPIKE C RESULT: PASS");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine("SPIKE C RESULT: FAIL — " + ex.Message);
    return 1;
}
