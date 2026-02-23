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
            AddText("Top", GuiTool.ForeColor);
        }
        else if (visibleStart + _ImagePanel.GetVisibleAmount() >= _EntryCollection.Size())
        {
            AddText("Bot", GuiTool.ForeColor);
        }
        else
        {
            var startPercent = (int)((decimal)visibleStart /
                                     (decimal)_EntryCollection.Size() *
                                     100);
            AddText($"{startPercent,2}%", GuiTool.ForeColor);
        }

        AddText(" --- ", GuiTool.ForeColorAlt);

        // entry count

        AddText($"{_EntryCollection.Size(),3}", GuiTool.ForeColor);
        AddText(" / ", GuiTool.ForeColorDisabled);
        AddText(_EntryCollection.MaxSize().ToString(), GuiTool.ForeColorDisabled);

        AddText(" --- ", GuiTool.ForeColorAlt);

        // marked

        var marked = _EntryCollection.GetMarked();
        if (marked.Count == 0)
        {
            AddText("  0 marked", GuiTool.ForeColorDisabled);
        }
        else
        {
            AddText($"{marked.Count,3}", Color.Red);
            AddText(" marked", GuiTool.ForeColorDisabled);
        }

        AddText(" --- ", GuiTool.ForeColorAlt);

        // filter

        AddText("[", GuiTool.ForeColorDisabled);
        var first = true;
        foreach (var fltr in _EntryCollection.GetFilters().Reverse())
        {
            if (first)
                first = false;
            else
                AddText(" / ", GuiTool.ForeColorDisabled);
            AddText(fltr?.ToString() ?? "", GuiTool.ForeColor);
        }
        AddText("]", GuiTool.ForeColorDisabled);
    }
}
