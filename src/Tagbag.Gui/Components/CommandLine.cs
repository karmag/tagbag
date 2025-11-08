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

    public CommandLine(EventHub eventHub)
    {
        _EventHub = eventHub;

        var pad = 5;

        _TextBox = new TextBox();
        _TextBox.Dock = DockStyle.Fill;
        Controls.Add(_TextBox);

        _ModeLabel = new Label();
        _ModeLabel.Dock = DockStyle.Left;
        _ModeLabel.Top = pad;
        _ModeLabel.Left = pad;
        Controls.Add(_ModeLabel);

        BackColor = Color.DarkGray;
        Font = new Font("Arial", 16);
        ClientSizeChanged += (_, _) =>
        {
            _TextBox.Width = Math.Max(300, Width / 2);
            Height = _TextBox.Height + 2 * pad;
        };

        _TextBox.KeyDown += HandleKey;
        SetMode(CommandLineMode.FilterMode);
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
}
