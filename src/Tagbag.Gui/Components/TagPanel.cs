using System.Windows.Forms;

namespace Tagbag.Gui.Components;

public class TagPanel : Panel
{
    private CardPanel _Cards;

    private TagTable _TagTable;
    private TagSummary _TagSummary;

    private const int EntryCard = 1;
    private const int SummaryCard = 2;

    public TagPanel(EventHub eventHub,
                    EntryCollection entryCollection,
                    ImageCache imageCache)
    {
        _Cards = new CardPanel();

        _TagTable = new Components.TagTable(eventHub, entryCollection, imageCache);
        _TagTable.Dock = DockStyle.Fill;

        _TagSummary = new Components.TagSummary(eventHub, entryCollection);
        _TagSummary.Dock = DockStyle.Fill;

        _TagSummary.SetActive(false);

        _Cards.Add(EntryCard, _TagTable);
        _Cards.Add(SummaryCard, _TagSummary);

        _Cards.Dock = DockStyle.Fill;
        Controls.Add(_Cards);

        GuiTool.Setup(this);
    }

    public void ShowEntry(bool showEntry)
    {
        var swap = (showEntry && _Cards.GetCurrent() == SummaryCard) ||
            (!showEntry && _Cards.GetCurrent() == EntryCard);

        if (swap)
        {
            _TagTable.SetActive(showEntry);
            _TagSummary.SetActive(!showEntry);

            if (_Cards.GetCurrent() == EntryCard)
                _Cards.Show(SummaryCard);
            else
                _Cards.Show(EntryCard);
        }
    }

    public void Swap()
    {
        ShowEntry(_Cards.GetCurrent() == SummaryCard);
    }
}
