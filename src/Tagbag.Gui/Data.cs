using Tagbag.Core;

namespace Tagbag.Gui;

public class Data
{
    public Mode Mode;

    public EventHub EventHub;

    public Tagbag.Core.Tagbag Tagbag { get; }
    public EntryCollection EntryCollection { get; }
    public ImageCache ImageCache { get; }

    public KeyMap KeyMap;

    public Components.TagTable TagTable;
    public Components.ImageGrid ImageGrid;
    public Components.CommandLine CommandLine;
    public Components.StatusBar StatusBar;

    public Data(Tagbag.Core.Tagbag tb)
    {
        Mode = Mode.GridMode;

        EventHub = new EventHub();

        Tagbag = tb;
        EntryCollection = new EntryCollection(EventHub);
        ImageCache = new ImageCache(tb, EventHub);

        KeyMap = new KeyMap();

        TagTable = new Components.TagTable(EventHub, ImageCache);
        ImageGrid = new Components.ImageGrid(EventHub, EntryCollection, ImageCache);
        CommandLine = new Components.CommandLine(EventHub);
        StatusBar = new Components.StatusBar();
    }

    public void Report(string msg)
    {
        System.Console.WriteLine(msg);
        StatusBar.SetText(msg);
    }
}

public enum Mode
{
    GridMode,
    CommandMode,
}
