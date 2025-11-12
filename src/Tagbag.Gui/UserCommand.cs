using System.Windows.Forms;
using Tagbag.Gui.Components;

namespace Tagbag.Gui;

public static class UserCommand
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

    public static void SetCommandMode(Data data, CommandLineMode mode)
    {
        data.CommandLine.SetMode(mode);
        data.CommandLine.Focus();
    }

    public static void MoveCursor(Data data, int xDelta, int yDelta)
    {
        data.ImageGrid.MoveCursor(xDelta, yDelta);
    }

    public static void MovePage(Data data, int pages)
    {
        data.ImageGrid.MovePage(pages);
    }

    public static void ToggleMarked(Data data, int x, int y)
    {
        System.Console.WriteLine("ToggleMarked not implemented");
    }

    public static void PopFilter(Data data)
    {
        data.EntryCollection.PopFilter();
    }

    public static void Save(Data data)
    {
        data.Tagbag?.Save();
    }

    public static void Backup(Data data)
    {
        data.Tagbag?.Backup();
    }

    public static void Quit(Data data)
    {
        Application.Exit();
    }
}
