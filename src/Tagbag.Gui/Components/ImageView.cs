using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tagbag.Core;

namespace Tagbag.Gui.Components;

public class ImageView : Panel
{
    private PictureBox _Picture;
    private EventHub _EventHub;
    private EntryCollection _EntryCollection;
    private ImageCache _ImageCache;

    private Entry? _Entry;

    public ImageView(EventHub eventHub,
                     EntryCollection entryCollection,
                     ImageCache imageCache)
    {
        _EventHub = eventHub;
        _EntryCollection = entryCollection;
        _ImageCache = imageCache;

        _Picture = new PictureBox();
        _Picture.Dock = DockStyle.Fill;
        _Picture.SizeMode = PictureBoxSizeMode.Zoom;
        Controls.Add(_Picture);

        SetActive(true);
    }

    public void SetActive(bool active)
    {
        if (active)
        {
            _EventHub.CursorMoved += ListenCursorMoved;
            ListenCursorMoved(new CursorMoved(null));
        }
        else
            _EventHub.CursorMoved -= ListenCursorMoved;
    }

    private void ListenCursorMoved(CursorMoved _)
    {
        _Entry = _EntryCollection.GetEntryAtCursor();
        if (_Entry is Entry entry)
        {
            var task = _ImageCache.GetImage(entry.Id, prio: true);
            if (task.IsCompleted)
            {
                _Picture.Image = task.Result;
            }
            else
            {
                _Picture.Image = null;
                var staticId = entry.Id;
                task.ContinueWith((task) => SetImage(staticId, task));
            }
        }
        else
        {
            _Picture.Image = null;
        }
    }

    private async void SetImage(Guid id, Task<Bitmap?> task)
    {
        if (id == _Entry?.Id)
            _Picture.Image = await task;
    }
}
