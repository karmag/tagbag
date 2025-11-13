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
        eventHub.EntriesUpdated += (_) => RefreshText();
        eventHub.MarkedChanged += (_) => RefreshText();
    }

    private void RefreshText()
    {
        Clear();

        // entry count
        
        AddText($"{_EntryCollection.Size(),3}", Color.Black);
        AddText(" / ", Color.Gray);
        AddText(_EntryCollection.MaxSize().ToString(), Color.Gray);

        AddText(" --- ", Color.DarkGray);

        // marked

        var marked = _EntryCollection.GetMarked();
        if (marked.Count == 0)
        {
            AddText("  0 marked", Color.Gray);
        }
        else
        {
            AddText($"{marked.Count,3}", Color.Red);
            AddText(" marked", Color.Gray);
        }

        AddText(" --- ", Color.DarkGray);

        // filter

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
