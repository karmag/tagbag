using System;
using System.Drawing;

namespace Tagbag.Gui;

public class ImageCache
{
    private Tagbag.Core.Tagbag _Tagbag;

    private UsageCache<Guid, Bitmap> _ImageCache;
    private UsageCache<Guid, Bitmap> _ThumbnailCache;

    private decimal _ThumbnailWidth;
    private decimal _ThumbnailHeight;

    public ImageCache(Tagbag.Core.Tagbag tb)
    {
        _Tagbag = tb;

        _ImageCache = new UsageCache<Guid, Bitmap>(10, CreateImage, DeleteBitmap);
        _ThumbnailCache = new UsageCache<Guid, Bitmap>(100, CreateThumbnail, DeleteBitmap);

        _ThumbnailWidth = 300;
        _ThumbnailHeight = 300;
    }

    public void SetThumbnailSize(int w, int h)
    {
        _ThumbnailWidth = w;
        _ThumbnailHeight = h;
        _ThumbnailCache.Clear();
    }

    public Bitmap? GetImage(Guid? id)
    {
        if (id is Guid realId)
            return _ImageCache.Get(realId);
        return null;
    }

    public Bitmap? GetThumbnail(Guid? id)
    {
        if (id is Guid realId)
            return _ThumbnailCache.Get(realId);
        return null;
    }

    private Bitmap CreateImage(Guid id)
    {
        var entry = _Tagbag.Get(id);
        if (entry != null)
        {
            var path = Tagbag.Core.TagbagUtil.GetPath(_Tagbag, entry.Path);
            return new Bitmap(path);
        }
        throw new InvalidOperationException($"No entry with id {id}");
    }

    private Bitmap CreateThumbnail(Guid id)
    {
        var original = CreateImage(id);

        var scale = Math.Min(_ThumbnailWidth / original.Width,
                             _ThumbnailHeight / original.Height);
        var w = (int)(original.Width * scale);
        var h = (int)(original.Height * scale);

        var thumbnail = new Bitmap(original, w, h);
        original.Dispose();
        return thumbnail;
    }

    private void DeleteBitmap(Guid id, Bitmap bitmap)
    {
        bitmap.Dispose();
    }
}
