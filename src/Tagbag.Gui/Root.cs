using System;
using System.Drawing;
using System.Windows.Forms;

namespace Tagbag.Gui;

public class Root : Form
{
    private Data _Data;

    public Root(Tagbag.Core.Tagbag tb)
    {
        _Data = new Data(tb);
        _Data.EventDispatcher = EventHandler.GridViewEventDispatch;

        Width = 800;
        Height = 600;
        MinimumSize = new Size(500, 500);

        var grid = MakeGridView(_Data);
        grid.Dock = DockStyle.Fill;
        Controls.Add(grid);

        Text = "Tagbag";

        Shown += (_, _) => { Command.SwapMode(_Data, Mode.CommandMode); };

        KeyPreview = true;
        KeyDown += KeyHandler;
        KeyUp += (_, _) => { };
        KeyPress += KeyPressHandler;

        foreach (var mode in new Mode[]{Mode.GridMode, Mode.CommandMode})
        {
            _Data.KeyMap.SwapMode(mode);

            _Data.KeyMap.Register(Keys.Alt | Keys.Q, (data) => { Command.ToggleSelection(data, 0, 0); });
            _Data.KeyMap.Register(Keys.Alt | Keys.W, (data) => { Command.ToggleSelection(data, 1, 0); });
            _Data.KeyMap.Register(Keys.Alt | Keys.E, (data) => { Command.ToggleSelection(data, 2, 0); });
            _Data.KeyMap.Register(Keys.Alt | Keys.R, (data) => { Command.ToggleSelection(data, 3, 0); });
            _Data.KeyMap.Register(Keys.Alt | Keys.T, (data) => { Command.ToggleSelection(data, 4, 0); });

            _Data.KeyMap.Register(Keys.Alt | Keys.A, (data) => { Command.ToggleSelection(data, 0, 1); });
            _Data.KeyMap.Register(Keys.Alt | Keys.S, (data) => { Command.ToggleSelection(data, 1, 1); });
            _Data.KeyMap.Register(Keys.Alt | Keys.D, (data) => { Command.ToggleSelection(data, 2, 1); });
            _Data.KeyMap.Register(Keys.Alt | Keys.F, (data) => { Command.ToggleSelection(data, 3, 1); });
            _Data.KeyMap.Register(Keys.Alt | Keys.G, (data) => { Command.ToggleSelection(data, 4, 1); });

            _Data.KeyMap.Register(Keys.Alt | Keys.Z, (data) => { Command.ToggleSelection(data, 0, 2); });
            _Data.KeyMap.Register(Keys.Alt | Keys.X, (data) => { Command.ToggleSelection(data, 1, 2); });
            _Data.KeyMap.Register(Keys.Alt | Keys.C, (data) => { Command.ToggleSelection(data, 2, 2); });
            _Data.KeyMap.Register(Keys.Alt | Keys.V, (data) => { Command.ToggleSelection(data, 3, 2); });
            _Data.KeyMap.Register(Keys.Alt | Keys.B, (data) => { Command.ToggleSelection(data, 4, 2); });
        }

        _Data.KeyMap.SwapMode(Mode.GridMode);
        _Data.KeyMap.Register(Keys.Escape, (data) => { Command.SwapMode(data, Mode.CommandMode); });

        _Data.KeyMap.SwapMode(Mode.CommandMode);
        _Data.KeyMap.Register(Keys.Escape, (data) => { Command.SwapMode(data, Mode.GridMode); });

        _Data.KeyMap.SwapMode(null);
        _Data.KeyMap.Register(Keys.F9, (data) => { _Data.Report($"{this.ActiveControl}"); });
    }

    private void KeyHandler(Object? o, KeyEventArgs e)
    {
        if (_Data.KeyMap.Get(e.KeyData) is Action<Data> action)
        {
            e.SuppressKeyPress = true;
            action(_Data);
        }
        _Data.Report($"{e.KeyData}");
        // https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.keyeventargs?view=windowsdesktop-9.0
    }

    private void KeyPressHandler(Object? o, KeyPressEventArgs e)
    {
        //e.Handled = true;
        // e.KeyChar
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

        data.CommandLine.Dock = DockStyle.Top;
        data.CommandLine.Padding = new Padding(pad);
        panel.Controls.Add(data.CommandLine);

        data.StatusBar.Dock = DockStyle.Bottom;
        panel.Controls.Add(data.StatusBar);

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
