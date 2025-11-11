using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tagbag.Gui;

public class ImageCache
{
    private Tagbag.Core.Tagbag _Tagbag;
    private ConcurrentDictionary<Guid, Task<Bitmap?>> _ImageCache;
    private ConcurrentDictionary<Guid, Task<Bitmap?>> _ThumbnailCache;
    private ConcurrentStack<Task<Bitmap?>> _TaskStack;
    private Thread _Worker;
    private bool _Running;

    private decimal _ThumbnailWidth;
    private decimal _ThumbnailHeight;

    public ImageCache(Tagbag.Core.Tagbag tb,
                      EventHub eventHub)
    {
        _Tagbag = tb;
        _ImageCache = new ConcurrentDictionary<Guid, Task<Bitmap?>>();
        _ThumbnailCache = new ConcurrentDictionary<Guid, Task<Bitmap?>>();
        _TaskStack = new ConcurrentStack<Task<Bitmap?>>();
        _Worker = new Thread(WorkerFunction);
        _Running = true;

        _ThumbnailWidth = 300;
        _ThumbnailHeight = 300;

        _Worker.Start();

        eventHub.Shutdown += (_) => { _Running = false; };
    }

    public Task<Bitmap?> GetImage(Guid id)
    {
        return GetImage(id).ContinueWith(CopyBitmap).Unwrap();
    }

    private Task<Bitmap?> GetImageBase(Guid id)
    {
        Task<Bitmap?>? existing;
        if (_ImageCache.TryGetValue(id, out existing))
            return existing;

        Task<Bitmap?> task = new Task<Bitmap?>(() => LoadImage(id));
        if (_ImageCache.TryAdd(id, task))
        {
            _TaskStack.Push(task);
            return task;
        }
        else if (_ImageCache.TryGetValue(id, out existing))
        {
            return existing;
        }
        else
        {
            throw new InvalidOperationException("Unable to determine cache status");
        }
    }

    public Task<Bitmap?> GetThumbnail(Guid id)
    {
        return GetThumbnailBase(id).ContinueWith(CopyBitmap).Unwrap();
    }

    private Task<Bitmap?> GetThumbnailBase(Guid id)
    {
        Task<Bitmap?>? existing;
        if (_ThumbnailCache.TryGetValue(id, out existing))
            return existing;

        Task<Bitmap?> loadTask  = new Task<Bitmap?>(() => LoadImage(id));
        Task<Bitmap?> task = loadTask.ContinueWith(IntoThumbnail).Unwrap();
        if (_ThumbnailCache.TryAdd(id, task))
        {
            _TaskStack.Push(loadTask);
            return task;
        }
        else if (_ThumbnailCache.TryGetValue(id, out existing))
        {
            return existing;
        }
        else
        {
            throw new InvalidOperationException("Unable to determine cache status");
        }
    }

    private async Task<Bitmap?> CopyBitmap(Task<Bitmap?> task)
    {
        if (await task is Bitmap image)
            lock(image)
                return new Bitmap(image);
        return null;
    }

    private Bitmap? LoadImage(Guid id)
    {
        var entry = _Tagbag.Get(id);
        if (entry != null)
        {
            var path = Tagbag.Core.TagbagUtil.GetPath(_Tagbag, entry.Path);
            if (File.Exists(path))
                return new Bitmap(path);
        }
        return null;
    }

    private async Task<Bitmap?> IntoThumbnail(Task<Bitmap?> image)
    {
        if (await image is Bitmap original)
        {
            var scale = Math.Min(_ThumbnailWidth / original.Width,
                                 _ThumbnailHeight / original.Height);
            var w = (int)(original.Width * scale);
            var h = (int)(original.Height * scale);

            var thumbnail = new Bitmap(original, w, h);
            original.Dispose();
            return thumbnail;
        }
        return null;
    }

    private void WorkerFunction()
    {
        Task<Bitmap?>? task;
        while (_Running)
        {
            if (_TaskStack.TryPop(out task))
            {
                task.RunSynchronously();
                task.Wait();
            }
            else
            {
                Thread.Sleep(250);
            }
        }
    }
}
