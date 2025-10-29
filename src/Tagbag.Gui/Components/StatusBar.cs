using System.Windows.Forms;

namespace Tagbag.Gui.Components;

public class StatusBar : Label
{
    private int _Counter;

    public void SetText(string msg)
    {
        _Counter++;
        Text = $"{_Counter} :: {msg}";
    }
}
