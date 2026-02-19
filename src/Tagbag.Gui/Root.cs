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
        Name = "Root";
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
        };

        KeyPreview = true;
        KeyDown += KeyHandler;
        KeyUp += (_, _) => { };
        KeyPress += KeyPressHandler;
        PreviewKeyDown += (_, ev) => { ev.IsInputKey = true; };

        _Data.EventHub.FilterCommand += ListenFilterCommand;
        _Data.EventHub.TagCommand += ListenTagCommand;
        _Data.EventHub.TagbagFileSet += ListenTagbagFileSet;

        SetupActionDefinitions(_Data.KeyMap);
        SetupKeyMap(_Data.KeyMap);
        _Data.SetTagbag(GetInitialTagbag());
    }

    private Tagbag.Core.Tagbag? GetInitialTagbag()
    {
        var args = Environment.GetCommandLineArgs();

        if (args.Length <= 1)
        {
            try {
                return Tagbag.Core.Tagbag.Open(null);
            } catch (ArgumentException) {
                // noop
            }
            return null;
        }
        else if (args.Length == 2)
        {
            var path = Tagbag.Core.Tagbag.Locate(args[1]);
            if (path == null)
                path = Tagbag.Core.Tagbag.Locate(null);
            if (path == null)
                return null;
            return Tagbag.Core.Tagbag.Open(path);
        }
        else
        {
            throw new ArgumentException($"Multiple command line arguments: [{String.Join(", ", args)}]");
        }
    }

    private void LayoutControls(Data data)
    {
        var pad = 5;

        var TagView = new Control();
        GuiTool.Setup(TagView);
        TagView.Dock = DockStyle.Fill;

        data.ImagePanel.Name = "ImagePanel";
        data.ImagePanel.Dock = DockStyle.Fill;
        data.ImagePanel.Padding = new Padding(pad);
        TagView.Controls.Add(data.ImagePanel);

        var split = new Splitter();
        GuiTool.Setup(split);
        split.Name = "Splitter";
        split.Width = 5;
        TagView.Controls.Add(split);

        var ttPanel = new Panel();
        GuiTool.Setup(ttPanel);
        ttPanel.Name = "TagTablePanel";
        ttPanel.Padding = new Padding(pad);
        ttPanel.Dock = DockStyle.Left;
        data.TagTable.Name = "TagTable";
        data.TagTable.Dock = DockStyle.Fill;
        ttPanel.Controls.Add(data.TagTable);
        TagView.Controls.Add(ttPanel);

        data.StatusBar.Name = "StatusBar";
        data.StatusBar.Dock = DockStyle.Top;
        TagView.Controls.Add(data.StatusBar);

        data.CommandLine.Name = "CommandLine";
        data.CommandLine.Dock = DockStyle.Top;
        data.CommandLine.Padding = new Padding(pad);
        TagView.Controls.Add(data.CommandLine);

        data.MainView.Add((int)Mode.ApplicationMode.Grid, TagView);
        data.MainView.Add((int)Mode.ApplicationMode.Single, TagView);

        data.Scan.Dock = DockStyle.Fill;
        data.MainView.Add((int)Mode.ApplicationMode.Scan, data.Scan);

        data.MainView.Dock = DockStyle.Fill;
        Controls.Add(data.MainView);

        var menu = MakeMenu(_Data);
        menu.Dock = DockStyle.Top;
        Controls.Add(menu);
    }

    private MenuStrip MakeMenu(Data data)
    {
        var menuStrip = new MenuStrip();

        // file

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

        // view

        var viewMenu = new ToolStripMenuItem("View");
        menuStrip.Items.Add(viewMenu);

        var vImages = new ToolStripMenuItem("Images");
        var vScan = new ToolStripMenuItem("Scan");
        viewMenu.DropDownItems.Add(vImages);
        viewMenu.DropDownItems.Add(vScan);

        vImages.Command = new Button(() => UserCommand.SetMode(data, data.Mode.Switch(Mode.ApplicationMode.Grid)) );
        vScan.Command = new Button(() => UserCommand.SetMode(data, data.Mode.Switch(Mode.ApplicationMode.Scan)) );

        // help

        var helpMenu = new ToolStripMenuItem("Help");
        menuStrip.Items.Add(helpMenu);

        var hDebug = new ToolStripMenuItem("Debug print");
        helpMenu.DropDownItems.Add(hDebug);

        hDebug.Command = new Button(() => System.Console.WriteLine($"Name: {ActiveControl?.Name}  Size: {ActiveControl?.Size}  Comp: {ActiveControl}"));

        return menuStrip;
    }

    private class Button : ICommand
    {
        private Action _Action;
        #pragma warning disable CS0067
        public event EventHandler? CanExecuteChanged;
        #pragma warning restore CS0067
        public Button(Action action) { _Action = action; }
        public bool CanExecute(object? _) { return true; }
        public void Execute(object? _) { _Action.Invoke(); }
    }

    public static void SetupActionDefinitions(KeyMap keyMap)
    {
        Action<ActionDef> def = keyMap.Add;

        def(new ActionDef("save", UserCommand.Save, "Save"));
        def(new ActionDef("backup", UserCommand.Backup, "Backup"));

        def(new ActionDef("mode/grid", (data) => UserCommand.SetMode(data, data.Mode.Switch(Mode.ApplicationMode.Grid))));
        def(new ActionDef("mode/single", (data) => UserCommand.SetMode(data, data.Mode.Switch(Mode.ApplicationMode.Single))));
        def(new ActionDef("mode/scan", (data) => UserCommand.SetMode(data, data.Mode.Switch(Mode.ApplicationMode.Scan))));
        def(new ActionDef("mode/browse", (data) => UserCommand.SetMode(data, data.Mode.Switch(Mode.InputMode.Browse))));
        def(new ActionDef("mode/command", (data) => UserCommand.SetMode(data, data.Mode.Switch(Mode.InputMode.Command))));

        def(new ActionDef("press-enter", UserCommand.PressEnter));

        def(new ActionDef("copy-image-to-clipboard", UserCommand.CursorImageToClipboard));
        def(new ActionDef("copy-path-to-clipboard", UserCommand.CursorPathToClipboard));

        def(new ActionDef("refresh", UserCommand.Refresh));

        def(new ActionDef("cursor/left", (data) => UserCommand.MoveCursor(data, -1)));
        def(new ActionDef("cursor/right", (data) => UserCommand.MoveCursor(data, 1)));
        def(new ActionDef("cursor/up", (data) => UserCommand.MoveCursor(data, 0, -1)));
        def(new ActionDef("cursor/down", (data) => UserCommand.MoveCursor(data, 0, 1)));

        def(new ActionDef("mark/clear", UserCommand.ClearMarked));
        def(new ActionDef("mark/all-visible", UserCommand.MarkVisible));

        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                int staticX = x;
                int staticY = y;
                def(new ActionDef($"mark/grid-{x}-{y}", (data) => data.ImagePanel.ToggleMarked(staticX, staticY)));
            }
        }

        Action<Data, int, int> toggleAndMove = (data, x, y) => {
            UserCommand.ToggleMarkCursor(data);
            UserCommand.MoveCursor(data, x, y);
        };

        def(new ActionDef("mark-and-move/left", (data) => toggleAndMove(data, -1, 0)));
        def(new ActionDef("mark-and-move/right", (data) => toggleAndMove(data, 1, 0)));
        def(new ActionDef("mark-and-move/up", (data) => toggleAndMove(data, 0, -1)));
        def(new ActionDef("mark-and-move/down", (data) => toggleAndMove(data, 0, 1)));

        def(new ActionDef("scroll/page-up", (data) => data.ImagePanel.MoveCursorPage(-1)));
        def(new ActionDef("scroll/page-down", (data) => data.ImagePanel.MoveCursorPage(1)));
        def(new ActionDef("scroll/top", (data) => data.EntryCollection.MoveCursor(-1000000)));
        def(new ActionDef("scroll/bottom", (data) => data.EntryCollection.MoveCursor(1000000)));

        def(new ActionDef("filter/pop", UserCommand.PopFilter));
    }

    public static void SetupKeyMap(KeyMap keyMap)
    {
        Action<KeyData> add = keyMap.Add;

        Mode GridCommand = new Mode(Mode.ApplicationMode.Grid, Mode.InputMode.Command);
        Mode GridBrowse = new Mode(Mode.ApplicationMode.Grid, Mode.InputMode.Browse);
        Mode SingleCommand = new Mode(Mode.ApplicationMode.Single, Mode.InputMode.Command);
        Mode SingleBrowse = new Mode(Mode.ApplicationMode.Single, Mode.InputMode.Browse);

        Func<Data, bool> isCmdClosed = (data) => { return !data.CommandLine.IsEnabled(); };

        // All modes

        add(new KeyData(null, Keys.Control | Keys.S, "save"));
        add(new KeyData(null, Keys.Control | Keys.B, "backup"));

        add(new KeyData(null, Keys.F1, "mode/grid"));
        add(new KeyData(null, Keys.F2, "mode/scan"));

        // All image modes

        foreach (var mode in new Mode[] { GridCommand, GridBrowse, SingleCommand, SingleBrowse })
        {
            add(new KeyData(mode, Keys.Enter, "press-enter"));

            add(new KeyData(mode, Keys.Control | Keys.C, "copy-image-to-clipboard"));
            add(new KeyData(mode, Keys.Control | Keys.Shift | Keys.C, "copy-path-to-clipboard"));

            add(new KeyData(mode, Keys.Control | Keys.R, "refresh"));

            add(new KeyData(mode, Keys.Alt | Keys.Left, "cursor/left"));
            add(new KeyData(mode, Keys.Alt | Keys.Right, "cursor/right"));
            add(new KeyData(mode, Keys.Alt | Keys.Up, "cursor/up"));
            add(new KeyData(mode, Keys.Alt | Keys.Down, "cursor/down"));

            add(new KeyData(mode, Keys.Control | Keys.Q, "mark/clear"));
            add(new KeyData(mode, Keys.Control | Keys.A, "mark/all-visible"));

            add(new KeyData(mode, Keys.Alt | Keys.Shift | Keys.Left, "mark-and-move/left"));
            add(new KeyData(mode, Keys.Alt | Keys.Shift | Keys.Right, "mark-and-move/right"));
            add(new KeyData(mode, Keys.Alt | Keys.Shift | Keys.Up, "mark-and-move/up"));
            add(new KeyData(mode, Keys.Alt | Keys.Shift | Keys.Down, "mark-and-move/down"));

            add(new KeyData(mode, Keys.PageUp, "scroll/page-up"));
            add(new KeyData(mode, Keys.PageDown, "scroll/page-down"));

            add(new KeyData(mode, Keys.Escape, "filter/pop"));
        }

        // Grid mode

        foreach (var mode in new Mode[] { GridCommand, GridBrowse })
        {
            add(new KeyData(mode, Keys.Tab, "mode/single"));

            List<List<Keys>> gridKeys = [[Keys.Q, Keys.W, Keys.E, Keys.R, Keys.T],
                                         [Keys.A, Keys.S, Keys.D, Keys.F, Keys.G],
                                         [Keys.Z, Keys.X, Keys.C, Keys.V, Keys.B]];

            for (int y = 0; y < gridKeys.Count; y++)
            {
                for (int x = 0; x < gridKeys[y].Count; x++)
                {
                    int staticX = x;
                    int staticY = y;
                    add(new KeyData(mode, Keys.Alt | gridKeys[y][x], $"mark/grid-{x}-{y}"));
                }
            }
        }

        // Single mode

        foreach (var mode in new Mode[] { SingleCommand, SingleBrowse })
        {
            add(new KeyData(mode, Keys.Tab, "mode/grid"));
        }

        // Command mode

        foreach (var mode in new Mode[] { GridCommand, SingleCommand })
        {
            add(new KeyData(mode, Keys.Control | Keys.Enter, "mode/browse"));

            add(new KeyData(mode, Keys.Alt | Keys.Home, "scroll/top"));
            add(new KeyData(mode, Keys.Alt | Keys.End, "scroll/bottom"));
        }

        // Browse mode

        foreach (var mode in new Mode[] { GridBrowse, SingleBrowse })
        {
            add(new KeyData(mode, Keys.Control | Keys.Enter, "mode/command"));

            add(new KeyData(mode, Keys.Home, "scroll/top"));
            add(new KeyData(mode, Keys.End, "scroll/bottom"));

            add(new KeyData(mode, Keys.Alt | Keys.Home, "scroll/top"));
            add(new KeyData(mode, Keys.Alt | Keys.End, "scroll/bottom"));

            add(new KeyData(mode, Keys.Left, "cursor/left", isCmdClosed));
            add(new KeyData(mode, Keys.Right, "cursor/right", isCmdClosed));
            add(new KeyData(mode, Keys.Up, "cursor/up", isCmdClosed));
            add(new KeyData(mode, Keys.Down, "cursor/down", isCmdClosed));

            add(new KeyData(mode, Keys.Space, "mark-and-move/right", isCmdClosed));

            add(new KeyData(mode, Keys.Shift | Keys.Left, "mark-and-move/left", isCmdClosed));
            add(new KeyData(mode, Keys.Shift | Keys.Right, "mark-and-move/right", isCmdClosed));
            add(new KeyData(mode, Keys.Shift | Keys.Up, "mark-and-move/up", isCmdClosed));
            add(new KeyData(mode, Keys.Shift | Keys.Down, "mark-and-move/down", isCmdClosed));
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
                if (_Data.KeyMap.Get(keyData.ActionId) is ActionDef def)
                    def.Action(_Data);
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

    private void ListenTagbagFileSet(TagbagFileSet ev)
    {
        if (ev.Tagbag == null)
            Text = "Tagbag";
        else
            Text = $"Tagbag - {TagbagUtil.GetRootDirectory(ev.Tagbag)}";
    }
}
