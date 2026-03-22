using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tagbag.Core;
using Tagbag.Core.Input;

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

    private HashSet<string> _HideTags;

    private bool _Active;

    public TagTable(EventHub eventHub,
                    Config config,
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

        _HideTags = new HashSet<string>();
        config.Ui.HideTags.Changed += (_, tagString) => SetHideTags(tagString);
        SetHideTags(config.Ui.HideTags.Get());

        _Tags.CellMouseDoubleClick += (Object? _, DataGridViewCellMouseEventArgs ev) =>
            {
                string? key = _Tags.Rows[ev.RowIndex].Cells[0].Value?.ToString();
                string? val = _Tags.Rows[ev.RowIndex].Cells[ev.ColumnIndex].Value?.ToString();

                for (int row = ev.RowIndex; row >= 0 && (key == null || key == ""); row--)
                    key = _Tags.Rows[row].Cells[0].Value?.ToString();

                IFilter? filter = null;

                if (ev.ColumnIndex == 0)
                {
                    if (key != null)
                        filter = Filter.Has(key);
                }
                else
                {
                    if (key != null && val != null)
                    {
                        int i;
                        if (int.TryParse(val, out i))
                            filter = Filter.Has(key, i);
                        else
                            filter = Filter.Has(key, val);
                    }
                }

                if (filter != null)
                    eventHub.Send(new ShowFilter(filter));
            };

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

    private void SetHideTags(string tagString)
    {
        try
        {
            var tags = new HashSet<string>();
            foreach (var token in Tokenizer.GetTokens(tagString))
                tags.Add(token.Text);
            _HideTags = tags;
        }
        catch (TokenizerException e)
        {
            System.Console.WriteLine($"Failed to parse hide-tags: {e.Message}");
        }
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
            var width = entry.GetIntBy(Const.Width, int.Max);
            var height = entry.GetIntBy(Const.Height, int.Max);
            var size = entry.GetIntBy(Const.Size, int.Max);

            _PathLabel.Text = entry.Path;
            _DimensionsLabel.Text = $"{width} x {height}";
            _SizeLabel.Text = ReadableSize(size);
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
                if (_HideTags.Contains(tag))
                    continue;

                if (entry.Get(tag) is Value value)
                {
                    var ints = value.GetInts();
                    var strs = value.GetStrings();

                    foreach (var i in ints ?? [])
                        data.Add(new string[] { tag, i.ToString() });

                    foreach (var s in value.GetStrings() ?? [])
                        data.Add(new string[] { tag, s });

                    if (ints == null && strs == null)
                        data.Add(new string[] { tag, "" });
                }
            }

            data.Sort((a, b) => {
                var diff = String.Compare(a[0], b[0]);
                if (diff == 0)
                    diff = String.Compare(a[1], b[1]);
                return diff;
            });

            var lastKey = "";
            foreach (var row in data)
            {
                if (lastKey != row[0])
                {
                    _Tags.Rows.Add(row);
                    lastKey = row[0];
                }
                else
                    _Tags.Rows.Add(new String[] {"", row[1]});
            }
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
