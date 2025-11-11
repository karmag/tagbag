using System;
using System.Drawing;
using System.Windows.Forms;
using Tagbag.Gui.Components;
using Tagbag.Core;

namespace Tagbag.Gui;

public class Root : Form
{
    private Data _Data;

    public Root()
    {
        Tagbag.Core.Tagbag tb = Tagbag.Core.Tagbag.Open("test-data");
        _Data = new Data(tb);

        Width = 900;
        Height = 600;
        MinimumSize = new Size(500, 500);

        var grid = MakeGridView(_Data);
        grid.Dock = DockStyle.Fill;
        Controls.Add(grid);

        Text = "Tagbag";

        Shown += (_, _) => { UserCommand.SwapMode(_Data, Mode.CommandMode); };
        FormClosing += (_, _) => { _Data.EventHub.Send(new Shutdown()); };

        KeyPreview = true;
        KeyDown += KeyHandler;
        KeyUp += (_, _) => { };
        KeyPress += KeyPressHandler;

        _Data.KeyMap.SwapMode(null);

        _Data.KeyMap.Register(Keys.Control | Keys.S, (data) => { UserCommand.Save(data); });
        _Data.KeyMap.Register(Keys.Control | Keys.B, (data) => { UserCommand.Backup(data); });

        _Data.KeyMap.Register(Keys.F9, (data) => { _Data.Report($"{this.ActiveControl}"); });

        _Data.KeyMap.Register(Keys.F1, (data) => { UserCommand.SetCommandMode(data, CommandLineMode.FilterMode); });
        _Data.KeyMap.Register(Keys.F2, (data) => { UserCommand.SetCommandMode(data, CommandLineMode.TagMode); });

        _Data.KeyMap.Register(Keys.Alt | Keys.Q, (data) => { UserCommand.ToggleMarked(data, 0, 0); });
        _Data.KeyMap.Register(Keys.Alt | Keys.W, (data) => { UserCommand.ToggleMarked(data, 1, 0); });
        _Data.KeyMap.Register(Keys.Alt | Keys.E, (data) => { UserCommand.ToggleMarked(data, 2, 0); });
        _Data.KeyMap.Register(Keys.Alt | Keys.R, (data) => { UserCommand.ToggleMarked(data, 3, 0); });
        _Data.KeyMap.Register(Keys.Alt | Keys.T, (data) => { UserCommand.ToggleMarked(data, 4, 0); });

        _Data.KeyMap.Register(Keys.Alt | Keys.A, (data) => { UserCommand.ToggleMarked(data, 0, 1); });
        _Data.KeyMap.Register(Keys.Alt | Keys.S, (data) => { UserCommand.ToggleMarked(data, 1, 1); });
        _Data.KeyMap.Register(Keys.Alt | Keys.D, (data) => { UserCommand.ToggleMarked(data, 2, 1); });
        _Data.KeyMap.Register(Keys.Alt | Keys.F, (data) => { UserCommand.ToggleMarked(data, 3, 1); });
        _Data.KeyMap.Register(Keys.Alt | Keys.G, (data) => { UserCommand.ToggleMarked(data, 4, 1); });

        _Data.KeyMap.Register(Keys.Alt | Keys.Z, (data) => { UserCommand.ToggleMarked(data, 0, 2); });
        _Data.KeyMap.Register(Keys.Alt | Keys.X, (data) => { UserCommand.ToggleMarked(data, 1, 2); });
        _Data.KeyMap.Register(Keys.Alt | Keys.C, (data) => { UserCommand.ToggleMarked(data, 2, 2); });
        _Data.KeyMap.Register(Keys.Alt | Keys.V, (data) => { UserCommand.ToggleMarked(data, 3, 2); });
        _Data.KeyMap.Register(Keys.Alt | Keys.B, (data) => { UserCommand.ToggleMarked(data, 4, 2); });

        _Data.KeyMap.Register(Keys.Alt | Keys.Up, (data) => { UserCommand.MoveCursor(data, 0, -1); });
        _Data.KeyMap.Register(Keys.Alt | Keys.Down, (data) => { UserCommand.MoveCursor(data, 0, 1); });
        _Data.KeyMap.Register(Keys.Alt | Keys.Left, (data) => { UserCommand.MoveCursor(data, -1, 0); });
        _Data.KeyMap.Register(Keys.Alt | Keys.Right, (data) => { UserCommand.MoveCursor(data, 1, 0); });

        _Data.KeyMap.Register(Keys.PageUp, (data) => { UserCommand.MovePage(data, -1); });
        _Data.KeyMap.Register(Keys.PageDown, (data) => { UserCommand.MovePage(data, 1); });

        _Data.KeyMap.SwapMode(Mode.CommandMode);
        _Data.KeyMap.Register(Keys.Escape, (data) => { UserCommand.PopFilter(data); });

        _Data.EventHub.FilterCommand += ListenFilterCommand;
        _Data.EventHub.TagCommand += ListenTagCommand;

        _Data.EntryCollection.SetBaseEntries(tb.GetEntries());
    }

    private void KeyHandler(Object? o, KeyEventArgs e)
    {
        if (_Data.KeyMap.Get(e.KeyData) is Action<Data> action)
        {
            e.SuppressKeyPress = true;
            action(_Data);
        }
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

        data.CommandLine.Dock = DockStyle.Top;
        data.CommandLine.Padding = new Padding(pad);
        panel.Controls.Add(data.CommandLine);

        data.StatusBar.Dock = DockStyle.Bottom;
        panel.Controls.Add(data.StatusBar);

        return panel;
    }

    private void ListenFilterCommand(FilterCommand ev)
    {
        _Data.EntryCollection.PushFilter(ev.Filter);
    }

    private void ListenTagCommand(TagCommand ev)
    {
        if (_Data.EntryCollection.GetEntryAtCursor() is Entry entry)
        {
            ev.Operation.Apply(entry);
            _Data.EventHub.Send(new ShowEntry(entry));
        }
    }
}
