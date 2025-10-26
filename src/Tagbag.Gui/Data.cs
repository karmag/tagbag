using System;
using Tagbag.Core;

namespace Tagbag.Gui;

public class Data
{
    public Tagbag.Core.Tagbag Tagbag { get; }
    public EntryCollection EntryCollection { get; }
    public ImageCache ImageCache { get; }
    public Action<Data, Event>? EventDispatcher;

    public Components.TagTable TagTable;
    public Components.ImageGrid ImageGrid;

    public Data(Tagbag.Core.Tagbag tb)
    {
        Tagbag = tb;
        EntryCollection = new EntryCollection(tb);
        ImageCache = new ImageCache(tb);

        TagTable = new Components.TagTable();
        ImageGrid = new Components.ImageGrid(this);
    }

    public void SendEvent(Event ev)
    {
        EventDispatcher?.Invoke(this, ev);
    }
}

public abstract record class Event();

public record CellClicked(Guid Id) : Event();
