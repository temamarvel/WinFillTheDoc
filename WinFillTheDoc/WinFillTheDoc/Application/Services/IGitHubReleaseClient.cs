namespace WinFillTheDoc.Application.Services;

public interface IGitHubReleaseClient
{
    Task<GitHubRelease> FetchLatestReleaseAsync(CancellationToken cancellationToken = default);
}
