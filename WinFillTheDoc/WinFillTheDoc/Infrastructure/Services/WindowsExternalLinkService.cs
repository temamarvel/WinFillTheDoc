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
}
