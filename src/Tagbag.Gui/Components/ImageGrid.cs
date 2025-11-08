using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Tagbag.Core;

namespace Tagbag.Gui.Components;

public class ImageGrid : TableLayoutPanel
{
    private int _Rows;
    private int _Columns;
    private double _CellRatio; // Height / width ratio for cells.
    private List<ImageCell> _Cells;
    private EventHub _EventHub;
    private EntryCollection _EntryCollection;
    private ImageCache _ImageCache;

    public ImageGrid(EventHub eventHub,
                     EntryCollection entryCollection,
                     ImageCache imageCache)
    {
        _Rows = 3;
        _Columns = 3;
        _CellRatio = 1.3;
        _Cells = new List<ImageCell>();

        _EventHub = eventHub;
        _EntryCollection = entryCollection;
        _ImageCache = imageCache;

        RowCount = _Rows;
        ColumnCount = _Columns;
        for (int i = 0; i < _Rows * _Columns; i++)
            _Cells.Add(new ImageCell(_EventHub, _ImageCache));

        Padding = new Padding(5);

        ClientSizeChanged += (_, _) => { LayoutChanged(_Rows); };
        _EventHub.EntriesUpdated += (_) => { ListenEntriesUpdated(); };
        _EventHub.CursorMoved += (ev) => { ListenCursorMoved(ev); };
    }

    private void LayoutChanged(int newRows)
    {
        newRows = Math.Max(1, newRows);

        int maxHeight = (Height
                         - Padding.Top - Padding.Bottom
                         - (Margin.Top + Margin.Bottom)*newRows) / newRows;
        int newColumns = Math.Max(1, (int)(Width / (maxHeight * _CellRatio)));
        int maxWidth = (Width
                        - Padding.Left - Padding.Right
                        - (Margin.Left + Margin.Right)*newColumns) / newColumns;

        if (newRows != RowCount || newColumns != ColumnCount)
        {
            var newCellCount = newRows * newColumns;
            var oldCellCount = RowCount * ColumnCount;

            var newCells = new List<ImageCell>(_Cells.Take(newCellCount));
            while (newCells.Count < newCellCount)
                newCells.Add(new ImageCell(_EventHub, _ImageCache));
            _Cells = newCells;

            RowCount = newRows;
            ColumnCount = newColumns;

            Controls.Clear();
            foreach (var cell in _Cells)
                Controls.Add(cell);
        }

        bool foundCursor = false;
        foreach (var cell in _Cells)
        {
            cell.SetSize(maxWidth, maxHeight);
            if (cell.IsCursor())
                foundCursor = true;
        }
        if (!foundCursor)
        {
            _Cells[0].SetIsCursor(true);
        }
    }

    public ImageCell? GetCell(int x, int y)
    {
        if (x < ColumnCount && y < RowCount)
        {
            var index = x + y * ColumnCount;
            if (index < _Cells.Count)
                return _Cells[index];
        }
        return null;
    }

    public void MoveCursor(int xDelta, int yDelta)
    {
        if (_EntryCollection.GetCursor() is int index)
        {
            var offset = xDelta + yDelta * ColumnCount;
            _EntryCollection.SetCursor(index + offset);
        }
    }

    private void ListenEntriesUpdated()
    {
        for (int i = 0; i < _Cells.Count; i++)
            _Cells[i].SetEntry(_EntryCollection.Get(i));
    }

    private void ListenCursorMoved(CursorMoved ev)
    {
        for (int i = 0; i < _Cells.Count; i++)
            _Cells[i].SetIsCursor(i == ev.Index);
    }
}

public class ImageCell : Panel
{
    private EventHub _EventHub;
    private ImageCache _ImageCache;
    private bool _IsCursor;
    private bool _IsMarked;

    private Entry? _Entry;
    private string _Format;

    private PictureBox _Picture;
    private Label _Text;

    public ImageCell(EventHub eventHub, ImageCache imageCache)
    {
        _EventHub = eventHub;
        _ImageCache = imageCache;
        _IsCursor = false;
        _IsMarked = false;

        _Entry = null;
        _Format = "";

        _Picture = new PictureBox();
        _Picture.Dock = DockStyle.Fill;
        _Picture.SizeMode = PictureBoxSizeMode.Zoom;

        _Text = new Label();
        _Text.Dock = DockStyle.Bottom;
        _Text.Text = "image plain text";

        Controls.Add(_Picture);
        Controls.Add(_Text);

        MouseClick += ReportMouseClick;
        _Picture.MouseClick += ReportMouseClick;
        _Text.MouseClick += ReportMouseClick;

        BackColor = Color.LightGray;
        Padding = new Padding(5);
    }

    private void ReportMouseClick(Object? _, EventArgs __)
    {
        if (_Entry is Entry entry)
            _EventHub.Send(new ShowEntry(entry));
    }

    public void SetIsCursor(bool isCursor)
    {
        if (isCursor != _IsCursor)
        {
            _IsCursor = isCursor;
            if (_IsCursor)
                BackColor = Color.FromArgb(0x73, 0x73, 0x9c);
            else
                BackColor = Color.LightGray;
        }
    }

    public bool IsCursor()
    {
        return _IsCursor;
    }

    public void SetSize(int w, int h)
    {
        Width = w;
        Height = h;
    }

    public void SetEntry(Entry? entry)
    {
        _Entry = entry;
        var img = _ImageCache.GetImage(_Entry?.Id ?? Guid.Empty);
        _Picture.Image = img;

        _Text.Text = _Entry?.Path;
    }

    public Entry? GetEntry()
    {
        return _Entry;
    }

    public void SetTextFormat(string format)
    {
        _Format = format;
        _Text.Text = $"Got a format: {format}";
    }

    public void SetIsMarked(bool isMarked)
    {
        if (isMarked != _IsMarked)
        {
            if (isMarked)
                BackColor = Color.Red;
            else
                BackColor = Color.LightGray;
        }
        _IsMarked = isMarked;
    }
}
