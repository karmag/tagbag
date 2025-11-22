using System.Collections.Generic;
using System.Windows.Forms;

namespace Tagbag.Gui.Components;

public class CardPanel : Control
{
    private Dictionary<int, Control> _Controls;

    public CardPanel()
    {
        _Controls = new Dictionary<int, Control>();
    }

    public void Add(int id, Control control)
    {
        _Controls.Add(id, control);
        if (_Controls.Count == 1)
            Show(id);
    }

    public bool Show(int id)
    {
        Control? ctrl;
        if (_Controls.TryGetValue(id, out ctrl))
        {
            Controls.Clear();
            Controls.Add(ctrl);
            return true;
        }

        return false;
    }
}
