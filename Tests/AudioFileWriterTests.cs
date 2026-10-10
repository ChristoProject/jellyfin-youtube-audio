using Xunit;
using Jellyfin.Plugin.YouTubeAudio.Models;
using Jellyfin.Plugin.YouTubeAudio.Services;

namespace Jellyfin.Plugin.YouTubeAudio.Tests;

public sealed class AudioFileWriterTests
{
    [Fact]
    public void Sanitizes_invalid_characters_and_versions_existing_files()
    {
        var directory = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var writer = new AudioFileWriter();
            var first = writer.ReservePath(directory, "video: test?", ContentCategory.Podcast);
            File.WriteAllText(first, "existing");
            var second = writer.ReservePath(directory, "video: test?", ContentCategory.Podcast);
            Assert.EndsWith("video_ test_(1).mp3", second);
            Assert.NotEqual(first, second);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void Selects_opus_for_music()
    {
        var directory = Directory.CreateTempSubdirectory().FullName;
        try { Assert.EndsWith(".opus", new AudioFileWriter().ReservePath(directory, "track", ContentCategory.Music)); }
        finally { Directory.Delete(directory, true); }
    }
}
