using Microsoft.AspNetCore.Mvc;
using Jellyfin.Plugin.YouTubeAudio.Configuration;
using Jellyfin.Plugin.YouTubeAudio.Download;
using Jellyfin.Plugin.YouTubeAudio.Models;

namespace Jellyfin.Plugin.YouTubeAudio.Controllers;

[ApiController]
[Route("YouTubeAudio")]
public sealed class YouTubeAudioController : ControllerBase
{
    private readonly IRequestStore _store;
    public YouTubeAudioController(IRequestStore store) => _store = store;

    [HttpPost("requests")]
    public ActionResult<Guid> Create([FromBody] CreateRequestDto input)
    {
        if (input.Category is null) return BadRequest("La categoria è obbligatoria.");
        if (string.IsNullOrWhiteSpace(input.Url)) return BadRequest("L'URL è obbligatorio.");
        if (!Uri.TryCreate(input.Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !(uri.Host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".youtube.com", StringComparison.OrdinalIgnoreCase) || uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase))) return BadRequest("Inserire un URL YouTube pubblico valido.");
        var destination = input.Category == ContentCategory.Music
            ? PluginConfiguration.DefaultMusicDestination
            : PluginConfiguration.DefaultPodcastDestination;
        if (!Directory.Exists(destination)) return BadRequest($"La destinazione {destination} non esiste nel container Jellyfin.");
        var request = new DownloadRequest { Id = Guid.NewGuid(), Url = input.Url, Category = input.Category.Value };
        _store.Add(request);
        return Accepted(request.Id);
    }

    [HttpGet("requests")]
    public ActionResult<IReadOnlyList<RequestSummary>> GetAll() => Ok(_store.GetAll().Select(ToSummary).ToList());

    [HttpGet("requests/{requestId:guid}")]
    public ActionResult<RequestSummary> Get(Guid requestId)
    { var request = _store.Get(requestId); return request is null ? NotFound() : Ok(ToSummary(request)); }

    private static RequestSummary ToSummary(DownloadRequest request) => new(request.Id, request.Url, request.Category, request.Items, request.IsFinished);
}
