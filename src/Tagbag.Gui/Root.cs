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

        StartPosition = FormStartPosition.CenterScreen;

        LayoutControls(_Data);

        Text = "Tagbag";

        FormClosing += (_, _) => { _Data.EventHub.Send(new Shutdown()); };
        Shown += (_, _) => {
            UserCommand.SetMode(_Data, new Mode(Mode.ApplicationMode.Grid, Mode.InputMode.Browse));
            _Data.CommandLine.SetEnabled(false);
        };

        KeyPreview = true;
        KeyDown += KeyHandler;
        KeyUp += (_, _) => { };
        KeyPress += KeyPressHandler;
        PreviewKeyDown += (_, ev) => { ev.IsInputKey = true; };

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

    private void LayoutControls(Data data)
    {
        var pad = 5;

        data.ImagePanel.Name = "ImagePanel";
        data.ImagePanel.Dock = DockStyle.Fill;
        data.ImagePanel.Padding = new Padding(pad);
        Controls.Add(data.ImagePanel);

        var split = new Splitter();
        split.Name = "Splitter";
        split.Width = 5;
        Controls.Add(split);

        var ttPanel = new Panel();
        ttPanel.Name = "TagTablePanel";
        ttPanel.Padding = new Padding(pad);
        ttPanel.Dock = DockStyle.Left;
        data.TagTable.Name = "TagTable";
        data.TagTable.Dock = DockStyle.Fill;
        ttPanel.Controls.Add(data.TagTable);
        Controls.Add(ttPanel);

        data.StatusBar.Name = "StatusBar";
        data.StatusBar.Dock = DockStyle.Top;
        Controls.Add(data.StatusBar);

        data.CommandLine.Name = "CommandLine";
        data.CommandLine.Dock = DockStyle.Top;
        data.CommandLine.Padding = new Padding(pad);
        Controls.Add(data.CommandLine);

        var menu = MakeMenu(_Data);
        menu.Dock = DockStyle.Top;
        Controls.Add(menu);
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
        var add = _Data.KeyMap.Add;

        Mode GridCommand = new Mode(Mode.ApplicationMode.Grid, Mode.InputMode.Command);
        Mode GridBrowse = new Mode(Mode.ApplicationMode.Grid, Mode.InputMode.Browse);
        Mode SingleCommand = new Mode(Mode.ApplicationMode.Single, Mode.InputMode.Command);
        Mode SingleBrowse = new Mode(Mode.ApplicationMode.Single, Mode.InputMode.Browse);

        Func<Data, bool> isCmdClosed = (data) => { return !data.CommandLine.IsEnabled(); };
        Action<Data, int, int> toggleAndMove = (data, x, y) => {
            UserCommand.ToggleMarkCursor(data);
            UserCommand.MoveCursor(data, x, y);
        };

        // All modes

        add(new KeyData(null, Keys.F9, (data) => {
            System.Console.WriteLine($"Name: {ActiveControl?.Name}  Size: {ActiveControl?.Size}");
        }));

        add(new KeyData(null, Keys.Control | Keys.S, UserCommand.Save));
        add(new KeyData(null, Keys.Control | Keys.B, UserCommand.Backup));

        // // All image modes

        foreach (var mode in new Mode[] { GridCommand, GridBrowse, SingleCommand, SingleBrowse })
        {
            add(new KeyData(mode, Keys.F1, (data) => UserCommand.SetCommandMode(data, CommandLineMode.FilterMode)));
            add(new KeyData(mode, Keys.F2, (data) => UserCommand.SetCommandMode(data, CommandLineMode.TagMode)));
            add(new KeyData(mode, Keys.Control | Keys.Tab, UserCommand.ToggleCommandMode));

            add(new KeyData(mode, Keys.Enter, UserCommand.PressEnter));

            add(new KeyData(mode, Keys.Control | Keys.C, UserCommand.CursorImageToClipboard));
            add(new KeyData(mode, Keys.Control | Keys.Shift | Keys.C, UserCommand.CursorPathToClipboard));

            add(new KeyData(mode, Keys.Tab, (data) =>
                               UserCommand.SetMode(
                                   data,
                                   data.Mode.Switch(data.Mode.Application == Mode.ApplicationMode.Grid ?
                                                    Mode.ApplicationMode.Single :
                                                    Mode.ApplicationMode.Grid))));

            add(new KeyData(mode, Keys.Alt | Keys.Left, (data) => UserCommand.MoveCursor(data, -1)));
            add(new KeyData(mode, Keys.Alt | Keys.Right, (data) => UserCommand.MoveCursor(data, 1)));
            add(new KeyData(mode, Keys.Alt | Keys.Up, (data) => UserCommand.MoveCursor(data, 0, -1)));
            add(new KeyData(mode, Keys.Alt | Keys.Down, (data) => UserCommand.MoveCursor(data, 0, 1)));

            add(new KeyData(mode, Keys.Control | Keys.Q, UserCommand.ClearMarked));
            add(new KeyData(mode, Keys.Control | Keys.A, UserCommand.MarkVisible));

            add(new KeyData(mode, Keys.Alt | Keys.Shift | Keys.Left, (data) => toggleAndMove(data, -1, 0)));
            add(new KeyData(mode, Keys.Alt | Keys.Shift | Keys.Right, (data) => toggleAndMove(data, 1, 0)));
            add(new KeyData(mode, Keys.Alt | Keys.Shift | Keys.Up, (data) => toggleAndMove(data, 0, -1)));
            add(new KeyData(mode, Keys.Alt | Keys.Shift | Keys.Down, (data) => toggleAndMove(data, 0, 1)));

            add(new KeyData(mode, Keys.PageUp, (data) => data.ImagePanel.MoveCursorPage(-1)));
            add(new KeyData(mode, Keys.PageDown, (data) => data.ImagePanel.MoveCursorPage(1)));

            add(new KeyData(mode, Keys.Escape, (data) => UserCommand.PopFilter(data)));
        }

        // Grid mode

        foreach (var mode in new Mode[] { GridCommand, GridBrowse })
        {
            add(new KeyData(mode, Keys.Tab, (data) => UserCommand.SetMode(data, data.Mode.Switch(Mode.ApplicationMode.Single))));

            List<List<Keys>> gridKeys = [[Keys.Q, Keys.W, Keys.E, Keys.R, Keys.T],
                                         [Keys.A, Keys.S, Keys.D, Keys.F, Keys.G],
                                         [Keys.Z, Keys.X, Keys.C, Keys.V, Keys.B]];

            for (int y = 0; y < gridKeys.Count; y++)
            {
                for (int x = 0; x < gridKeys[y].Count; x++)
                {
                    int staticX = x;
                    int staticY = y;
                    add(new KeyData(mode, Keys.Alt | gridKeys[y][x], (data) => data.ImagePanel.ToggleMarked(staticX, staticY)));
                }
            }
        }

        // Single mode

        foreach (var mode in new Mode[] { SingleCommand, SingleBrowse })
        {
            add(new KeyData(mode, Keys.Tab, (data) => UserCommand.SetMode(data, data.Mode.Switch(Mode.ApplicationMode.Grid))));
        }

        // Command mode

        foreach (var mode in new Mode[] { GridCommand, SingleCommand })
        {
            add(new KeyData(mode, Keys.Control | Keys.Enter, (data) => UserCommand.SetMode(data, data.Mode.Switch(Mode.InputMode.Browse))));
        }

        // Browse mode

        foreach (var mode in new Mode[] { GridBrowse, SingleBrowse })
        {
            add(new KeyData(mode, Keys.Control | Keys.Enter, (data) => UserCommand.SetMode(data, data.Mode.Switch(Mode.InputMode.Command))));

            add(new KeyData(mode, Keys.Home, (data) => data.EntryCollection.MoveCursor(-1000000)));
            add(new KeyData(mode, Keys.End, (data) => data.EntryCollection.MoveCursor(1000000)));

            add(new KeyData(mode, Keys.Left, (data) => UserCommand.MoveCursor(data, -1), isCmdClosed));
            add(new KeyData(mode, Keys.Right, (data) => UserCommand.MoveCursor(data, 1), isCmdClosed));
            add(new KeyData(mode, Keys.Up, (data) => UserCommand.MoveCursor(data, 0, -1), isCmdClosed));
            add(new KeyData(mode, Keys.Down, (data) => UserCommand.MoveCursor(data, 0, 1), isCmdClosed));

            add(new KeyData(mode, Keys.Space, (data) => toggleAndMove(data, 1, 0), isCmdClosed));

            add(new KeyData(mode, Keys.Shift | Keys.Left, (data) => toggleAndMove(data, -1, 0), isCmdClosed));
            add(new KeyData(mode, Keys.Shift | Keys.Right, (data) => toggleAndMove(data, 1, 0), isCmdClosed));
            add(new KeyData(mode, Keys.Shift | Keys.Up, (data) => toggleAndMove(data, 0, -1), isCmdClosed));
            add(new KeyData(mode, Keys.Shift | Keys.Down, (data) => toggleAndMove(data, 0, 1), isCmdClosed));
        }
    }

    private void KeyHandler(Object? o, KeyEventArgs e)
    {
        System.Console.WriteLine($"{ActiveControl?.Name} --- {e.KeyData}");
        if (_Data.KeyMap.Get(e.KeyData) is KeyData keyData)
        {
            if (keyData.IsValid?.Invoke(_Data) ?? true)
            {
                e.SuppressKeyPress = true;
                keyData.Action(_Data);
            }
        }
        // https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.keyeventargs?view=windowsdesktop-9.0
    }

    private void KeyPressHandler(Object? o, KeyPressEventArgs e)
    {
        //e.Handled = true;
        // System.Console.WriteLine(e.KeyChar);
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

            var imgs = "image";
            if (entries.Count > 1)
                imgs = $"{entries.Count} images";
            _Data.EventHub.Send(new Log(LogType.Info, $"Tagged {imgs} with {ev.Operation.ToString()}"));
        }

        _Data.EventHub.Send(new ShowEntry(atCursor));
    }
}
