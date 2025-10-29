using System;
using Tagbag.Gui.Components;

namespace Tagbag.Gui;

public static class Command
{
    public static void ToggleSelection(Data data, int x, int y)
    {
        if (data.ImageGrid.GetCell(x, y) is ImageCell cell)
        {
            if (cell.GetKey() is Guid id)
            {
                var selected = data.EntryCollection.IsSelected(id);
                data.EntryCollection.SetSelected(id, !selected);
                cell.SetIsSelected(!selected);
            }
        }
    }
}
