using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Tagbag.Core.Input;

namespace Tagbag.Gui.Components;

public class CommandLine : Panel
{
    private EventHub _EventHub;
    private bool _Enabled;

    private Label _ModeLabel;
    private TextBox _TextBox;
    private RichLabel _StatusLabel;

    private History _History;
    private string _FilterPrefix = ":";
    private bool _IsTagMode;

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

        _TextBox.KeyDown += HandleKeyDown;
        _TextBox.KeyUp += HandleKeyUp;

        _History = new History();

        SetEnabled(true);
        UpdateMode(force: true);

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
        UpdateMode(force: true);
    }

    public bool IsEnabled()
    {
        return _Enabled;
    }

    new public void Focus()
    {
        _TextBox.Focus();
    }

    private void ListenLog(Log log)
    {
        _StatusLabel.Clear();
        _StatusLabel.AddText(" ");
        _StatusLabel.AddText(System.DateTime.Now.ToShortTimeString(), Color.Gray);

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
            _History.Add(txt);
            try
            {
                txt = txt.TrimStart();
                if (txt.StartsWith(_FilterPrefix))
                {
                    txt = txt.Substring(_FilterPrefix.Length);
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

    private void HandleKeyDown(Object? o, KeyEventArgs e)
    {
        switch (e.KeyData)
        {
            case Keys.Enter:
                e.SuppressKeyPress = true;
                PerformCommand();
                break;

            case Keys.Up:
                e.SuppressKeyPress = true;
                if (_History.Next() is string s)
                    _TextBox.Text = s;
                break;

            case Keys.Down:
                e.SuppressKeyPress = true;
                _TextBox.Text = _History.Prev();
                break;
        }
    }

    private void HandleKeyUp(Object? o, KeyEventArgs e)
    {
        UpdateMode();
    }

    private void UpdateMode(bool force = false)
    {
        var tagMode = !_TextBox.Text.TrimStart().StartsWith(_FilterPrefix);

        if (tagMode == _IsTagMode && !force)
            return;

        _IsTagMode = tagMode;
        if (_Enabled)
        {
            if (_IsTagMode)
            {
                _ModeLabel.BackColor = Color.Pink;
                _ModeLabel.Text = "Tag";
            }
            else
            {
                _ModeLabel.BackColor = Color.LightGreen;
                _ModeLabel.Text = "Filter";
            }
        }
        else
        {
            _ModeLabel.BackColor = Color.LightGray;
            _ModeLabel.Text = "-";
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
