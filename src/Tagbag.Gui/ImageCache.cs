using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tagbag.Gui;

public class ImageCache
{
    private Tagbag.Core.Tagbag? _Tagbag;
    private ConcurrentDictionary<Guid, Task<Bitmap?>> _ImageCache;
    private ConcurrentDictionary<Guid, Task<Bitmap?>> _ThumbnailCache;
    private ConcurrentStack<Task<Bitmap?>> _TaskStack;
    private ConcurrentStack<Task<Bitmap?>> _TaskHighPrio;
    private Thread _Worker;
    private bool _Running;

    private decimal _ThumbnailWidth;
    private decimal _ThumbnailHeight;

    public ImageCache(EventHub eventHub)
    {
        _Tagbag = null;
        _ImageCache = new ConcurrentDictionary<Guid, Task<Bitmap?>>();
        _ThumbnailCache = new ConcurrentDictionary<Guid, Task<Bitmap?>>();
        _TaskStack = new ConcurrentStack<Task<Bitmap?>>();
        _TaskHighPrio = new ConcurrentStack<Task<Bitmap?>>();
        _Worker = new Thread(WorkerFunction);
        _Running = true;

        _ThumbnailWidth = 300;
        _ThumbnailHeight = 300;

        eventHub.Shutdown += (_) => { _Running = false; };

        _Worker.Start();
    }

    public void SetTagbag(Tagbag.Core.Tagbag? tb)
    {
        _Tagbag = tb;
        _ImageCache.Clear();
        _ThumbnailCache.Clear();
        _TaskStack.Clear();
        _TaskHighPrio.Clear();
    }

    // Loads the image for the given entry id. If prio is true the
    // work of loading the image is assigned to the high priority
    // queue.
    public Task<Bitmap?> GetImage(Guid id, bool prio = false)
    {
        var task = GetOrLoad(id, _ImageCache);
        if (prio && !task.IsCompleted)
            _TaskHighPrio.Push(task);
        return task.ContinueWith(CopyBitmap).Unwrap();
    }

    // Function as GetImage but also resizes the image to fit as a
    // thumbnail.
    public Task<Bitmap?> GetThumbnail(Guid id, bool prio = false)
    {
        var task = GetOrLoad(id, _ThumbnailCache, ResizeAsThumbnail);
        if (prio && !task.IsCompleted)
            _TaskHighPrio.Push(task);
        return task.ContinueWith(CopyBitmap).Unwrap();
    }

    private Task<Bitmap?> GetOrLoad(
        Guid id,
        ConcurrentDictionary<Guid, Task<Bitmap?>> cache,
        Func<Bitmap, Bitmap>? transform = null)
    {
        Task<Bitmap?>? existing;
        if (cache.TryGetValue(id, out existing))
            return existing;

        Task<Bitmap?>? task;
        if (transform != null)
        {
            task = new Task<Bitmap?>(() => {
                if (LoadImage(id) is Bitmap image)
                    return transform(image);
                return null;
            });
        }
        else
        {
            task = new Task<Bitmap?>(() => LoadImage(id));
        }

        if (cache.TryAdd(id, task))
        {
            _TaskStack.Push(task);
            return task;
        }
        else if (cache.TryGetValue(id, out existing))
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
        if (_Tagbag != null)
        {
            var entry = _Tagbag.Get(id);
            if (entry != null)
            {
                var path = Tagbag.Core.TagbagUtil.GetPath(_Tagbag, entry.Path);
                if (File.Exists(path))
                    return new Bitmap(path);
            }
        }
        return null;
    }

    private Bitmap ResizeAsThumbnail(Bitmap original)
    {
        var scale = Math.Min(_ThumbnailWidth / original.Width,
                             _ThumbnailHeight / original.Height);
        var w = (int)(original.Width * scale);
        var h = (int)(original.Height * scale);

        var thumbnail = new Bitmap(original, w, h);
        original.Dispose();
        return thumbnail;
    }

    private void WorkerFunction()
    {
        Task<Bitmap?>? task;
        while (_Running)
        {
            if (_TaskHighPrio.TryPop(out task) || _TaskStack.TryPop(out task))
            {
                if (!task.IsCompleted)
                {
                    task.RunSynchronously();
                    task.Wait();
                }
            }
            else
            {
                Thread.Sleep(250);
            }
        }
    }
}
