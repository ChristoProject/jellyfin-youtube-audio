using System.Text.Json;
using Jellyfin.Plugin.YouTubeAudio.Download;
using Jellyfin.Plugin.YouTubeAudio.Models;

namespace Jellyfin.Plugin.YouTubeAudio.Services;

public sealed class JsonRequestStore : IRequestStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private readonly List<DownloadRequest> _requests;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public JsonRequestStore(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        _path = Path.Combine(dataDirectory, "requests.json");
        _requests = Load();
        foreach (var request in _requests)
            foreach (var item in request.Items.Where(x => x.Status == DownloadStatus.Processing))
            { item.Status = DownloadStatus.Pending; item.Progress = 0; }
    }

    public IReadOnlyList<DownloadRequest> GetAll() { lock (_gate) return _requests.ToList(); }
    public DownloadRequest? Get(Guid id) { lock (_gate) return _requests.FirstOrDefault(x => x.Id == id); }
    public void Add(DownloadRequest request) { lock (_gate) { _requests.Add(request); Persist(); } }
    public void Save(DownloadRequest request) { lock (_gate) { Persist(); } }

    private List<DownloadRequest> Load()
    {
        try { return File.Exists(_path) ? JsonSerializer.Deserialize<List<DownloadRequest>>(File.ReadAllText(_path), Options) ?? [] : []; }
        catch (JsonException) { return []; }
    }
    private void Persist()
    {
        var temporary = _path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(_requests, Options));
        File.Move(temporary, _path, true);
    }
}
