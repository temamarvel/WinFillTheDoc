using System.Collections.ObjectModel;
using WinFillTheDoc.Domain.Placeholders;

namespace WinFillTheDoc.Application.ViewModels;

public sealed class PlaceholderSectionGroup
{
    public PlaceholderSectionGroup(PlaceholderSection section, IEnumerable<DocumentFieldValue> fields)
    {
        Title = section.GetTitle();
        Fields = new ObservableCollection<DocumentFieldValue>(fields);
    }

    public string Title { get; }
    public ObservableCollection<DocumentFieldValue> Fields { get; }
}
