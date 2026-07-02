using System.Net;
using NUnit.Framework;
using WinFillTheDoc.Application.Services;
using WinFillTheDoc.Infrastructure.Services;

namespace WinFillTheDoc.Tests;

public sealed class AppUpdateTests
{
    [TestCase("v1.2.3", "1.2.3")]
    [TestCase(" V2.0.0 ", "2.0.0")]
    [TestCase("1.0.0", "1.0.0")]
    public void NormalizeVersion_RemovesLeadingV(string raw, string expected) =>
        Assert.That(GitHubReleaseVersionParser.NormalizeVersion(raw), Is.EqualTo(expected));

    [Test]
    public void CompareVersions_HandlesSemverLikeVersions()
    {
        Assert.That(GitHubReleaseVersionParser.IsVersionGreaterThan("1.10.0", "1.2.0"), Is.True);
        Assert.That(GitHubReleaseVersionParser.CompareVersions("1.0.0", "v1.0.0"), Is.EqualTo(0));
        Assert.That(GitHubReleaseVersionParser.CompareVersions("bad", "1.0.0"), Is.EqualTo(0));
    }

    [Test]
    public async Task CheckForUpdateAsync_ReturnsNull_WhenLatestIsNotNewer()
    {
        var checker = new AppUpdateChecker(
            new FakeReleaseClient(new GitHubRelease("v1.0.0", "Release", "", "https://example.test/release", [])),
            new FakeVersionProvider("1.0.0"));

        var update = await checker.CheckForUpdateAsync();

        Assert.That(update, Is.Null);
    }

    [Test]
    public async Task CheckForUpdateAsync_ReturnsUpdate_WhenLatestIsNewer()
    {
        var checker = new AppUpdateChecker(
            new FakeReleaseClient(new GitHubRelease("v1.2.0", "Release", "Notes", "https://example.test/release",
            [
                new GitHubReleaseAsset("app.zip", "https://example.test/app.zip"),
                new GitHubReleaseAsset("app.msi", "https://example.test/app.msi"),
            ])),
            new FakeVersionProvider("1.0.0"));

        var update = await checker.CheckForUpdateAsync();

        Assert.That(update?.LatestVersion, Is.EqualTo("1.2.0"));
        Assert.That(update?.DownloadUrl, Is.EqualTo("https://example.test/app.msi"));
    }

    [Test]
    public void PreferredDownloadAsset_PrefersMsiThenExeThenZip()
    {
        var asset = AppUpdateChecker.PreferredDownloadAsset(
        [
            new GitHubReleaseAsset("app.zip", "zip"),
            new GitHubReleaseAsset("app.exe", "exe"),
            new GitHubReleaseAsset("app.msi", "msi"),
        ]);

        Assert.That(asset?.BrowserDownloadUrl, Is.EqualTo("msi"));
    }

    [Test]
    public async Task GitHubReleaseClient_ParsesReleaseJson()
    {
        var client = new GitHubReleaseClient(new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, """
        {
          "tag_name": "v1.2.0",
          "name": "Release 1.2",
          "body": "Notes",
          "html_url": "https://example.test/release",
          "assets": [
            { "name": "app.msi", "browser_download_url": "https://example.test/app.msi" }
          ]
        }
        """)));

        var release = await client.FetchLatestReleaseAsync();

        Assert.That(release.TagName, Is.EqualTo("v1.2.0"));
        Assert.That(release.Assets.Single().Name, Is.EqualTo("app.msi"));
    }

    [Test]
    public void GitHubReleaseClient_ThrowsOnNonSuccess()
    {
        var client = new GitHubReleaseClient(new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.NotFound, "{}")));

        Assert.ThrowsAsync<InvalidOperationException>(() => client.FetchLatestReleaseAsync());
    }

    private sealed class FakeReleaseClient : IGitHubReleaseClient
    {
        private readonly GitHubRelease _release;

        public FakeReleaseClient(GitHubRelease release)
        {
            _release = release;
        }

        public Task<GitHubRelease> FetchLatestReleaseAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_release);
    }

    private sealed class FakeVersionProvider : IAppVersionProvider
    {
        public FakeVersionProvider(string currentVersion)
        {
            CurrentVersion = currentVersion;
        }

        public string CurrentVersion { get; }
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public FakeHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(_statusCode) { Content = new StringContent(_content) });
    }
}
