using System.Windows.Forms;

namespace Tagbag.Gui;

public class Root : Form
{
    public Root(Tagbag.Core.Tagbag tb)
    {
        var data = new Data(tb);
        data.EventDispatcher = EventHandler.GridViewEventDispatch;

        Width = 800;
        Height = 600;

        var grid = MakeGridView(data);
        grid.Dock = DockStyle.Fill;
        Controls.Add(grid);
    }

    private Control MakeGridView(Data data)
    {
        var panel = new Control();

        data.ImageGrid.Dock = DockStyle.Fill;
        panel.Controls.Add(data.ImageGrid);

        var split = new Splitter();
        panel.Controls.Add(split);

        data.TagTable.Dock = DockStyle.Left;
        panel.Controls.Add(data.TagTable);

        data.TagTable.SetEntry(data.EntryCollection.Get(0));

        var input = new TextBox();
        input.Dock = DockStyle.Bottom;
        panel.Controls.Add(input);
        
        return panel;
    }
}

public static class EventHandler
{
    public static void GridViewEventDispatch(Data data, Event ev)
    {
        switch (ev)
        {
            case CellClicked e:
                data.ImageGrid.SetCursor(e.Id);
                data.TagTable.SetEntry(data.Tagbag.Get(e.Id));
                break;

            default:
                MessageBox.Show($"Unknown event {ev}");
                break;
        }
    }
}
