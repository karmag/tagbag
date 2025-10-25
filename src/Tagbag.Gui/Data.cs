using Tagbag.Core;

namespace Tagbag.Gui;

public class Data
{
    public Tagbag.Core.Tagbag Tagbag { get; }
    public EntryCollection EntryCollection { get; }
    public ImageCache ImageCache { get; }

    public Data(Tagbag.Core.Tagbag tb)
    {
        Tagbag = tb;
        EntryCollection = new EntryCollection(tb);
        ImageCache = new ImageCache(tb);
    }
}
