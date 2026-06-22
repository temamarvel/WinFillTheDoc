using System.IO;

namespace WinFillTheDoc.Domain.Documents;

public sealed record DocumentFile(string FullPath)
{
    public string FileName => Path.GetFileName(FullPath);
}
