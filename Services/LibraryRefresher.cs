using MediaBrowser.Controller.Library;
using Jellyfin.Plugin.YouTubeAudio.Download;

namespace Jellyfin.Plugin.YouTubeAudio.Services;

public sealed class LibraryRefresher : ILibraryRefresher
{
    private readonly ILibraryManager _libraryManager;
    public LibraryRefresher(ILibraryManager libraryManager) => _libraryManager = libraryManager;
    public Task RefreshAsync(CancellationToken cancellationToken)
    { _libraryManager.QueueLibraryScan(); return Task.CompletedTask; }
}
