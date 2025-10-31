using System;
using System.Drawing;
using System.Windows.Forms;

namespace Tagbag.Gui.Components;

public class CommandLine : Panel
{
    private Data _Data;
    private TextBox _TextBox;

    public CommandLine(Data data)
    {
        _Data = data;

        var pad = 5;

        _TextBox = new TextBox();
        _TextBox.Top = pad;
        _TextBox.Left = 100;
        Controls.Add(_TextBox);

        BackColor = Color.DarkGray;
        Font = new Font("Arial", 16);
        ClientSizeChanged += (_, _) =>
        {
            _TextBox.Width = Math.Max(300, Width / 2);
            Height = _TextBox.Height + 2 * pad;
        };

        _TextBox.KeyDown += HandleKey;
    }

    private void HandleKey(Object? o, KeyEventArgs e)
    {
        _Data.Report(e.KeyData.ToString());
        if (e.KeyData == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            _Data.Report("ENTER GOGO");
        }
    }
    
    public void SetEnabled(bool enabled)
    {
        _TextBox.Enabled = enabled;
    }

    new public void Focus()
    {
        _TextBox.Focus();
    }
}
