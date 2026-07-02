namespace WinFillTheDoc.Application.Services;

public interface IExternalLinkService
{
    void Open(string url);
    void OpenFile(string filePath);
    void OpenFolder(string folderPath);
}
