namespace WinFillTheDoc.Application.Services;

public sealed record UpdateAvailability(
    string CurrentVersion,
    string LatestVersion,
    string ReleasePageUrl,
    string? DownloadUrl,
    string? ReleaseTitle,
    string? ReleaseNotes);
