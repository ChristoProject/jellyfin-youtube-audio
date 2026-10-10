using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.YouTubeAudio.Configuration;

public sealed class PluginConfiguration : BasePluginConfiguration
{
    public string MusicDestination { get; set; } = string.Empty;
    public string PodcastDestination { get; set; } = string.Empty;
}
