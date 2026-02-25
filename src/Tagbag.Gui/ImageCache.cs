using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tagbag.Gui;

public class ImageCache
{
    private decimal _ThumbnailWidth;
    private decimal _ThumbnailHeight;
    private int _MaxImages;
    private int _MaxThumbnails;

    private Tagbag.Core.Tagbag? _Tagbag;
    private ConcurrentDictionary<Guid, Task<Bitmap?>> _ImageCache;
    private ConcurrentDictionary<Guid, Task<Bitmap?>> _ThumbnailCache;
    private ConcurrentStack<Task<Bitmap?>> _TaskStack;
    private ConcurrentStack<Task<Bitmap?>> _TaskHighPrio;
    private RecencyQueue<Guid> _RecentImages;
    private RecencyQueue<Guid> _RecentThumbnails;

    private Thread _Worker;
    private bool _Running;

    public ImageCache(EventHub eventHub)
    {
        _ThumbnailWidth = 300;
        _ThumbnailHeight = 300;
        _MaxImages = 10;
        _MaxThumbnails = 100;

        _Tagbag = null;
        _ImageCache = new ConcurrentDictionary<Guid, Task<Bitmap?>>();
        _ThumbnailCache = new ConcurrentDictionary<Guid, Task<Bitmap?>>();
        _TaskStack = new ConcurrentStack<Task<Bitmap?>>();
        _TaskHighPrio = new ConcurrentStack<Task<Bitmap?>>();
        _RecentImages = new RecencyQueue<Guid>(_MaxImages, (guid) => {
            Task<Bitmap?>? task;
            _ImageCache.TryRemove(guid, out task);
        });
        _RecentThumbnails = new RecencyQueue<Guid>(_MaxThumbnails, (guid) => {
            Task<Bitmap?>? task;
            _ThumbnailCache.TryRemove(guid, out task);
        });

        _Worker = new Thread(WorkerFunction);
        _Running = true;

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
        var task = GetOrLoad(id, _ImageCache, _RecentImages);
        if (prio && !task.IsCompleted)
            _TaskHighPrio.Push(task);
        return task.ContinueWith(CopyBitmap).Unwrap();
    }

    // Function as GetImage but also resizes the image to fit as a
    // thumbnail.
    public Task<Bitmap?> GetThumbnail(Guid id, bool prio = false)
    {
        var task = GetOrLoad(id, _ThumbnailCache, _RecentThumbnails, ResizeAsThumbnail);
        if (prio && !task.IsCompleted)
            _TaskHighPrio.Push(task);
        return task.ContinueWith(CopyBitmap).Unwrap();
    }

    private Task<Bitmap?> GetOrLoad(
        Guid id,
        ConcurrentDictionary<Guid, Task<Bitmap?>> cache,
        RecencyQueue<Guid> recent,
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
            recent.AddAndPop(id);
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
                    using (var stream = File.Open(path, FileMode.Open))
                        return new Bitmap(stream);
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

    private class RecencyQueue<T> where T : IComparable<T>
    {
        private int _Max;
        private Action<T>? _CleanupFunction;

        private HashSet<T> _Existence;
        private List<T?> _Ordered;
        private int _Index;

        // When the queue becomes filled AddAndPop will call the
        // cleanup function with the item being removed from the
        // queue.
        public RecencyQueue(int max, Action<T>? cleanupFunction = null)
        {
            _Max = max;
            _CleanupFunction = cleanupFunction;

            _Existence = new HashSet<T>();
            _Ordered = new List<T?>();
            _Index = 0;

            for (int i = 0; i < max; i++)
                _Ordered.Add(default(T));
        }

        // Adds the given item to the Queue. Returns the item that was
        // pushed out, if any.
        public T? AddAndPop(T t)
        {
            lock (this)
            {
                if (_Existence.Contains(t))
                {
                    for (int i = 0; i < _Max; i++)
                    {
                        if (t.Equals(_Ordered[i]))
                        {
                            // TODO: just dumping the element isn't
                            // enough, need to squash the remaining
                            // elements together as well
                            _Ordered[i] = default(T);
                            break;
                        }
                    }
                }
                else
                {
                    _Existence.Add(t);
                }

                var old = _Ordered[_Index % _Max];
                _Ordered[_Index % _Max] = t;
                _Index++;
                if (old is T oldT)
                {
                    _Existence.Remove(oldT);
                    _CleanupFunction?.Invoke(oldT);
                }

                return old;
            }
        }

        public void Clear()
        {
            _Existence.Clear();
            for (int i = 0; i < _Max; i++)
                _Ordered[i] = default(T);
            _Index = 0;
        }
    }
}
