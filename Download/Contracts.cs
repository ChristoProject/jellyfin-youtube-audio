using Jellyfin.Plugin.YouTubeAudio.Models;

namespace Jellyfin.Plugin.YouTubeAudio.Download;

public sealed record SourceItem(string Url, string Title, int Index);
public interface IDownloadEngine
{
    Task<IReadOnlyList<SourceItem>> AnalyzeAsync(string url, CancellationToken cancellationToken);
    Task DownloadAsync(SourceItem source, string destination, ContentCategory category, IProgress<int> progress, CancellationToken cancellationToken);
}

public interface IRequestStore
{
    IReadOnlyList<DownloadRequest> GetAll();
    DownloadRequest? Get(Guid id);
    void Add(DownloadRequest request);
    void Save(DownloadRequest request);
}

public interface IAudioFileWriter
{
    string ReservePath(string directory, string title, ContentCategory category);
}

public interface ILibraryRefresher
{
    Task RefreshAsync(CancellationToken cancellationToken);
}
