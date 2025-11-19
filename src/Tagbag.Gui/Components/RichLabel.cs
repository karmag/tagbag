using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Tagbag.Gui.Components;

public class RichLabel : RichTextBox
{
    public RichLabel()
    {
        BorderStyle = BorderStyle.None;
        Font = new Font("Verdana", 16);
        Height = 25;

        ReadOnly = true;
        Multiline = true;
        AcceptsTab = true;
        TabStop = false;
    }

    new public void Clear()
    {
        Text = "";
    }

    public void AddText(string text, Color? foreColor = null, Color? backColor = null)
    {
        var old = Text;
        AppendText(text);

        Select(old.Length, old.Length + text.Length);
        if (foreColor is Color fg)
            SelectionColor = fg;
        if (backColor is Color bg)
            SelectionBackColor = bg;
        Select(0, 0);
    }
}
