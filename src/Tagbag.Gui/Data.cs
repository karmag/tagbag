namespace Tagbag.Gui;

public class Data
{
    public Mode Mode;

    public EventHub EventHub;

    public Tagbag.Core.Tagbag? Tagbag;
    public EntryCollection EntryCollection { get; }
    public ImageCache ImageCache { get; }

    public KeyMap KeyMap;

    public Components.TagTable TagTable;
    public Components.ImagePanel ImagePanel;
    public Components.CommandLine CommandLine;
    public Components.StatusBar StatusBar;

    public Data()
    {
        Mode = Mode.BrowseMode;

        EventHub = new EventHub();

        Tagbag = null;
        EntryCollection = new EntryCollection(EventHub);
        ImageCache = new ImageCache(EventHub);

        KeyMap = new KeyMap();

        TagTable = new Components.TagTable(EventHub, ImageCache);
        ImagePanel = new Components.ImagePanel(EventHub, EntryCollection, ImageCache);
        CommandLine = new Components.CommandLine(EventHub);
        StatusBar = new Components.StatusBar(EventHub, EntryCollection);
    }

    public void SetTagbag(Tagbag.Core.Tagbag? tb)
    {
        Tagbag = tb;
        ImageCache.SetTagbag(tb);
        if (tb != null)
            EntryCollection.SetBaseEntries(tb.GetEntries());
        else
            EntryCollection.SetBaseEntries([]);
    }

    public void Report(string msg)
    {
        System.Console.WriteLine(msg);
    }
}

public enum Mode
{
    BrowseMode,
    SingleMode,
}
