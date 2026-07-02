using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using WinFillTheDoc.Application.Services;

namespace WinFillTheDoc.Infrastructure.Services;

public sealed class GitHubReleaseClient : IGitHubReleaseClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _httpClient;
    private readonly string _owner;
    private readonly string _repo;

    public GitHubReleaseClient(HttpClient httpClient) : this(httpClient, "temamarvel", "FillTheDoc")
    {
    }

    public GitHubReleaseClient(HttpClient httpClient, string owner, string repo)
    {
        _httpClient = httpClient;
        _owner = owner;
        _repo = repo;
    }

    public async Task<GitHubRelease> FetchLatestReleaseAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.UserAgent.ParseAdd("WinFillTheDoc");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GitHub вернул ошибку со статусом {(int)response.StatusCode}.");

        return JsonSerializer.Deserialize<GitHubRelease>(body, JsonOptions)
            ?? throw new InvalidOperationException("Некорректный ответ GitHub.");
    }
}
