using System.Linq;
using System.Drawing;

namespace Tagbag.Gui.Components;

public class StatusBar : RichLabel
{
    private EntryCollection _EntryCollection;
    private ImagePanel _ImagePanel;

    public StatusBar(EventHub eventHub,
                     EntryCollection entryCollection,
                     ImagePanel imagePanel)
    {
        _EntryCollection = entryCollection;
        _ImagePanel = imagePanel;
        eventHub.EntriesUpdated += (_) => RefreshText();
        eventHub.MarkedChanged += (_) => RefreshText();
        eventHub.ViewChanged += (_) => RefreshText();

        GuiTool.Setup(this);
    }

    private void RefreshText()
    {
        Clear();

        // position

        var visibleStart = _ImagePanel.GetVisibleStartIndex();
        if (visibleStart == 0)
        {
            AddText("Top", Color.Black);
        }
        else if (visibleStart + _ImagePanel.GetVisibleAmount() >= _EntryCollection.Size())
        {
            AddText("Bot", Color.Black);
        }
        else
        {
            var startPercent = (int)((decimal)visibleStart /
                                     (decimal)_EntryCollection.Size() *
                                     100);
            AddText($"{startPercent,2}%", Color.Black);
        }

        AddText(" --- ", Color.DarkGray);

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
