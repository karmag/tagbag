using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Tagbag.Gui.Components;

public class ImageGrid : TableLayoutPanel
{
    private int _Rows;
    private int _Columns;
    private double _CellRatio; // Height / width ratio for cells.
    private List<ImageCell> _Cells;
    private Data _Data;

    public ImageGrid(Data data)
    {
        _Rows = 3;
        _Columns = 3;
        _CellRatio = 1.3;
        _Cells = new List<ImageCell>();
        _Data = data;

        RowCount = _Rows;
        ColumnCount = _Columns;
        for (int i = 0; i < _Rows * _Columns; i++)
            _Cells.Add(new ImageCell(_Data));

        ClientSizeChanged += (Object? _, EventArgs _) => { UpdateGrid(_Rows); };
    }

    private void UpdateGrid(int newRows)
    {
        newRows = Math.Max(1, newRows);

        int maxHeight = Height / _Rows;
        int newColumns = Math.Max(1, (int)(Width / (maxHeight * _CellRatio)));
        int maxWidth = Width / newColumns;

        if (newRows != RowCount || newColumns != ColumnCount)
        {
            var newCellCount = newRows * newColumns;
            var oldCellCount = RowCount * ColumnCount;

            var newCells = new List<ImageCell>(_Cells.Take(newCellCount));
            while (newCells.Count < newCellCount)
                newCells.Add(new ImageCell(_Data));
            _Cells = newCells;

            for (int i = 0; i < newCellCount; i++)
            {
                var entry = _Data.EntryCollection.Get(i);
                _Cells[i].SetKey(entry?.Id);
            }

            RowCount = newRows;
            ColumnCount = newColumns;

            Controls.Clear();
            foreach (var cell in _Cells)
                Controls.Add(cell);
        }

        foreach (var cell in _Cells)
            cell.SetSize(maxWidth, maxHeight);

        // Populate with controls?
    }
}

public class ImageCell : Control
{
    private Data _Data;

    private Guid? _Key;
    private string _Format;

    private PictureBox _Picture;
    private Label _Text;

    public ImageCell(Data data)
    {
        _Data = data;

        _Key = null;
        _Format = "";

        _Picture = new PictureBox();
        _Picture.Dock = DockStyle.Fill;
        _Picture.SizeMode = PictureBoxSizeMode.Zoom;

        _Text = new Label();
        _Text.Dock = DockStyle.Bottom;
        _Text.Text = "image plain text";

        Controls.Add(_Picture);
        Controls.Add(_Text);
    }

    public void SetSize(int w, int h)
    {
        Width = w;
        Height = h;
    }

    public void SetKey(Guid? key)
    {
        _Key = key;
        var img = _Data.ImageCache.GetImage(key ?? Guid.Empty);
        _Picture.Image = img;

        _Text.Text = _Data.Tagbag.Get(key ?? Guid.Empty)?.Path;
    }

    public void SetTextFormat(string format)
    {
        _Format = format;
        _Text.Text = $"Got a format: {format}";
    }
}
