using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.YouTubeAudio.Configuration;
using Jellyfin.Plugin.YouTubeAudio.Download;
using Jellyfin.Plugin.YouTubeAudio.Models;

namespace Jellyfin.Plugin.YouTubeAudio.Services;

public sealed class DownloadWorker : BackgroundService
{
    private readonly IRequestStore _store;
    private readonly IDownloadEngine _engine;
    private readonly IAudioFileWriter _writer;
    private readonly ILibraryRefresher _library;
    private readonly ILogger<DownloadWorker> _logger;
    public DownloadWorker(IRequestStore store, IDownloadEngine engine, IAudioFileWriter writer, ILibraryRefresher library, ILogger<DownloadWorker> logger)
    { _store = store; _engine = engine; _writer = writer; _library = library; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { foreach (var request in _store.GetAll().Where(x => !x.IsFinished)) await ProcessAsync(request, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception) { _logger.LogError(exception, "Errore nel worker YouTube Audio"); }
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task ProcessAsync(DownloadRequest request, CancellationToken token)
    {
        if (request.Items.Count == 0)
        {
            try
            {
                var sources = await _engine.AnalyzeAsync(request.Url, token);
                request.Items.AddRange(sources.Select((source, index) => new QueueItem { Id = Guid.NewGuid(), RequestId = request.Id, SourceUrl = source.Url, Title = source.Title, Index = index, Category = request.Category, Status = DownloadStatus.Pending, Progress = 0 }));
                _store.Save(request);
            }
            catch (Exception exception)
            { AddFailure(request, "Analisi non riuscita: " + SafeError(exception)); }
            return;
        }

        var item = request.Items.OrderBy(x => x.Index).FirstOrDefault(x => x.Status is DownloadStatus.Pending or DownloadStatus.Processing);
        if (item is not null)
        {
            var destination = Destination(request.Category);
            if (string.IsNullOrWhiteSpace(destination) || !Directory.Exists(destination) || !CanWrite(destination))
            { Fail(item, "La destinazione configurata non esiste o non è scrivibile."); _store.Save(request); }
            else
            {
                item.Status = DownloadStatus.Processing; item.Progress = 0; _store.Save(request);
                try
                {
                    var finalPath = _writer.ReservePath(destination, item.Title, item.Category);
                    await _engine.DownloadAsync(new SourceItem(item.SourceUrl, item.Title, item.Index), finalPath, item.Category, new Progress<int>(p => { item.Progress = p; _store.Save(request); }), token);
                    item.FinalPath = finalPath; item.Progress = 100; item.Status = DownloadStatus.Completed;
                }
                catch (Exception exception) { Fail(item, SafeError(exception)); }
                _store.Save(request);
            }
        }
        if (request.IsFinished && !request.LibraryRefreshRequested)
        { request.LibraryRefreshRequested = true; _store.Save(request); await _library.RefreshAsync(token); }
    }

    private string Destination(ContentCategory category) => category == ContentCategory.Music ? Plugin.Instance?.Configuration.MusicDestination ?? "" : Plugin.Instance?.Configuration.PodcastDestination ?? "";
    private static bool CanWrite(string path) { try { using var file = File.Open(Path.Combine(path, ".youtube-audio-write-test"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read); file.Close(); File.Delete(Path.Combine(path, ".youtube-audio-write-test")); return true; } catch { return false; } }
    private static void Fail(QueueItem item, string error) { item.Status = DownloadStatus.Failed; item.Error = error; item.Progress = 0; }
    private static void AddFailure(DownloadRequest request, string error) { request.Items.Add(new QueueItem { Id = Guid.NewGuid(), RequestId = request.Id, Title = "Sorgente non valida", Index = 0, Category = request.Category, Status = DownloadStatus.Failed, Error = error, Progress = 0 }); }
    private static string SafeError(Exception exception) => exception is OperationCanceledException ? "Operazione annullata o scaduta." : exception.Message.Length > 500 ? exception.Message[..500] : exception.Message;
}
