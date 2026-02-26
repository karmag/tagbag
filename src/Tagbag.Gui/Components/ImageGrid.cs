using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Tagbag.Core;

namespace Tagbag.Gui.Components;

public class ImageGrid : Control
{
    private EventHub _EventHub;
    private EntryCollection _EntryCollection;
    private ImageCache _ImageCache;

    private bool _Active;
    private int _IndexOffset; // offset into EntryCollection

    // canvas size
    private int _Width;
    private int _Height;

    // tile properties
    private int _Rows = 3;
    private int _Columns = 3;
    private double _TileRatio = 1.3;
    private int _TileWidth = 100;
    private int _TileHeight = 100;
    private int _Padding = 2;

    private int _ThumbnailVersion = 0;
    private List<Bitmap?> _ThumbnailCache;

    public ImageGrid(EventHub eventHub,
                     EntryCollection entryCollection,
                     ImageCache imageCache)
    {
        _EventHub = eventHub;
        _EntryCollection = entryCollection;
        _ImageCache = imageCache;

        _ThumbnailCache = new List<Bitmap?>(_Rows * _Columns);

        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);

        ResizeRedraw = true;

        _EventHub.EntriesUpdated += ListenEntriesUpdated;

        _Active = false;
        SetActive(true);

        GuiTool.Setup(this);
    }

    private void RefreshThumbnailCache()
    {
        lock (this)
        {
            _ThumbnailVersion++;
            _ThumbnailCache.Clear();
            while (_ThumbnailCache.Count < _Rows * _Columns)
                _ThumbnailCache.Add(null);

            var gridSize = _Rows * _Columns;

            // Preload images for next page
            for (int i = 0; i < gridSize; i++)
                if (_EntryCollection.Get(_IndexOffset + gridSize + i) is Entry entry)
                    _ImageCache.GetThumbnail(entry.Id);

            // Load currently visibile images
            var staticVersion = _ThumbnailVersion;
            for (int i = gridSize; i >= 0; i--)
            {
                if (_EntryCollection.Get(_IndexOffset + i) is Entry entry)
                {
                    var task = _ImageCache.GetThumbnail(entry.Id);
                    if (task.IsCompleted)
                    {
                        _ThumbnailCache[i] = task.Result;
                    }
                    else
                    {
                        var staticIndex = i;
                        task.ContinueWith(
                            (task) => PopulateThumbnailIndex(staticVersion, staticIndex, task.Result));
                    }
                }
            }

            Invalidate();
            _EventHub.Send(new ViewChanged(_IndexOffset, _Rows * _Columns));
        }
    }

    private void PopulateThumbnailIndex(int version, int index, Bitmap? image)
    {
        lock (this)
        {
            if (version == _ThumbnailVersion)
            {
                _ThumbnailCache[index] = image;
                Invalidate();
            }
        }
    }

    // If necessary, updates the fields used for rendering.
    private void UpdateRenderingFields()
    {
        if (Size.Width != _Width || Size.Height != _Height)
        {
            var oldRows = _Rows;
            var oldColumns = _Columns;

            _Width = Size.Width;
            _Height = Size.Height;

            _TileHeight = _Height / _Rows;
            _Columns = Math.Max(1, (int)(_Width / (_TileHeight * _TileRatio)));
            _TileWidth = _Width / _Columns;

            if (_EntryCollection.GetCursor() is int index)
                _IndexOffset = (index / _Columns) * _Columns;
            else
                _IndexOffset = 0;

            if (_Rows != oldRows || _Columns != oldColumns)
                RefreshThumbnailCache();
        }
    }

    override protected void OnPaint(PaintEventArgs e)
    {
        UpdateRenderingFields();

        using var backBrush = new SolidBrush(Color.FromArgb(255, 50, 50, 50));
        using var tileBrush = new SolidBrush(Color.FromArgb(255, 80, 80, 80));
        using var markedBrush = new SolidBrush(Color.Red);
        using var markedBackBrush = new SolidBrush(Color.LightGray);
        using var cursorPen = new Pen(Color.Yellow, _Padding);

        e.Graphics.FillRectangle(backBrush, e.ClipRectangle);

        int cursorIndex = _EntryCollection.GetCursor() ?? -1;

        var tileRect = new Rectangle(0, 0, _TileWidth - _Padding * 2, _TileHeight - _Padding * 2);
        int counter = 0;
        for (int y = 0; y < _Rows; y++)
        {
            for (int x = 0; x < _Columns; x++)
            {
                tileRect.X = x * _TileWidth + _Padding;
                tileRect.Y = y * _TileHeight + _Padding;

                if (_EntryCollection.Get(_IndexOffset + counter) is Entry entry &&
                    _EntryCollection.IsMarked(entry.Id))
                {
                    e.Graphics.FillRectangle(markedBrush, tileRect);
                    var markSize = _Padding * 2;
                    e.Graphics.FillRectangle(markedBackBrush,
                                             tileRect.X + markSize,
                                             tileRect.Y + markSize,
                                             tileRect.Width - markSize * 2,
                                             tileRect.Height - markSize * 2);
                }
                else
                    e.Graphics.FillRectangle(tileBrush, tileRect);

                if (_IndexOffset + counter == cursorIndex)
                    e.Graphics.DrawRectangle(cursorPen, tileRect);

                if (_ThumbnailCache[counter] is Bitmap image)
                {
                    float xFactor = (float)(tileRect.Width - _Padding * 2) / image.Width;
                    float yFactor = (float)(tileRect.Height - _Padding * 2) / image.Height;
                    float factor = Math.Min(xFactor, yFactor);
                    factor = factor * (float)0.95;
                    var w = image.Width * factor;
                    var h = image.Height * factor;

                    e.Graphics.DrawImage(
                        image,
                        x * _TileWidth + (_TileWidth - w) / 2,
                        y * _TileHeight + (_TileHeight - h) / 2,
                        w,
                        h);
                }

                counter++;
            }
        }
    }

    public void SetActive(bool active)
    {
        if (active != _Active)
        {
            _Active = active;
            if (active)
            {
                _EventHub.CursorMoved += ListenCursorMoved;
                _EventHub.MarkedChanged += ListenMarkedChanged;
                _EventHub.Send(new CursorMoved(_EntryCollection.GetCursor()));
            }
            else
            {
                _EventHub.CursorMoved -= ListenCursorMoved;
                _EventHub.MarkedChanged -= ListenMarkedChanged;
            }
        }
    }

    public Entry? GetEntryAt(int x, int y)
    {
        if (x < _Columns && y < _Rows)
            return _EntryCollection.Get(_IndexOffset + x + y * _Columns);
        return null;
    }

    public void MoveCursor(int xDelta, int yDelta)
    {
        if (_EntryCollection.GetCursor() is int index)
        {
            var offset = xDelta + yDelta * _Columns;
            _EntryCollection.SetCursor(index + offset);
        }
    }

    public void MovePage(int pages)
    {
        var offset = pages * _Columns * _Rows;
        if (_IndexOffset + offset >= _EntryCollection.Size())
            return;

        _IndexOffset += offset;
        if (_IndexOffset < 0)
            _IndexOffset = 0;
        _IndexOffset = Math.Min(
            _IndexOffset,
            (_EntryCollection.Size() / _Columns - 1) * _Columns);

        RefreshThumbnailCache();
        if (_EntryCollection.GetCursor() is int index)
            _EntryCollection.SetCursor(index + offset);
    }

    private void ListenEntriesUpdated(EntriesUpdated _) { RefreshThumbnailCache(); }
    private void ListenMarkedChanged(MarkedChanged _) { Invalidate(); }

    private void ListenCursorMoved(CursorMoved ev)
    {
        if (ev.Index is int index)
        {
            var rowDiff = index / _Columns - _IndexOffset / _Columns;
            if (rowDiff < 0 || rowDiff >= _Rows)
            {
                if (rowDiff == -1)
                    _IndexOffset -= _Columns;
                else if (rowDiff == _Rows)
                    _IndexOffset += _Columns;
                else
                    _IndexOffset += rowDiff * _Columns;
                RefreshThumbnailCache();
            }
            Invalidate();
        }
    }

    public int GetVisibleStartIndex()
    {
        return _IndexOffset;
    }

    public int GetPageSize()
    {
        return _Rows * _Columns;
    }
}
