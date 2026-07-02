using NUnit.Framework;
using WinFillTheDoc.Application.ViewModels;

namespace WinFillTheDoc.Tests;

public sealed class DocumentSetupViewModelTests
{
    [TestCase(@"C:\docs\template.docx", true)]
    [TestCase(@"C:\docs\template.pdf", false)]
    [TestCase(@"C:\docs\template.txt", false)]
    public void CanDropTemplate_AcceptsOnlyDocx(string path, bool expected) =>
        Assert.That(DocumentSetupViewModel.CanDropTemplate(path), Is.EqualTo(expected));

    [TestCase(@"C:\docs\source.txt", true)]
    [TestCase(@"C:\docs\source.docx", true)]
    [TestCase(@"C:\docs\source.pdf", true)]
    [TestCase(@"C:\docs\source.xlsx", false)]
    [TestCase(@"C:\docs\source.rtf", false)]
    public void CanDropSource_AcceptsSupportedSourceFormats(string path, bool expected) =>
        Assert.That(DocumentSetupViewModel.CanDropSource(path), Is.EqualTo(expected));
}
