using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Tagbag.Gui;

public class Root : Form
{
    private TabControl tabControl = new TabControl();
    
    public Root(Tagbag.Core.Tagbag tb)
    {
        tabControl.Dock = DockStyle.Fill;
        Controls.Add(tabControl);

        var data = new Data(tb);

        SketchGogo(data);
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
        var img = new Bitmap("test-data\\a.jpg");
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
        var img = new Bitmap("test-data\\a.jpg");

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
        var img = new Bitmap("test-data\\a.jpg");

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

    private void SketchGogo(Data data)
    {
        var ig = new Components.ImageGrid(data);

        Add("grid", ig);
    }
}
