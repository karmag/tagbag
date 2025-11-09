using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tagbag.Core;

namespace Tagbag.Gui.Components;

public class TagTable : Panel
{
    private Entry? _Entry;
    private ImageCache _ImageCache;

    private PictureBox _Picture;
    private Label _Label;
    private DataGridView _Tags;

    public TagTable(EventHub eventHub,
                    ImageCache imageCache)
    {
        _ImageCache = imageCache;

        _Picture = new PictureBox();
        _Label = new Label();
        _Tags = new DataGridView();
        ConfigureComponents();

        _Tags.Dock = DockStyle.Fill;
        Controls.Add(_Tags);

        _Label.Dock = DockStyle.Top;
        Controls.Add(_Label);

        _Picture.Dock = DockStyle.Top;
        Controls.Add(_Picture);

        ClientSizeChanged += (_, _) => {
            _Picture.Width = Width;
            _Picture.Height = (int)(Width * 0.7);
        };
        eventHub.ShowEntry += (ev) => { ListenShowEntry(ev); };
    }

    public void ConfigureComponents()
    {
        _Picture.SizeMode = PictureBoxSizeMode.Zoom;

        _Tags.Enabled = false;
        _Tags.ColumnCount = 2;
        _Tags.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
        _Tags.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _Tags.ColumnHeadersVisible = false;
        _Tags.RowHeadersVisible = false;

        _Tags.AllowUserToAddRows = false;
        _Tags.AllowUserToDeleteRows = false;
        _Tags.AllowUserToOrderColumns = false;
        _Tags.AllowUserToResizeRows = false;
        _Tags.EditMode = DataGridViewEditMode.EditProgrammatically;
        _Tags.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _Tags.MultiSelect = false;

        _Tags.TabStop = false;
    }

    private async void SetImage(Guid id, Task<Bitmap?> task)
    {
        if (_Entry?.Id == id && await task is Bitmap image)
            _Picture.Image = image;
    }

    public void SetEntry(Entry? entry)
    {
        _Entry = entry;

        _Picture.Image = null;

        if (_Entry != null)
        {
            var staticId = _Entry.Id;
            _ImageCache.GetThumbnail(staticId)
                .ContinueWith((task) => SetImage(staticId, task));
        }

        if (entry != null)
        {
            var width = "?";
            var height = "?";

            if (entry.GetInts("width") is HashSet<int> widthValues)
            {
                foreach (var value in widthValues)
                {
                    width = value.ToString();
                    break;
                }
            }

            if (entry.GetInts("height") is HashSet<int> heightValues)
            {
                foreach (var value in heightValues)
                {
                    height = value.ToString();
                    break;
                }
            }

            _Label.Text = $"{width} x {height}";
        }
        else
        {
            _Label.Text = "";
        }

        _Tags.Rows.Clear();

        if (entry != null)
        {
            var data = new List<string[]>();

            foreach (var tag in entry.GetAllTags())
            {
                var value = entry.Get(tag);
                if (value != null)
                {
                    var valueColl = new List<string>();

                    if (value.IsTag())
                        valueColl.Add("true");

                    foreach (var i in value.GetInts() ?? [])
                        valueColl.Add(i.ToString());

                    foreach (var s in value.GetStrings() ?? [])
                        valueColl.Add(s);

                    data.Add(
                        new string[] { tag, String.Join(", ", valueColl) });
                }
            }

            data.Sort((a, b) => { return String.Compare(a[0], b[0]); });
            foreach (var row in data)
                _Tags.Rows.Add(row);
        }
    }

    public void RefreshEntry()
    {
        SetEntry(_Entry);
    }

    private void ListenShowEntry(ShowEntry ev)
    {
        SetEntry(ev.Entry);
    }
}
