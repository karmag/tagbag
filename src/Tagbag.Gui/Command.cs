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

    public static void ToggleMarked(Data data, int x, int y)
    {
        if (data.ImageGrid.GetCell(x, y) is ImageCell cell)
        {
            if (cell.GetKey() is Guid id)
            {
                var marked = data.EntryCollection.IsMarked(id);
                data.EntryCollection.SetMarked(id, !marked);
                cell.SetIsMarked(!marked);
            }
        }
    }
}
