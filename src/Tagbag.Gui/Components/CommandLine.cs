using System;
using System.Drawing;
using System.Windows.Forms;

namespace Tagbag.Gui.Components;

public enum CommandLineMode
{
    TagMode,
    FilterMode,
}

public class CommandLine : Panel
{
    private Data _Data;
    private CommandLineMode _Mode;
    private bool _Enabled;

    private Label _ModeLabel;
    private TextBox _TextBox;

    public CommandLine(Data data)
    {
        _Data = data;

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
        _Data.Report(e.KeyData.ToString());
        switch (e.KeyData)
        {
            case Keys.F1:
                SetMode(CommandLineMode.FilterMode);
                break;

            case Keys.F2:
                SetMode(CommandLineMode.TagMode);
                break;

            case Keys.Enter:
                e.SuppressKeyPress = true;
                var txt = _TextBox.Text;
                if (txt.Length > 0)
                {
                    _TextBox.Text = "";
                    if (_Mode == CommandLineMode.FilterMode)
                        _Data.SendEvent(new RunFilterCommand(txt));
                    else
                        _Data.SendEvent(new RunTagCommand(txt));
                }
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
