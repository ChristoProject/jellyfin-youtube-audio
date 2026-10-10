using Microsoft.Extensions.DependencyInjection;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Jellyfin.Plugin.YouTubeAudio.Download;

namespace Jellyfin.Plugin.YouTubeAudio.Services;

public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection services, IServerApplicationHost applicationHost)
    {
        services.AddSingleton<IRequestStore>(provider =>
        {
            var paths = provider.GetRequiredService<IApplicationPaths>();
            return new JsonRequestStore(Path.Combine(paths.DataPath, "youtube-audio"));
        });
        services.AddSingleton<IAudioFileWriter, AudioFileWriter>();
        services.AddSingleton<IDownloadEngine>(provider =>
        {
            var paths = provider.GetRequiredService<IApplicationPaths>();
            var nativeDirectory = Path.Combine(paths.PluginsPath, "YouTubeAudio", "NativeAssets", "linux-x64");
            var executable = Path.Combine(nativeDirectory, OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp");
            var ffmpegDirectory = nativeDirectory;
            return new YtDlpDownloadEngine(executable, ffmpegDirectory);
        });
        services.AddSingleton<ILibraryRefresher, LibraryRefresher>();
        services.AddHostedService<DownloadWorker>();
    }
}
