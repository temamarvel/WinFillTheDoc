namespace WinFillTheDoc.Application.Services;

public sealed class AppUpdateChecker : IAppUpdateChecker
{
    private readonly IGitHubReleaseClient _releaseClient;
    private readonly IAppVersionProvider _versionProvider;

    public AppUpdateChecker(IGitHubReleaseClient releaseClient, IAppVersionProvider versionProvider)
    {
        _releaseClient = releaseClient;
        _versionProvider = versionProvider;
    }

    public async Task<UpdateAvailability?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        var currentVersion = GitHubReleaseVersionParser.NormalizeVersion(_versionProvider.CurrentVersion);
        var release = await _releaseClient.FetchLatestReleaseAsync(cancellationToken).ConfigureAwait(false);
        var latestVersion = GitHubReleaseVersionParser.NormalizeVersion(release.TagName);

        if (!GitHubReleaseVersionParser.IsVersionGreaterThan(latestVersion, currentVersion))
            return null;

        var preferredAsset = PreferredDownloadAsset(release.Assets);
        return new UpdateAvailability(
            currentVersion,
            latestVersion,
            release.HtmlUrl,
            preferredAsset?.BrowserDownloadUrl,
            release.Name,
            release.Body);
    }

    public static GitHubReleaseAsset? PreferredDownloadAsset(IReadOnlyList<GitHubReleaseAsset> assets) =>
        assets.FirstOrDefault(x => x.Name.Contains(".msi", StringComparison.OrdinalIgnoreCase))
        ?? assets.FirstOrDefault(x => x.Name.Contains(".exe", StringComparison.OrdinalIgnoreCase))
        ?? assets.FirstOrDefault(x => x.Name.Contains(".zip", StringComparison.OrdinalIgnoreCase))
        ?? assets.FirstOrDefault();
}
