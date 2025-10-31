using System;
using Tagbag.Core;

namespace Tagbag.Gui;

public class Data
{
    public Mode Mode;

    public Tagbag.Core.Tagbag Tagbag { get; }
    public EntryCollection EntryCollection { get; }
    public ImageCache ImageCache { get; }
    public Action<Data, Event>? EventDispatcher;

    public KeyMap KeyMap;

    public Components.TagTable TagTable;
    public Components.ImageGrid ImageGrid;
    public Components.CommandLine CommandLine;
    public Components.StatusBar StatusBar;

    public Data(Tagbag.Core.Tagbag tb)
    {
        Mode = Mode.GridMode;

        Tagbag = tb;
        EntryCollection = new EntryCollection(tb);
        ImageCache = new ImageCache(tb);

        KeyMap = new KeyMap();

        TagTable = new Components.TagTable();
        ImageGrid = new Components.ImageGrid(this);
        CommandLine = new Components.CommandLine(this);
        StatusBar = new Components.StatusBar();
    }

    public void SendEvent(Event ev)
    {
        EventDispatcher?.Invoke(this, ev);
    }

    public void Report(string msg)
    {
        StatusBar.SetText(msg);
        System.Console.WriteLine(msg);
    }
}

public enum Mode
{
    GridMode,
    CommandMode,
}

public abstract record class Event();

public record CellClicked(Guid Id) : Event();

public record RunTagCommand(string command) : Event();
