using System.Diagnostics;

namespace LeafVox.Tts;

/// <summary>Downloads a catalog voice archive (resumable file name), extracts it with system tar, returns the model dir.</summary>
public sealed class VoiceDownloader
{
    public async Task<string> DownloadAsync(VoiceInfo voice, IProgress<double>? progress, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Paths.Models);
        var fileName = Path.GetFileName(new Uri(voice.ArchiveUrl).LocalPath); // *.tar.bz2
        var partPath = Path.Combine(Paths.Models, fileName + ".part");

        using (var http = new HttpClient())
        {
            http.Timeout = TimeSpan.FromMinutes(30);
            using var resp = await http.GetAsync(voice.ArchiveUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            var total = resp.Content.Headers.ContentLength ?? voice.ArchiveBytes;
            await using var src = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using var dst = new FileStream(partPath, FileMode.Create, FileAccess.Write, FileShare.None);
            var buffer = new byte[81920];
            long done = 0;
            int read;
            while ((read = await src.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
            {
                await dst.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                done += read;
                if (total > 0) progress?.Report(Math.Min(1.0, done / (double)total));
            }
        }
        progress?.Report(1.0);

        var extractDir = Paths.Models;
        await RunTarAsync(new[] { "-xjf", partPath, "-C", extractDir }, ct).ConfigureAwait(false);
        File.Delete(partPath);

        var stem = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(fileName)); // strip .tar.bz2
        var modelDir = Path.Combine(extractDir, stem);
        if (!Directory.Exists(modelDir))
        {
            // be tolerant: pick the newest directory that contains tokens.txt
            modelDir = Directory.EnumerateDirectories(extractDir)
                .Where(d => File.Exists(Path.Combine(d, "tokens.txt")))
                .OrderByDescending(Directory.GetLastWriteTimeUtc)
                .FirstOrDefault() ?? modelDir;
        }
        if (!Directory.Exists(modelDir))
            throw new LeafVoxTtsException($"Extraction of {fileName} did not produce a model directory.");
        return modelDir;
    }

    private static async Task RunTarAsync(string[] args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo("tar")
        {
            ArgumentList = { },
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var proc = Process.Start(psi) ?? throw new LeafVoxTtsException("Could not start tar.");
        var err = await proc.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
        await proc.WaitForExitAsync(ct).ConfigureAwait(false);
        if (proc.ExitCode != 0)
            throw new LeafVoxTtsException($"tar failed (exit {proc.ExitCode}): {err}");
    }
}
