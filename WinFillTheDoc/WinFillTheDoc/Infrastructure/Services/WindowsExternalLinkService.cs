using System.Diagnostics;
using WinFillTheDoc.Application.Services;

namespace WinFillTheDoc.Infrastructure.Services;

public sealed class WindowsExternalLinkService : IExternalLinkService
{
    public void Open(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    public void OpenFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
    }

    public void OpenFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath)) return;

        Process.Start(new ProcessStartInfo("explorer.exe", folderPath) { UseShellExecute = true });
    }
}
