using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Keirohi_App.Updates;

internal sealed class GitHubReleaseClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(10) };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<IReadOnlyList<GitHubReleaseDto>> ListReleasesAsync(
        string repo,
        CancellationToken cancellationToken)
    {
        var url = $"https://api.github.com/repos/{repo}/releases?per_page=30";
        var list = await GetJsonAsync<List<GitHubReleaseDto>>(url, cancellationToken);
        return list ?? [];
    }

    public async Task DownloadAsync(
        string downloadUrl,
        string destinationPath,
        IProgress<OnlineUpdateProgress>? progress,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
        ApplyHeaders(request);
        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = File.Create(destinationPath);

        var buffer = new byte[81920];
        long received = 0;
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            received += read;
            if (total is > 0)
            {
                var percent = received * 100.0 / total.Value;
                progress?.Report(new OnlineUpdateProgress($"ダウンロード中... {percent:0}%", percent));
            }
        }

        progress?.Report(new OnlineUpdateProgress("ダウンロード完了", 100));
    }

    private static async Task<T?> GetJsonAsync<T>(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyHeaders(request);
        using var response = await Http.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }

    private static void ApplyHeaders(HttpRequestMessage request)
    {
        request.Headers.UserAgent.ParseAdd("keirohi-updater");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }
}

internal sealed class GitHubReleaseDto
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    [JsonPropertyName("published_at")]
    public DateTimeOffset? PublishedAt { get; set; }

    [JsonPropertyName("assets")]
    public List<GitHubReleaseAssetDto>? Assets { get; set; }
}

internal sealed class GitHubReleaseAssetDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; set; }
}
