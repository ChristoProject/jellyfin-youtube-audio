using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Jellyfin.Plugin.YouTubeAudio.Configuration;

namespace Jellyfin.Plugin.YouTubeAudio;

public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public static Plugin? Instance { get; private set; }

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer) => Instance = this;

    public override string Name => "YouTube Audio Downloader";
    public override Guid Id => Guid.Parse("8c1c54f0-2df1-4e8d-9e34-0c5d4f2cbb20");

    public IEnumerable<PluginPageInfo> GetPages() =>
    [
        new PluginPageInfo
        {
            Name = Name,
            EmbeddedResourcePath = "Jellyfin.Plugin.YouTubeAudio.Configuration.configPage.html"
        }
    ];
}
