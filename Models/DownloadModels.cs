namespace Jellyfin.Plugin.YouTubeAudio.Models;

public enum ContentCategory { Music, Podcast }
public enum DownloadStatus { Pending, Processing, Completed, Failed }

public sealed class DownloadRequest
{
    public Guid Id { get; init; }
    public string Url { get; init; } = string.Empty;
    public ContentCategory Category { get; init; }
    public List<QueueItem> Items { get; init; } = [];
    public bool LibraryRefreshRequested { get; set; }
    public bool IsFinished => Items.Count > 0 && Items.All(x => x.Status is DownloadStatus.Completed or DownloadStatus.Failed);
}

public sealed class QueueItem
{
    public Guid Id { get; init; }
    public Guid RequestId { get; init; }
    public string SourceUrl { get; set; } = string.Empty;
    public string Title { get; set; } = "In attesa";
    public int Index { get; init; }
    public ContentCategory Category { get; init; }
    public DownloadStatus Status { get; set; }
    public int Progress { get; set; }
    public string? Error { get; set; }
    public string? FinalPath { get; set; }
}

public sealed record CreateRequestDto(string Url, ContentCategory? Category);
public sealed record RequestSummary(Guid Id, string Url, ContentCategory Category, IReadOnlyList<QueueItem> Items, bool Finished);
