using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Jellyfin.Plugin.YouTubeAudio.Download;
using Jellyfin.Plugin.YouTubeAudio.Models;

namespace Jellyfin.Plugin.YouTubeAudio.Services;

/// Adapter for the binaries shipped beside the plugin. Arguments are passed directly to Process.
public sealed class YtDlpDownloadEngine : IDownloadEngine
{
    private readonly string _executable;
    private readonly string _ffmpegDirectory;
    private readonly TimeSpan _timeout;
    public YtDlpDownloadEngine(string executable, string ffmpegDirectory, TimeSpan? timeout = null)
    { _executable = executable; _ffmpegDirectory = ffmpegDirectory; _timeout = timeout ?? TimeSpan.FromHours(2); }

    public async Task<IReadOnlyList<SourceItem>> AnalyzeAsync(string url, CancellationToken cancellationToken)
    {
        ValidateUrl(url);
        var output = await RunAsync(["--flat-playlist", "--dump-single-json", "--no-warnings", url], cancellationToken);
        using var json = JsonDocument.Parse(output);
        var root = json.RootElement;
        var result = new List<SourceItem>();
        if (root.TryGetProperty("entries", out var entries) && entries.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var entry in entries.EnumerateArray())
            {
                if (!entry.TryGetProperty("id", out var id)) continue;
                var source = entry.TryGetProperty("url", out var u) ? u.GetString() : null;
                source ??= "https://www.youtube.com/watch?v=" + id.GetString();
                var title = entry.TryGetProperty("title", out var t) ? t.GetString() : null;
                result.Add(new SourceItem(source!, title ?? source!, index++));
            }
        }
        else
        {
            var source = root.TryGetProperty("webpage_url", out var page) ? page.GetString() : url;
            var title = root.TryGetProperty("title", out var titleElement) ? titleElement.GetString() : source;
            result.Add(new SourceItem(source!, title ?? source!, 0));
        }
        if (result.Count == 0) throw new InvalidOperationException("La sorgente non contiene video pubblici.");
        return result;
    }

    public async Task DownloadAsync(SourceItem source, string destination, ContentCategory category, IProgress<int> progress, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temporary = destination + ".part";
        var format = category == ContentCategory.Music ? "opus" : "mp3";
        try
        {
            await RunAsync(["--no-playlist", "--extract-audio", "--audio-format", format, "--ffmpeg-location", _ffmpegDirectory, "--newline", "--progress", "-o", temporary, source.Url], progress, cancellationToken);
            var produced = Directory.GetFiles(Path.GetDirectoryName(destination)!, Path.GetFileName(temporary) + "*")
                .OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
            if (produced is null) throw new InvalidOperationException("yt-dlp non ha prodotto una traccia audio.");
            File.Move(produced, destination, false);
            progress.Report(100);
        }
        catch { foreach (var file in Directory.GetFiles(Path.GetDirectoryName(destination)!, Path.GetFileName(temporary) + "*")) TryDelete(file); throw; }
    }

    private async Task<string> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken) => await RunAsync(args, null, cancellationToken);
    private async Task<string> RunAsync(IReadOnlyList<string> args, IProgress<int>? progress, CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = new ProcessStartInfo(_executable) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true } };
        foreach (var arg in args) process.StartInfo.ArgumentList.Add(arg);
        if (!process.Start()) throw new InvalidOperationException("yt-dlp non è disponibile o non è eseguibile.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);
        var output = new System.Text.StringBuilder();
        try
        {
            var stdout = Task.Run(async () => { while (await process.StandardOutput.ReadLineAsync(timeout.Token) is { } line) { output.AppendLine(line); ParseProgress(line, progress); } }, timeout.Token);
            var error = await process.StandardError.ReadToEndAsync(timeout.Token);
            await stdout;
            await process.WaitForExitAsync(timeout.Token);
            if (process.ExitCode != 0) throw new InvalidOperationException(Sanitize(error));
        }
        catch { TryKill(process); throw; }
        return output.ToString();
    }

    private static void ParseProgress(string line, IProgress<int>? progress)
    {
        var marker = line.IndexOf('%');
        if (marker < 1) return;
        var start = line.LastIndexOf(' ', marker - 1) + 1;
        if (double.TryParse(line[start..marker].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            progress?.Report(Math.Clamp((int)Math.Round(value), 0, 100));
    }
    private static void ValidateUrl(string url)
    { if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !(uri.Host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".youtube.com", StringComparison.OrdinalIgnoreCase) || uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("URL YouTube non valida."); }
    private static string Sanitize(string value) => string.IsNullOrWhiteSpace(value) ? "Errore durante l'acquisizione." : value.Length > 500 ? value[..500] : value;
    private static void TryKill(Process p) { try { if (!p.HasExited) p.Kill(true); } catch { } }
    private static void TryDelete(string path) { try { File.Delete(path); } catch { } }
}
