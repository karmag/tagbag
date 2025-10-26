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

        Text = "Tagbag";
    }

    private Control MakeGridView(Data data)
    {
        var pad = 5;

        var panel = new Control();

        data.ImageGrid.Dock = DockStyle.Fill;
        data.ImageGrid.Padding = new Padding(pad);
        panel.Controls.Add(data.ImageGrid);

        var split = new Splitter();
        split.Width = 5;
        panel.Controls.Add(split);

        var ttPanel = new Panel();
        ttPanel.Padding = new Padding(pad);
        ttPanel.Dock = DockStyle.Left;
        data.TagTable.Dock = DockStyle.Fill;
        ttPanel.Controls.Add(data.TagTable);
        panel.Controls.Add(ttPanel);

        data.TagTable.SetEntry(data.EntryCollection.Get(0));

        var commandLine = new Components.CommandLine(pad);
        commandLine.Dock = DockStyle.Bottom;
        commandLine.Padding = new Padding(pad);
        panel.Controls.Add(commandLine);

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
