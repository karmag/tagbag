namespace Tagbag.Gui;

public class Data
{
    public Mode Mode;

    public EventHub EventHub;

    public Tagbag.Core.Tagbag? Tagbag;
    public EntryCollection EntryCollection { get; }
    public ImageCache ImageCache { get; }

    public KeyMap KeyMap;

    public string? LastActionId; // Internal helper variable
    // Variable for use by actions that want to make decisions based
    // on repeated action.
    public int ActionRepeatCount;

    public Components.CardPanel MainView;

    public Components.TagTable TagTable;
    public Components.ImagePanel ImagePanel;
    public Components.CommandLine CommandLine;
    public Components.StatusBar StatusBar;

    public Components.Scan Scan;

    public Data()
    {
        Mode = new Mode(Mode.ApplicationMode.Grid, Mode.InputMode.Command);

        EventHub = new EventHub();

        Tagbag = null;
        EntryCollection = new EntryCollection(EventHub);
        ImageCache = new ImageCache(EventHub);

        KeyMap = new KeyMap();

        MainView = new Components.CardPanel();

        TagTable = new Components.TagTable(EventHub, ImageCache);
        ImagePanel = new Components.ImagePanel(EventHub, EntryCollection, ImageCache);
        CommandLine = new Components.CommandLine(EventHub);
        StatusBar = new Components.StatusBar(EventHub, EntryCollection, ImagePanel);

        Scan = new Components.Scan(this);
    }

    public void SetTagbag(Tagbag.Core.Tagbag? tb)
    {
        Tagbag = tb;
        ImageCache.SetTagbag(tb);
        if (tb != null)
            EntryCollection.SetBaseEntries(tb.GetEntries());
        else
            EntryCollection.SetBaseEntries([]);

        EventHub.Send(new TagbagFileSet(tb));
    }
}

public class Mode
{
    public enum ApplicationMode
    {
        Grid = 1 << 2,
        Single = 1 << 3,
        Scan = 1 << 4,
    }

    public enum InputMode
    {
        Command = 1 << 0,
        Browse = 1 << 1,
    }

    public ApplicationMode Application { get; }
    public InputMode? Input { get => GetInput(); }
    private InputMode? _ActualInput;

    public Mode(ApplicationMode app, InputMode? input = null)
    {
        Application = app;
        _ActualInput = input;
    }

    public Mode Switch(ApplicationMode app) { return new Mode(app, _ActualInput); }
    public Mode Switch(InputMode input) { return new Mode(Application, input); }

    private InputMode? GetInput()
    {
        switch (Application)
        {
            case ApplicationMode.Grid:
            case ApplicationMode.Single:
                return _ActualInput;

            default:
                return null;
        }
    }
    
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
        if (Input is InputMode input)
            return (int)Application | (int)input;
        return (int)Application;
    }

    override public string? ToString()
    {
        var result = string.Empty;

        switch (Application)
        {
            case ApplicationMode.Grid:
                result = "Grid";
                break;
            case ApplicationMode.Single:
                result = "Single";
                break;
            case ApplicationMode.Scan:
                result = "Scan";
                break;
        }

        switch (Input)
        {
            case InputMode.Command:
                result += ", Command";
                break;
            case InputMode.Browse:
                result += ", Browse";
                break;
        }

        return $"Mode({result})";
    }
}
