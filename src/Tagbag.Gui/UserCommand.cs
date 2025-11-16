using System;
using System.Windows;
using System.Windows.Forms;
using Tagbag.Core;
using Tagbag.Gui.Components;

namespace Tagbag.Gui;

public static class UserCommand
{
    public static void SetMode(Data data, Mode mode)
    {
        data.Mode = mode;
        switch (mode)
        {
            case Mode.BrowseMode:
                data.ImagePanel.ShowGrid(true);
                break;

            case Mode.SingleMode:
                data.ImagePanel.ShowGrid(false);
                break;
        }

        data.KeyMap.SwapMode(mode);
    }

    public static void SetCommandMode(Data data, CommandLineMode mode)
    {
        data.CommandLine.SetMode(mode);
        data.CommandLine.Focus();
    }

    public static void MoveCursor(Data data, int offset)
    {
        if (data.EntryCollection.GetCursor() is int index)
            data.EntryCollection.SetCursor(index + offset);
    }

    public static void MoveGridCursor(Data data, int xDelta, int yDelta)
    {
        if (data.Mode == Mode.BrowseMode)
            data.ImagePanel.ImageGrid.MoveCursor(xDelta, yDelta);
    }

    public static void MoveGridPage(Data data, int pages)
    {
        if (data.Mode == Mode.BrowseMode)
            data.ImagePanel.ImageGrid.MovePage(pages);
    }

    public static void ToggleGridMarked(Data data, int x, int y)
    {
        if (data.Mode == Mode.BrowseMode &&
            data.ImagePanel.ImageGrid.GetEntryAt(x, y) is Entry entry)
        {
            data.EntryCollection.SetMarked(
                entry.Id,
                !data.EntryCollection.IsMarked(entry.Id));
        }
    }

    public static void ToggleMarkCursor(Data data)
    {
        if (data.EntryCollection.GetEntryAtCursor()?.Id is Guid id)
        {
            data.EntryCollection.SetMarked(
                id,
                !data.EntryCollection.IsMarked(id));
        }
    }

    public static void ClearMarked(Data data)
    {
        data.EntryCollection.ClearMarked();
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

    public static void CursorImageToClipboard(Data data)
    {
        if (data.EntryCollection.GetEntryAtCursor() is Entry entry)
        {
            var task = data.ImageCache.GetImage(entry.Id, prio: true);
            var bitmap = task.Result;
            if (bitmap != null)
            {
                Clipboard.SetData(DataFormats.Bitmap, bitmap);
                data.Report($"Copied {entry.Path} to clipboard");
            }
        }
    }

    public static void CursorPathToClipboard(Data data)
    {
        if (data.EntryCollection.GetEntryAtCursor() is Entry entry &&
            data.Tagbag is Tagbag.Core.Tagbag tb)
        {
            var path = TagbagUtil.GetPath(tb, entry.Path);
            Clipboard.SetData(DataFormats.Text, path);
            data.Report($"Copied {entry.Path} to clipboard");
        }
    }
}
