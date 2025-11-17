using System;
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

    public CommandLine(EventHub eventHub)
    {
        _EventHub = eventHub;

        var pad = 5;

        _StatusLabel = new RichLabel();
        _StatusLabel.Dock = DockStyle.Fill;
        _StatusLabel.Left = pad * 10;
        _StatusLabel.Font = new Font("Verdana", 14);
        Controls.Add(_StatusLabel);

        _TextBox = new TextBox();
        _TextBox.Dock = DockStyle.Left;
        _TextBox.Width = 300;
        _TextBox.Font = new Font("Arial", 16);
        _TextBox.Multiline = true;
        _TextBox.AcceptsTab = true;
        _TextBox.Height = _TextBox.Font.Height + 4;
        Controls.Add(_TextBox);

        _ModeLabel = new Label();
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
        SetMode(CommandLineMode.FilterMode);

        eventHub.Log += ListenLog;
    }

    private void HandleKey(Object? o, KeyEventArgs e)
    {
        switch (e.KeyData)
        {
            case Keys.Enter:
                e.SuppressKeyPress = true;
                var txt = _TextBox.Text;
                if (txt.Length > 0)
                {
                    _TextBox.Text = "";
                    if (_Mode == CommandLineMode.FilterMode)
                        PerformFilterCommand(txt);
                    else
                        PerformTagCommand(txt);
                }
                break;
        }
    }

    private void PerformFilterCommand(string text)
    {
        try
        {
            var filter = FilterBuilder.Build(text);
            _EventHub.Send(new FilterCommand(filter));
        }
        catch (BuildException e)
        {
            System.Console.WriteLine($"Filter failed: {e}");
        }
    }

    private void PerformTagCommand(string text)
    {
        try
        {
            var tagOperation = TagBuilder.Build(text);
            _EventHub.Send(new TagCommand(tagOperation));
        }
        catch (BuildException e)
        {
            System.Console.WriteLine($"Tag failed {e}");
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

    public void SetEnabled(bool enabled)
    {
        if (_Enabled != enabled)
        {
            _Enabled = enabled;
            _TextBox.Enabled = enabled;
            UpdateModeLabel();
        }
    }

    public void SetMode(CommandLineMode mode)
    {
        if (_Mode != mode)
        {
            _Mode = mode;
            switch (mode)
            {
                case CommandLineMode.TagMode:
                    _ModeLabel.Text = "TAG";
                    break;

                case CommandLineMode.FilterMode:
                    _ModeLabel.Text = "FILTER";
                    break;
            }
            UpdateModeLabel();
        }
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
                _StatusLabel.AddText("Info", Color.Green);
                break;

            case LogType.Error:
                _StatusLabel.AddText("Error", Color.Red);
                break;
        }

        _StatusLabel.AddText(" ");
        _StatusLabel.AddText(log.Message, Color.Black);
    }
}
