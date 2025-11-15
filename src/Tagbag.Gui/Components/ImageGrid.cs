using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tagbag.Core;

namespace Tagbag.Gui.Components;

public class ImageGrid : TableLayoutPanel
{
    private EventHub _EventHub;
    private EntryCollection _EntryCollection;
    private ImageCache _ImageCache;

    private int _Rows;
    private int _Columns;
    private double _CellRatio; // Height / width ratio for cells.
    private List<ImageCell> _Cells;
    private int _IndexOffset;
    private ImageCell? _CursorCell;

    public ImageGrid(EventHub eventHub,
                     EntryCollection entryCollection,
                     ImageCache imageCache)
    {
        _EventHub = eventHub;
        _EntryCollection = entryCollection;
        _ImageCache = imageCache;

        _Rows = 3;
        _Columns = 3;
        _CellRatio = 1.3;
        _Cells = new List<ImageCell>();
        _IndexOffset = 0;
        _CursorCell = null;

        RowCount = _Rows;
        ColumnCount = _Columns;
        for (int i = 0; i < _Rows * _Columns; i++)
            _Cells.Add(new ImageCell(_EventHub, _ImageCache));

        Padding = new Padding(5);

        ClientSizeChanged += (_, _) => { LayoutChanged(_Rows); };
        SetActive(true);
    }

    public void SetActive(bool active)
    {
        if (active)
        {
            _EventHub.EntriesUpdated += ListenEntriesUpdated;
            _EventHub.CursorMoved += ListenCursorMoved;
            _EventHub.MarkedChanged += ListenMarkedChanged;
        }
        else
        {
            _EventHub.EntriesUpdated -= ListenEntriesUpdated;
            _EventHub.CursorMoved -= ListenCursorMoved;
            _EventHub.MarkedChanged -= ListenMarkedChanged;
        }
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
            if (_EntryCollection.GetCursor() is int index)
                _IndexOffset = (index / newColumns) * newColumns;
            else
                _IndexOffset = 0;

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
            UpdateCellEntries();
            UpdateCursor();
        }

        foreach (var cell in _Cells)
            cell.SetSize(maxWidth, maxHeight);
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

    public void MovePage(int pages)
    {
        var offset = pages * ColumnCount * RowCount;
        if (_IndexOffset + offset >= _EntryCollection.Size())
            return;

        _IndexOffset += offset;
        if (_IndexOffset < 0)
            _IndexOffset = 0;
        _IndexOffset = Math.Min(
            _IndexOffset,
            (_EntryCollection.Size() / ColumnCount - 1) * ColumnCount);

        UpdateCellEntries();
        if (_EntryCollection.GetCursor() is int index)
            _EntryCollection.SetCursor(index + offset);
    }

    private void UpdateCellEntries()
    {
        for (int i = 0; i < _Cells.Count; i++)
            _Cells[i].SetEntry(_EntryCollection.Get(i + _IndexOffset));
        UpdateMarked();
    }

    private void UpdateMarked()
    {
        foreach (var cell in _Cells)
            cell.SetIsMarked(
                _EntryCollection.IsMarked(
                    cell.GetEntry()?.Id ?? Guid.Empty));
    }

    private void UpdateCursor()
    {
        if (_CursorCell is ImageCell cell)
            cell.SetIsCursor(false);

        _CursorCell = null;

        if (_EntryCollection.GetCursor() is int index)
        {
            var cellIndex = index - _IndexOffset;
            if (0 <= cellIndex && cellIndex < _Cells.Count)
            {
                _CursorCell = _Cells[cellIndex];
                _CursorCell.SetIsCursor(true);
            }
        }
    }

    private void ListenEntriesUpdated(EntriesUpdated _)
    {
        UpdateCellEntries();
        UpdateCursor();
    }

    private void ListenCursorMoved(CursorMoved ev)
    {
        if (ev.Index is int index)
        {
            var rowDiff = index / ColumnCount - _IndexOffset / ColumnCount;
            if (rowDiff < 0 || rowDiff >= RowCount)
            {
                if (rowDiff == -1)
                    _IndexOffset -= ColumnCount;
                else if (rowDiff == RowCount)
                    _IndexOffset += ColumnCount;
                else
                    _IndexOffset += rowDiff * ColumnCount;
                UpdateCellEntries();
            }
        }

        UpdateCursor();
    }

    private void ListenMarkedChanged(MarkedChanged _)
    {
        UpdateMarked();
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
                BorderStyle = BorderStyle.FixedSingle;
            else
                BorderStyle = BorderStyle.None;
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
        if (_Entry != null)
        {
            _Picture.Image = null;
            var staticId = _Entry.Id;
            _ImageCache
                .GetThumbnail(_Entry.Id)
                .ContinueWith((task) => SetImage(staticId, task));
        }
        else
        {
            _Picture.Image = null;
        }

        _Text.Text = _Entry?.Path;
    }

    private async void SetImage(Guid id, Task<Bitmap?> task)
    {
        if (id == _Entry?.Id && await task is Bitmap image)
            _Picture.Image = image;
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
