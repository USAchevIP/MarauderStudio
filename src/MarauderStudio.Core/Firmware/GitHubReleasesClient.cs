using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MarauderStudio.Core.Firmware;

/// <summary>
/// Описание одного релиза Marauder с GitHub.
/// </summary>
public sealed record MarauderRelease(
    string TagName,
    string Name,
    DateTimeOffset PublishedAt,
    IReadOnlyList<MarauderAsset> Assets)
{
    public string DisplayVersion => TagName.StartsWith('v') ? TagName[1..] : TagName;
}

/// <summary>
/// Один ассет (бинарник) в релизе.
/// </summary>
public sealed record MarauderAsset(
    string Name,
    long Size,
    string DownloadUrl,
    DateTimeOffset CreatedAt);

/// <summary>
/// Клиент GitHub Releases для репозитория justcallmekoko/ESP32Marauder.
/// Использует прямой HTTP без Octokit (минимум зависимостей).
/// </summary>
public sealed class GitHubReleasesClient
{
    public const string RepoOwner = "justcallmekoko";
    public const string RepoName  = "ESP32Marauder";

    private static readonly Uri ReleasesUri =
        new($"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases");

    private readonly HttpClient _http;

    public GitHubReleasesClient(HttpClient? httpClient = null)
    {
        _http = httpClient ?? new HttpClient();
        if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
            _http.DefaultRequestHeaders.Add("User-Agent", "MarauderStudio/1.0");
        if (!_http.DefaultRequestHeaders.Contains("Accept"))
            _http.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
    }

    /// <summary>
    /// Список релизов (свежие первые). По умолчанию — 10 последних.
    /// </summary>
    public async Task<IReadOnlyList<MarauderRelease>> GetReleasesAsync(int count = 10, CancellationToken ct = default)
    {
        var url = new Uri($"{ReleasesUri}?per_page={Math.Clamp(count, 1, 50)}");
        await using var stream = await _http.GetStreamAsync(url, ct).ConfigureAwait(false);
        var dtos = await JsonSerializer.DeserializeAsync<List<ReleaseDto>>(stream, JsonOpts.Default, ct).ConfigureAwait(false);
        return (dtos ?? new()).Select(ToRelease).ToList();
    }

    /// <summary>
    /// Последний релиз.
    /// </summary>
    public async Task<MarauderRelease?> GetLatestAsync(CancellationToken ct = default)
    {
        var url = new Uri($"{ReleasesUri}/latest");
        await using var stream = await _http.GetStreamAsync(url, ct).ConfigureAwait(false);
        var dto = await JsonSerializer.DeserializeAsync<ReleaseDto>(stream, JsonOpts.Default, ct).ConfigureAwait(false);
        return dto is null ? null : ToRelease(dto);
    }

    /// <summary>
    /// Скачивает ассет в указанный файл, отдавая прогресс.
    /// </summary>
    public async Task DownloadAsync(MarauderAsset asset, string destinationPath,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength ?? asset.Size;

        await using var src = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var dst = File.Create(destinationPath);

        var buffer = new byte[64 * 1024];
        long readTotal = 0;
        int read;
        while ((read = await src.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            await dst.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
            readTotal += read;
            if (total > 0)
                progress?.Report((double)readTotal / total);
        }
        progress?.Report(1.0);
    }

    private static MarauderRelease ToRelease(ReleaseDto d) => new(
        TagName: d.TagName ?? string.Empty,
        Name: d.Name ?? d.TagName ?? string.Empty,
        PublishedAt: d.PublishedAt,
        Assets: (d.Assets ?? new()).Select(a => new MarauderAsset(
            Name: a.Name ?? string.Empty,
            Size: a.Size,
            DownloadUrl: a.BrowserDownloadUrl ?? string.Empty,
            CreatedAt: a.CreatedAt)).ToList());

    // ───── DTOs для System.Text.Json ─────

    private sealed class ReleaseDto
    {
        [JsonPropertyName("tag_name")]    public string? TagName { get; set; }
        [JsonPropertyName("name")]        public string? Name { get; set; }
        [JsonPropertyName("published_at")]public DateTimeOffset PublishedAt { get; set; }
        [JsonPropertyName("assets")]      public List<AssetDto>? Assets { get; set; }
    }

    private sealed class AssetDto
    {
        [JsonPropertyName("name")]                  public string? Name { get; set; }
        [JsonPropertyName("size")]                  public long Size { get; set; }
        [JsonPropertyName("browser_download_url")] public string? BrowserDownloadUrl { get; set; }
        [JsonPropertyName("created_at")]            public DateTimeOffset CreatedAt { get; set; }
    }

    private static class JsonOpts
    {
        public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web);
    }
}
