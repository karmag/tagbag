using System.Collections.Generic;
using System.Windows.Forms;

namespace Tagbag.Gui.Components;

public class CardPanel : Panel
{
    private Dictionary<int, Control> _Controls;
    private int _Current;

    public CardPanel()
    {
        _Controls = new Dictionary<int, Control>();
        _Current = int.MinValue;
        GuiTool.Setup(this);
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
            if (_Current != id)
            {
                _Current = id;
                Controls.Clear();
                Controls.Add(ctrl);
                return true;
            }
        }

        return false;
    }

    public int GetCurrent()
    {
        return _Current;
    }
}
