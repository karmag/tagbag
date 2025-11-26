using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Tagbag.Core.Input;

namespace Tagbag.Gui.Components;

public enum CommandLineMode
{
    TagMode,
    FilterMode,
}

public class CommandLine : Panel
{
    private EventHub _EventHub;
    private CommandLineMode _Mode;
    private bool _Enabled;

    private Label _ModeLabel;
    private TextBox _TextBox;
    private RichLabel _StatusLabel;

    private History _TagHistory;
    private History _FilterHistory;
    private History _CurrentHistory;

    public CommandLine(EventHub eventHub)
    {
        _EventHub = eventHub;

        var pad = 5;

        _StatusLabel = new RichLabel();
        _StatusLabel.Name = "CommandLine:StatusLabel";
        _StatusLabel.Dock = DockStyle.Fill;
        _StatusLabel.Left = pad * 10;
        _StatusLabel.Font = new Font("Verdana", 14);
        Controls.Add(_StatusLabel);

        _TextBox = new TextBox();
        _TextBox.Name = "CommandLine:TextBox";
        _TextBox.Dock = DockStyle.Left;
        _TextBox.Width = 300;
        _TextBox.Font = new Font("Arial", 16);
        _TextBox.Multiline = true;
        _TextBox.AcceptsTab = true;
        _TextBox.Height = _TextBox.Font.Height + 4;
        Controls.Add(_TextBox);

        _ModeLabel = new Label();
        _ModeLabel.Name = "CommandLine:ModeLabel";
        _ModeLabel.Dock = DockStyle.Left;
        _ModeLabel.Top = pad;
        _ModeLabel.Left = pad;
        _ModeLabel.Font = new Font("Arial", 14);
        Controls.Add(_ModeLabel);

        BackColor = Color.DarkGray;
        ClientSizeChanged += (_, _) =>
        {
            _TextBox.Width = Math.Max(300, Width / 4);
            Height = _TextBox.Font.Height + 4 + 2 * pad;
        };

        _TextBox.KeyDown += HandleKey;

        _TagHistory = new History();
        _FilterHistory = new History();
        _CurrentHistory = _TagHistory;

        SetMode(CommandLineMode.FilterMode);
        SetEnabled(true);

        eventHub.Log += ListenLog;

        GuiTool.Setup(this);
        GuiTool.Setup(_StatusLabel);
        GuiTool.Setup(_TextBox);
        GuiTool.Setup(_ModeLabel);
    }

    public void SetEnabled(bool enabled)
    {
        _Enabled = enabled;
        _TextBox.Enabled = enabled;
        UpdateModeLabel();
    }

    public bool IsEnabled()
    {
        return _Enabled;
    }

    public void SetMode(CommandLineMode mode)
    {
        _Mode = mode;
        switch (mode)
        {
            case CommandLineMode.TagMode:
                _ModeLabel.Text = "TAG";
                _TagHistory.Reset();
                _CurrentHistory = _TagHistory;
                break;

            case CommandLineMode.FilterMode:
                _ModeLabel.Text = "FILTER";
                _FilterHistory.Reset();
                _CurrentHistory = _FilterHistory;
                break;
        }
        UpdateModeLabel();
    }

    public CommandLineMode GetMode()
    {
        return _Mode;
    }

    new public void Focus()
    {
        _TextBox.Focus();
    }

    private void ListenLog(Log log)
    {
        _StatusLabel.Clear();
        _StatusLabel.AddText(" ");

        switch (log.Type)
        {
            case LogType.Info:
                //_StatusLabel.AddText("Info", Color.Green);
                break;

            case LogType.Error:
                _StatusLabel.AddText("ERROR", Color.Red);
                break;
        }

        _StatusLabel.AddText(" ");
        _StatusLabel.AddText(log.Message, Color.Black);
    }

    public void PerformCommand()
    {
        var txt = _TextBox.Text;
        if (txt.Trim().Length == 0)
        {
            _TextBox.Text = "";
        }
        else if (txt.Length > 0)
        {
            _CurrentHistory.Add(txt);
            try
            {
                if (_Mode == CommandLineMode.FilterMode)
                {
                    var filter = FilterBuilder.Build(txt);
                    _EventHub.Send(new FilterCommand(filter));
                }
                else
                {
                    var tagOperation = TagBuilder.Build(txt);
                    _EventHub.Send(new TagCommand(tagOperation));
                }
                _TextBox.Text = "";
            }
            catch (BuildException e)
            {
                _EventHub.Send(new Log(LogType.Error, e.FullMessage()));
            }
        }
    }

    private void HandleKey(Object? o, KeyEventArgs e)
    {
        switch (e.KeyData)
        {
            case Keys.Enter:
                e.SuppressKeyPress = true;
                PerformCommand();
                break;

            case Keys.Up:
                e.SuppressKeyPress = true;
                if (_CurrentHistory.Next() is string s)
                    _TextBox.Text = s;
                break;

            case Keys.Down:
                e.SuppressKeyPress = true;
                _TextBox.Text = _CurrentHistory.Prev();
                break;
        }
    }

    private void UpdateModeLabel()
    {
        if (_Enabled)
        {
            switch (_Mode)
            {
                case CommandLineMode.TagMode:
                    _ModeLabel.BackColor = Color.Pink;
                    break;
                case CommandLineMode.FilterMode:
                    _ModeLabel.BackColor = Color.LightGreen;
                    break;
            }
        }
        else
        {
            _ModeLabel.BackColor = Color.DarkGray;
        }
    }

    private class History
    {
        private LinkedList<String> _History;
        private LinkedListNode<String>? _ScrollBackPosition;

        public History()
        {
            _History = new LinkedList<String>();
            _ScrollBackPosition = null;
        }

        public void Reset()
        {
            _ScrollBackPosition = null;
        }

        public void Add(string cmd)
        {
            cmd = cmd.Trim();
            _History.Remove(cmd);
            _History.AddFirst(cmd);
            _ScrollBackPosition = null;
        }

        public string? Next()
        {
            if (_ScrollBackPosition == null)
                _ScrollBackPosition = _History.First;
            else
                _ScrollBackPosition = _ScrollBackPosition.Next ?? _ScrollBackPosition;

            return _ScrollBackPosition?.Value;
        }

        public string? Prev()
        {
            if (_ScrollBackPosition == null)
                return null;
            else
                _ScrollBackPosition = _ScrollBackPosition.Previous;

            return _ScrollBackPosition?.Value;
        }
    }
}
