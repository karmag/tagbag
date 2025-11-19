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
        System.Console.WriteLine($"Mode :: {data.Mode} -> {mode}");
        data.Mode = mode;
        switch (mode.Application)
        {
            case Mode.ApplicationMode.Grid:
                data.ImagePanel.ShowGrid(true);
                break;

            case Mode.ApplicationMode.Single:
                data.ImagePanel.ShowGrid(false);
                break;
        }

        switch (mode.Input)
        {
            case Mode.InputMode.Command:
                data.CommandLine.SetEnabled(true);
                data.CommandLine.Focus();
                break;

            case Mode.InputMode.Browse:
                break;
        }

        data.KeyMap.SetMode(mode);
    }

    public static void SetCommandMode(Data data, CommandLineMode mode)
    {
        data.CommandLine.SetMode(mode);
        data.CommandLine.Focus();
    }

    // Moves the cursor in either x or y. The cursor is only moved one
    // step and in one cardinal direction. Values other than -1, 0, 1
    // are changed to be in that interval.
    public static void MoveCursor(Data data, int xDelta, int yDelta = 0)
    {
        if (yDelta == 0)
        {
            var xOffset = Math.Min(1, Math.Max(-1, xDelta));
            if (data.EntryCollection.GetCursor() is int index)
                data.EntryCollection.SetCursor(index + xOffset);
        }
        else
        {
            var yOffset = Math.Min(1, Math.Max(-1, yDelta));
            data.ImagePanel.MoveCursorRow(yOffset);
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

    public static void MarkVisible(Data data)
    {
        if (data.Mode.Application == Mode.ApplicationMode.Single)
            ToggleMarkCursor(data);
        else
            data.EntryCollection.MarkVisible();
    }

    public static void PopFilter(Data data)
    {
        data.EntryCollection.PopFilter();
    }

    public static void Save(Data data)
    {
        data.Tagbag?.Save();
        data.EventHub.Send(new Log(LogType.Info, "File saved"));
    }

    public static void Backup(Data data)
    {
        data.Tagbag?.Backup();
        data.EventHub.Send(new Log(LogType.Info, "Backup created"));
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
                data.EventHub.Send(new Log(LogType.Info,
                                           "Copied image to clipboard"));
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
            data.EventHub.Send(new Log(LogType.Info,
                                       "Copied path to clipboard"));
        }
    }

    // Enables and focuses the command line as necessary for user
    // input to be accepted.
    public static void OpenCommandLine(Data data)
    {
        data.CommandLine.SetEnabled(true);
        data.CommandLine.Focus();
    }

    public static void PressEnter(Data data)
    {
        if (data.CommandLine.IsEnabled())
        {
            data.CommandLine.PerformCommand();
            if (data.Mode.Input == Mode.InputMode.Browse)
                data.CommandLine.SetEnabled(false);
        }
        else
        {
            data.CommandLine.SetEnabled(true);
            data.CommandLine.Focus();
        }
    }
}
