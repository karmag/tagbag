using System.Linq;
using System.Drawing;

namespace Tagbag.Gui.Components;

public class StatusBar : RichLabel
{
    private EntryCollection _EntryCollection;

    public StatusBar(EventHub eventHub,
                     EntryCollection entryCollection)
    {
        _EntryCollection = entryCollection;
        eventHub.EntriesUpdated += ListenEntriesUpdated;
    }

    private void ListenEntriesUpdated(EntriesUpdated _)
    {
        RefreshText();
    }

    private void RefreshText()
    {
        Clear();

        AddText(_EntryCollection.Size().ToString(), Color.Black);
        AddText(" / ", Color.Gray);
        AddText(_EntryCollection.MaxSize().ToString(), Color.Gray);

        AddText(" --- ", Color.White);

        AddText("[", Color.Gray);
        var first = true;
        foreach (var fltr in _EntryCollection.GetFilters().Reverse())
        {
            if (first)
                first = false;
            else
                AddText(" / ", Color.Gray);
            AddText(fltr?.ToString() ?? "", Color.Black);
        }
        AddText("]", Color.Gray);
    }
}
