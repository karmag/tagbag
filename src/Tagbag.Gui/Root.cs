using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Tagbag.Core;

namespace Tagbag.Gui;

public class Root : Form
{
    private TabControl tabControl = new TabControl();

    public Root()
    {
        tabControl.Dock = DockStyle.Fill;
        Controls.Add(tabControl);

        SketchResize();
        SketchImage();
        SketchBasicView();

        Width = 500;
        Height = 500;
    }

    private void Add(string name, Control control)
    {
        var page = new TabPage(name);
        control.Dock = DockStyle.Fill;
        page.Controls.Add(control);
        tabControl.Controls.Add(page);
    }

    private void SketchBasicView()
    {
        var details = new DataGridView();
        details.Dock = DockStyle.Left;
        details.BackColor = Color.LightGreen;
        details.Width = 300;
        details.ColumnCount = 2;
        details.Rows.Add(new string[]{"Tag", "Value, Value, Value, Value, Value, Value"});
        details.Rows.Add(new string[]{"A", "B"});
        details.Rows.Add(new string[]{"A", "B"});

        var images = new TableLayoutPanel();
        images.Dock = DockStyle.Fill;
        images.BackColor = Color.LightBlue;
        images.RowCount = 3;
        images.ColumnCount = 3;
        //images.AutoScroll = true;
        var img = new Bitmap("a.jpg");
        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < 3; x++)
            {
                var item = new PictureBox();
                item.Image = img;
                item.SizeMode = PictureBoxSizeMode.Zoom;
                //item.Dock = DockStyle.Fill;
                images.Controls.Add(item);
            }
        }

        var input = new TextBox();
        input.Dock = DockStyle.Bottom;

        var panel = new Control();
        panel.BackColor = Color.Pink;
        panel.Controls.Add(images);
        panel.Controls.Add(details);
        panel.Controls.Add(input);

        Add("Basic", panel);
    }

    private void SketchImage()
    {
        var img = new Bitmap("a.jpg");

        // var images = new TableLayoutPanel();
        // images.Dock = DockStyle.Fill;
        // images.BackColor = Color.LightBlue;
        // images.RowCount = 3;
        // images.ColumnCount = 3;
        // //images.AutoScroll = true;
        // for (var y = 0; y < 3; y++)
        // {
        //     for (var x = 0; x < 3; x++)
        //     {
        //         var item = new PictureBox();
        //         item.Image = img;
        //         item.SizeMode = PictureBoxSizeMode.Zoom;
        //         item.Dock = DockStyle.Fill;
        //         images.Controls.Add(item);
        //     }
        // }

        var images = new Control();

        for (var i = 0; i < 3; i++)
        {
            var im = new PictureBox();
            im.Image = img;
            im.Dock = DockStyle.Left;
            im.SizeMode = PictureBoxSizeMode.Zoom;
            images.Controls.Add(im);
        }

        var split = new Splitter();
        images.Controls.Add(split);

        for (var i = 0; i < 3; i++)
        {
            var im = new PictureBox();
            im.Image = img;
            im.Dock = DockStyle.Left;
            im.SizeMode = PictureBoxSizeMode.Zoom;
            images.Controls.Add(im);
        }

        Add("image", images);
    }

    private void SketchResize()
    {
        var img = new Bitmap("a.jpg");

        var panel = new TableLayoutPanel();
        panel.RowCount = 3;
        panel.ColumnCount = 4;

        var images = new List<PictureBox>();
        for (var i = 0; i < panel.RowCount * panel.ColumnCount; i++)
            images.Add(new PictureBox());

        void ClientSizeChanged(Object? sender, EventArgs e)
        {
            var w = panel.Width / panel.ColumnCount;
            var h = panel.Height / panel.RowCount;
            foreach (var im in images)
            {
                im.Width = w;
                im.Height = h;
            }
        }
        panel.ClientSizeChanged += ClientSizeChanged;

        foreach (var im in images)
        {
            im.Image = img;
            //im.Dock = DockStyle.Fill;
            im.SizeMode = PictureBoxSizeMode.Zoom;
            panel.Controls.Add(im);
        }

        Add("resize", panel);
    }
}

public class ImageGallery : TableLayoutPanel
{
    private int _Rows;
    private int _Columns;
    private double _CellRatio; // Height / width ratio for cells.
    private List<ImageCell> _Cells;
    private EntryCollection _Entries;

    public ImageGallery(EntryCollection entryColl)
    {
        _Rows = 3;
        _Columns = 3;
        _CellRatio = 1.3;
        _Cells = new List<ImageCell>();
        _Entries = entryColl;

        RowCount = _Rows;
        ColumnCount = _Columns;
        for (int i = 0; i < _Rows * _Columns; i++)
            _Cells.Add(new ImageCell());
        UpdateGrid(_Rows);

        ClientSizeChanged += (Object? _, EventArgs _) => { UpdateGrid(_Rows); };
    }

    private void UpdateGrid(int newRows)
    {
        int maxHeight = Height / _Rows;
        int newColumns = Math.Max(1, (int)(Width / (maxHeight * _CellRatio)));
        int maxWidth = Width / newColumns;

        if (newRows != RowCount || newColumns != ColumnCount)
        {
            var newCells = new List<ImageCell>(_Cells.Take(newRows * newColumns));
            while (newCells.Count < newRows * newColumns)
                newCells.Add(new ImageCell());
            _Cells = newCells;
        }

        foreach (var cell in _Cells)
            cell.SetSize(maxWidth, maxHeight);

        // Populate with controls?

        RowCount = newRows;
        ColumnCount = newColumns;
    }

    private class ImageCell : Control
    {
        public void SetSize(int w, int h)
        {
            Width = w;
            Height = h;
        }
    }
}
