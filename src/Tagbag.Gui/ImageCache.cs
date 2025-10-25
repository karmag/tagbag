using System;
using System.Collections.Generic;
using System.Drawing;

namespace Tagbag.Gui;

public class ImageCache
{
    private Tagbag.Core.Tagbag _Tagbag;

    private Dictionary<Guid, Bitmap> _Images;
    private CircularList<Guid> _ImageCache;

    public ImageCache(Tagbag.Core.Tagbag tb)
    {
        _Tagbag = tb;

        _Images = new Dictionary<Guid, Bitmap>();
        _ImageCache = new CircularList<Guid>(20);
    }

    public Bitmap? GetImage(Guid key)
    {
        Bitmap? img;
        if (_Images.TryGetValue(key, out img))
            return img;

        var entry = _Tagbag.Get(key);
        if (entry != null)
        {
            var path = Tagbag.Core.TagbagUtil.GetPath(_Tagbag, entry.Path);
            img = new Bitmap(path);
            _Images[key] = img;
        }

        return img;
    }
}
