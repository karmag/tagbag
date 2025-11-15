using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using Tagbag.Core;
using Tagbag.Gui.Components;

namespace Tagbag.Gui;

public class Root : Form
{
    private Data _Data;

    public Root()
    {
        _Data = new Data();

        Width = 900;
        Height = 600;
        MinimumSize = new Size(800, 500);

        var grid = MakeGridView(_Data);
        grid.Dock = DockStyle.Fill;
        Controls.Add(grid);

        var menu = MakeMenu(_Data);
        menu.Dock = DockStyle.Top;
        this.Controls.Add(menu);

        Text = "Tagbag";

        Shown += (_, _) => { UserCommand.SwapMode(_Data, Mode.CommandMode); };
        FormClosing += (_, _) => { _Data.EventHub.Send(new Shutdown()); };

        KeyPreview = true;
        KeyDown += KeyHandler;
        KeyUp += (_, _) => { };
        KeyPress += KeyPressHandler;

        _Data.EventHub.FilterCommand += ListenFilterCommand;
        _Data.EventHub.TagCommand += ListenTagCommand;

        SetupKeyMap();
        _Data.SetTagbag(GetInitialTagbag());
    }

    private Tagbag.Core.Tagbag? GetInitialTagbag()
    {
        var args = Environment.GetCommandLineArgs();

        if (args.Length <= 1)
            return null;

        if (args.Length == 2)
        {
            var path = Tagbag.Core.Tagbag.Locate(args[1]);
            if (path == null)
                path = Tagbag.Core.Tagbag.Locate(null);
            if (path == null)
                return null;
            return Tagbag.Core.Tagbag.Open(path);
        }

        throw new ArgumentException($"Multiple command line arguments: [{String.Join(", ", args)}]");
    }

    private MenuStrip MakeMenu(Data data)
    {
        var menuStrip = new MenuStrip();

        var fileMenu = new ToolStripMenuItem("File");
        menuStrip.Items.Add(fileMenu);

        var fNew = new ToolStripMenuItem("New...");
        var fOpen = new ToolStripMenuItem("Open...");
        var fRecent = new ToolStripMenuItem("Recent");
        var fSave = new ToolStripMenuItem("Save");
        var fBackup = new ToolStripMenuItem("Backup");
        var fQuit = new ToolStripMenuItem("Quit");
        fileMenu.DropDownItems.Add(fNew);
        fileMenu.DropDownItems.Add(fOpen);
        fileMenu.DropDownItems.Add(fRecent);
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(fSave);
        fileMenu.DropDownItems.Add(fBackup);
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(fQuit);

        fNew.Command = new Button(() => { GuiCommand.NewTagbag(data); });
        fOpen.Command = new Button(() => { GuiCommand.OpenTagbag(data); });
        fSave.Command = new Button(() => { UserCommand.Save(data); });
        fBackup.Command = new Button(() => { UserCommand.Backup(data); });
        fQuit.Command = new Button(() => { UserCommand.Quit(data); });

        var viewMenu = new ToolStripMenuItem("View");
        menuStrip.Items.Add(viewMenu);

        var vGrid = new ToolStripMenuItem("Grid");
        var vSingle = new ToolStripMenuItem("Single");
        var vSummary = new ToolStripMenuItem("Tag summary");
        var vBulk = new ToolStripMenuItem("Bulk");
        viewMenu.DropDownItems.Add(vGrid);
        viewMenu.DropDownItems.Add(vSingle);
        viewMenu.DropDownItems.Add(vSummary);
        viewMenu.DropDownItems.Add(vBulk);

        var helpMenu = new ToolStripMenuItem("Help");
        menuStrip.Items.Add(helpMenu);
        
        return menuStrip;
    }

    private class Button : ICommand
    {
        private Action _Action;
        public event EventHandler? CanExecuteChanged;
        public Button(Action action) { _Action = action; }
        public bool CanExecute(object? _) { return true; }
        public void Execute(object? _) { _Action.Invoke(); }
    }

