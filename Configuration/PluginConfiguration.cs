using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.YouTubeAudio.Configuration;

public sealed class PluginConfiguration : BasePluginConfiguration
{
    public const string DefaultMusicDestination = "/Media/Musica";
    public const string DefaultPodcastDestination = "/Media/Podcast";

    public string MusicDestination { get; set; } = DefaultMusicDestination;
    public string PodcastDestination { get; set; } = DefaultPodcastDestination;
}
