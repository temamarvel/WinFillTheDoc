using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WinFillTheDoc.Application.ViewModels;

namespace WinFillTheDoc.Views;

public partial class DocumentSetupView : UserControl
{
    public DocumentSetupView()
    {
        InitializeComponent();
    }

    private void OnTemplateDragOver(object sender, DragEventArgs e) =>
        SetDropEffect(e, DocumentSetupViewModel.CanDropTemplate);

    private void OnSourceDragOver(object sender, DragEventArgs e) =>
        SetDropEffect(e, DocumentSetupViewModel.CanDropSource);

    private void OnTemplateDrop(object sender, DragEventArgs e)
    {
        if (DataContext is DocumentSetupViewModel viewModel && FirstDroppedFile(e) is { } path)
            viewModel.UseDroppedTemplate(path);
        e.Handled = true;
    }

    private void OnSourceDrop(object sender, DragEventArgs e)
    {
        if (DataContext is DocumentSetupViewModel viewModel && FirstDroppedFile(e) is { } path)
            viewModel.UseDroppedSource(path);
        e.Handled = true;
    }

    private static void SetDropEffect(DragEventArgs e, Func<string, bool> canDrop)
    {
        if (!ContainsFilePayload(e))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        // Some shell sources do not expose the final file name reliably during DragOver.
        // Keep the cursor permissive and validate the actual extension on Drop.
        AllowFileDrag(e);
    }

    private static void AllowFileDrag(DragEventArgs e)
    {
        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private static string? FirstDroppedFile(DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop, autoConvert: true) &&
            e.Data.GetData(DataFormats.FileDrop, autoConvert: true) is string[] files)
            return files.FirstOrDefault();

        if (e.Data.GetDataPresent("FileNameW", autoConvert: true) &&
            e.Data.GetData("FileNameW", autoConvert: true) is string[] wideFiles)
            return wideFiles.FirstOrDefault();

        if (e.Data.GetDataPresent("FileName", autoConvert: true) &&
            e.Data.GetData("FileName", autoConvert: true) is string[] ansiFiles)
            return ansiFiles.FirstOrDefault();

        return null;
    }

    private static bool ContainsFilePayload(DragEventArgs e) =>
        e.Data.GetDataPresent(DataFormats.FileDrop, autoConvert: true)
        || e.Data.GetDataPresent("FileNameW", autoConvert: true)
        || e.Data.GetDataPresent("FileName", autoConvert: true);
}
