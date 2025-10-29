using System;
using Tagbag.Gui.Components;

namespace Tagbag.Gui;

public static class Command
{
    public static void SwapMode(Data data, Mode mode)
    {
        data.KeyMap.SwapMode(mode);

        data.Report($"Mode: {mode}");

        switch (mode)
        {
            case Mode.GridMode:
                data.CommandLine.SetEnabled(false);
                break;

            case Mode.CommandMode:
                data.CommandLine.SetEnabled(true);
                data.CommandLine.Focus();
                break;
        }
    }

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
