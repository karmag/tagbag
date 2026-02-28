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
    private EventHub _EventHub;
    private EntryCollection _EntryCollection;
    private ImageCache _ImageCache;

    private PictureBox _Picture;
    private Label _PathLabel;
    private Label _DimensionsLabel;
    private Label _SizeLabel;
    private DataGridView _Tags;

    private bool _Active;

    public TagTable(EventHub eventHub,
                    EntryCollection entryCollection,
                    ImageCache imageCache)
    {
        _EventHub = eventHub;
        _EntryCollection = entryCollection;
        _ImageCache = imageCache;

        _Picture = new PictureBox();
        _PathLabel = new Label();
        _DimensionsLabel = new Label();
        _SizeLabel = new Label();
        _Tags = new DataGridView();

        _Active = false;

        GuiTool.Setup(this);
        GuiTool.Setup(_Picture);
        GuiTool.Setup(_PathLabel);
        GuiTool.Setup(_DimensionsLabel);
        GuiTool.Setup(_SizeLabel);
        GuiTool.Setup(_Tags);

        LayoutComponents();
        SetActive(true);
    }

    public void SetActive(bool active)
    {
        if (active != _Active)
        {
            _Active = active;
            if (active)
            {
                ClientSizeChanged += ListenClientSizeChanged;
                _EventHub.ShowEntry += ListenShowEntry;
                SetEntry(_EntryCollection.GetEntryAtCursor());
            }
            else
            {
                ClientSizeChanged -= ListenClientSizeChanged;
                _EventHub.ShowEntry -= ListenShowEntry;
            }
        }
    }

    private void LayoutComponents()
    {
        var font = new Font("Arial", 14);
        var smallFont = new Font("Arial", 10);
        var smallSizeFont = new Font("Courier New", 12);

        _Tags.ReadOnly = true;
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

        _Tags.SelectionChanged += (_, _) => { _Tags.ClearSelection(); };

        _Tags.DefaultCellStyle.Font = font;

        _Tags.Dock = DockStyle.Fill;
        Controls.Add(_Tags);

        _SizeLabel.Font = smallSizeFont;
        _SizeLabel.Dock = DockStyle.Top;
        Controls.Add(_SizeLabel);

        _DimensionsLabel.Font = font;
        _DimensionsLabel.Dock = DockStyle.Top;
        Controls.Add(_DimensionsLabel);

        _PathLabel.Font = smallFont;
        _PathLabel.Dock = DockStyle.Top;
        Controls.Add(_PathLabel);

        _Picture.SizeMode = PictureBoxSizeMode.Zoom;
        _Picture.Dock = DockStyle.Top;
        Controls.Add(_Picture);
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
            _ImageCache.GetThumbnail(staticId, prio: true)
                .ContinueWith((task) => SetImage(staticId, task));
        }

        if (entry != null)
        {
            var width = "?";
            var height = "?";
            var size = "?";

            if (entry.GetInts(Const.Width) is HashSet<int> widthValues)
            {
                foreach (var value in widthValues)
                {
                    width = value.ToString();
                    break;
                }
            }

            if (entry.GetInts(Const.Height) is HashSet<int> heightValues)
            {
                foreach (var value in heightValues)
                {
                    height = value.ToString();
                    break;
                }
            }

            if (entry.GetInts(Const.Size) is HashSet<int> sizeValues)
            {
                foreach (var value in sizeValues)
                {
                    size = ReadableSize(value);
                    break;
                }
            }

            _PathLabel.Text = entry.Path;
            _DimensionsLabel.Text = $"{width} x {height}";
            _SizeLabel.Text = size;
        }
        else
        {
            _PathLabel.Text = "";
            _DimensionsLabel.Text = "";
            _SizeLabel.Text = "";
        }

        _Tags.Rows.Clear();

        if (entry != null)
        {
            var data = new List<string[]>();

            foreach (var tag in entry.GetAllTags())
            {
                if (entry.Get(tag) is Value value)
                {
                    var valueColl = new List<string>();

                    if (value.IsTag())
                        valueColl.Add("true");

                    foreach (var i in value.GetInts() ?? [])
                        valueColl.Add(i.ToString());

                    foreach (var s in value.GetStrings() ?? [])
                        valueColl.Add(s);

                    if (valueColl.Count == 1 && Const.BuiltinTags.Contains(tag))
                        continue;

                    if (valueColl.Count == 1 && value.IsTag())
                        data.Add(new string[] { tag, "" });
                    else
                        data.Add(new string[] { tag, String.Join(", ", valueColl) });
                }
            }

            data.Sort((a, b) => { return String.Compare(a[0], b[0]); });
            foreach (var row in data)
                _Tags.Rows.Add(row);
        }
    }

    private string ReadableSize(int inputSize)
    {
        var size = (decimal)inputSize;
        var postfix = "";

        foreach (var item in new string[]{"k", "M", "G", "T"})
        {
            if (size / 1024 > 1)
            {
                size /= 1024;
                postfix = item;
            }
            else
                break;
        }

        postfix = " " + postfix + "b";

        var digits = ((int)size).ToString().Length;
        if (digits > 2)
            return ((int)size).ToString() + postfix;
        else
            return size.ToString("#.#") + postfix;
    }

    public void RefreshEntry()
    {
        SetEntry(_Entry);
    }

    private void ListenClientSizeChanged(object? _, EventArgs __)
    {
        _Picture.Width = Width;
        _Picture.Height = (int)(Width * 0.7);
    }

    private void ListenShowEntry(ShowEntry ev)
    {
        SetEntry(ev.Entry);
    }
}