    private void SetupKeyMap()
    {
        _Data.KeyMap.SwapMode(null);

        _Data.KeyMap.Register(Keys.Control | Keys.S, UserCommand.Save);
        _Data.KeyMap.Register(Keys.Control | Keys.B, UserCommand.Backup);

        _Data.KeyMap.Register(Keys.F9, (data) => _Data.Report($"{this.ActiveControl}"));

        _Data.KeyMap.Register(Keys.F1, (data) => UserCommand.SetCommandMode(data, CommandLineMode.FilterMode));
        _Data.KeyMap.Register(Keys.F2, (data) => UserCommand.SetCommandMode(data, CommandLineMode.TagMode));

        _Data.KeyMap.Register(Keys.Alt | Keys.Q, (data) => UserCommand.ToggleMarked(data, 0, 0));
        _Data.KeyMap.Register(Keys.Alt | Keys.W, (data) => UserCommand.ToggleMarked(data, 1, 0));
        _Data.KeyMap.Register(Keys.Alt | Keys.E, (data) => UserCommand.ToggleMarked(data, 2, 0));
        _Data.KeyMap.Register(Keys.Alt | Keys.R, (data) => UserCommand.ToggleMarked(data, 3, 0));
        _Data.KeyMap.Register(Keys.Alt | Keys.T, (data) => UserCommand.ToggleMarked(data, 4, 0));

        _Data.KeyMap.Register(Keys.Alt | Keys.A, (data) => UserCommand.ToggleMarked(data, 0, 1));
        _Data.KeyMap.Register(Keys.Alt | Keys.S, (data) => UserCommand.ToggleMarked(data, 1, 1));
        _Data.KeyMap.Register(Keys.Alt | Keys.D, (data) => UserCommand.ToggleMarked(data, 2, 1));
        _Data.KeyMap.Register(Keys.Alt | Keys.F, (data) => UserCommand.ToggleMarked(data, 3, 1));
        _Data.KeyMap.Register(Keys.Alt | Keys.G, (data) => UserCommand.ToggleMarked(data, 4, 1));

        _Data.KeyMap.Register(Keys.Alt | Keys.Z, (data) => UserCommand.ToggleMarked(data, 0, 2));
        _Data.KeyMap.Register(Keys.Alt | Keys.X, (data) => UserCommand.ToggleMarked(data, 1, 2));
        _Data.KeyMap.Register(Keys.Alt | Keys.C, (data) => UserCommand.ToggleMarked(data, 2, 2));
        _Data.KeyMap.Register(Keys.Alt | Keys.V, (data) => UserCommand.ToggleMarked(data, 3, 2));
        _Data.KeyMap.Register(Keys.Alt | Keys.B, (data) => UserCommand.ToggleMarked(data, 4, 2));

        _Data.KeyMap.Register(Keys.Control | Keys.Q, (data) => UserCommand.ClearMarked(data));
        _Data.KeyMap.Register(Keys.Control | Keys.Space, (data) => UserCommand.ToggleMarkCursor(data));

        _Data.KeyMap.Register(Keys.Alt | Keys.Up, (data) => UserCommand.MoveCursor(data, 0, -1));
        _Data.KeyMap.Register(Keys.Alt | Keys.Down, (data) => UserCommand.MoveCursor(data, 0, 1));
        _Data.KeyMap.Register(Keys.Alt | Keys.Left, (data) => UserCommand.MoveCursor(data, -1, 0));
        _Data.KeyMap.Register(Keys.Alt | Keys.Right, (data) => UserCommand.MoveCursor(data, 1, 0));

        _Data.KeyMap.Register(Keys.PageUp, (data) => UserCommand.MovePage(data, -1));
        _Data.KeyMap.Register(Keys.PageDown, (data) => UserCommand.MovePage(data, 1));
        _Data.KeyMap.Register(Keys.Home, (data) => UserCommand.MovePage(data, -1000000));
        _Data.KeyMap.Register(Keys.End, (data) => UserCommand.MoveCursor(data, 1000, 1000000));

        _Data.KeyMap.Register(Keys.Control | Keys.C, UserCommand.CursorImageToClipboard);
        _Data.KeyMap.Register(Keys.Control | Keys.Shift | Keys.C, UserCommand.CursorPathToClipboard);

        _Data.KeyMap.SwapMode(Mode.CommandMode);
        _Data.KeyMap.Register(Keys.Escape, (data) => UserCommand.PopFilter(data));
    }

    private void KeyHandler(Object? o, KeyEventArgs e)
    {
        if (e.KeyData == Keys.Tab)
        {
            System.Console.WriteLine("TAB");
            e.SuppressKeyPress = true;
        }

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
        // System.Console.WriteLine(e.KeyChar);
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

        data.StatusBar.Dock = DockStyle.Top;
        panel.Controls.Add(data.StatusBar);

        data.CommandLine.Dock = DockStyle.Top;
        data.CommandLine.Padding = new Padding(pad);
        panel.Controls.Add(data.CommandLine);

        return panel;
    }

    private void ListenFilterCommand(FilterCommand ev)
    {
        _Data.EntryCollection.PushFilter(ev.Filter);
    }

    private void ListenTagCommand(TagCommand ev)
    {
        var atCursor = _Data.EntryCollection.GetEntryAtCursor();
        ICollection<Guid> entries = _Data.EntryCollection.GetMarked();
        if (entries.Count == 0 && atCursor != null)
            entries = [atCursor.Id];

        if (entries.Count > 0)
        {
            foreach (var id in entries)
                if (_Data.Tagbag?.Get(id) is Entry entry)
                    ev.Operation.Apply(entry);
        }

        _Data.EventHub.Send(new ShowEntry(atCursor));
    }
}
