using Microsoft.Win32;
using WinFillTheDoc.Application.Services;

namespace WinFillTheDoc.Infrastructure.Services;

public sealed class FileDialogService : IFileDialogService
{
    public string? SelectTemplateFile() => SelectFile(
        "Шаблоны Word (*.docx)|*.docx",
        "Выберите DOCX-шаблон");

    public string? SelectSourceFile() => SelectFile(
        "Поддерживаемые файлы|*.doc;*.docx;*.pdf;*.xls;*.xlsx;*.txt|Все файлы|*.*",
        "Выберите файл с данными");

    public string? SelectOutputFile(string suggestedFileName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Документ Word (*.docx)|*.docx",
            Title = "Сохранить заполненный документ",
            FileName = suggestedFileName,
            DefaultExt = ".docx",
            AddExtension = true,
            OverwritePrompt = true,
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static string? SelectFile(string filter, string title)
    {
        var dialog = new OpenFileDialog { Filter = filter, Title = title, CheckFileExists = true };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
