using System.Text;
using Jellyfin.Plugin.YouTubeAudio.Download;
using Jellyfin.Plugin.YouTubeAudio.Models;

namespace Jellyfin.Plugin.YouTubeAudio.Services;

public sealed class AudioFileWriter : IAudioFileWriter
{
    public string ReservePath(string directory, string title, ContentCategory category)
    {
        Directory.CreateDirectory(directory);
        var invalid = new HashSet<char>(Path.GetInvalidFileNameChars().Concat("<>:\"/\\|?*"));
        var builder = new StringBuilder(title.Trim());
        for (var i = 0; i < builder.Length; i++)
            if (invalid.Contains(builder[i])) builder[i] = '_';
        var baseName = builder.ToString().Trim().TrimEnd('.') is { Length: > 0 } value ? value : "audio";
        if (baseName is "." or "..") baseName = "audio";
        if (baseName.Length > 180) baseName = baseName[..180].TrimEnd(' ', '.');
        var extension = category == ContentCategory.Music ? ".opus" : ".mp3";
        var path = Path.Combine(directory, baseName + extension);
        var suffix = 1;
        while (File.Exists(path))
            path = Path.Combine(directory, $"{baseName}({suffix++}){extension}");
        return path;
    }
}
