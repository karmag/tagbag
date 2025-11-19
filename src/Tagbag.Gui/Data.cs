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
        Mode = new Mode(Mode.ApplicationMode.Grid, Mode.InputMode.Command);

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
}

public class Mode
{
    public enum ApplicationMode
    {
        Grid = 1 << 0,
        Single = 1 << 1,
    }

    public enum InputMode
    {
        Command = 1 << 2,
        Browse = 1 << 3,
    }

    public ApplicationMode Application { get; }
    public InputMode Input { get; }

    public Mode(ApplicationMode app, InputMode input)
    {
        Application = app;
        Input = input;
    }

    public Mode Switch(ApplicationMode app) { return new Mode(app, Input); }
    public Mode Switch(InputMode input) { return new Mode(Application, input); }

    override public bool Equals(object? other)
    {
        if (other is Mode mode)
        {
            return Application == mode.Application && Input == mode.Input;
        }
        return false;
    }

    override public int GetHashCode()
    {
        return (int)Application | (int)Input;
    }

    override public string? ToString()
    {
        var app = string.Empty;
        var input = string.Empty;

        switch (Application)
        {
            case ApplicationMode.Grid:
                app = "Grid";
                break;
            case ApplicationMode.Single:
                app = "Single";
                break;
        }

        switch (Input)
        {
            case InputMode.Command:
                input = "Command";
                break;
            case InputMode.Browse:
                input = "Browse";
                break;
        }

        return $"Mode({app}, {input})";
    }
}
