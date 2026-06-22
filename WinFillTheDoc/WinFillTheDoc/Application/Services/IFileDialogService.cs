namespace WinFillTheDoc.Application.Services;

public interface IFileDialogService
{
    string? SelectTemplateFile();
    string? SelectSourceFile();
}
