using System;
using System.Drawing;
using System.Windows.Forms;

namespace Tagbag.Gui.Components;

public class CommandLine : Panel
{
    private TextBox _TextBox;

    public CommandLine(int pad)
    {
        _TextBox = new TextBox();
        _TextBox.Top = pad;
        Controls.Add(_TextBox);

        BackColor = Color.DarkGray;
        Font = new Font("Courier New", 16);
        ClientSizeChanged += (_, _) =>
        {
            _TextBox.Width = Math.Max(300, Width / 2);
            _TextBox.Left = (Width - _TextBox.Width) / 2;
            Height = _TextBox.Height + 2 * pad;
        };
    }
}
