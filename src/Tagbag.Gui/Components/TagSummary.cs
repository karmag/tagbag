using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Tagbag.Core;
using Tagbag.Core.Input;

namespace Tagbag.Gui.Components;

public class TagSummary : Panel
{
    public Action<IFilter>? Selection;

    private EventHub _EventHub;
    private EntryCollection _EntryCollection;

    private DataGridView _Tags;
    private HashSet<string> _HideTags;

    private bool _Active;

    public TagSummary(EventHub eventHub,
                      Config config,
                      EntryCollection entryCollection)
    {
        _EventHub = eventHub;
        _EntryCollection = entryCollection;

        _Tags = new DataGridView();

        _HideTags = new HashSet<string>();
        config.Ui.HideSummaryTags.Changed += (_, tagString) => SetHideTags(tagString);
        SetHideTags(config.Ui.HideSummaryTags.Get());

        GuiTool.Setup(this);
        GuiTool.Setup(_Tags);

        LayoutComponents();

        _Tags.CellMouseDoubleClick += (Object? _, DataGridViewCellMouseEventArgs ev) =>
            {
                string? key = _Tags.Rows[ev.RowIndex].Cells[0].Value?.ToString();
                string? val = _Tags.Rows[ev.RowIndex].Cells[ev.ColumnIndex].Value?.ToString();

                for (int row = ev.RowIndex; row >= 0 && (key == null || key == ""); row--)
                    key = _Tags.Rows[row].Cells[0].Value?.ToString();

                IFilter? filter = null;

                switch (ev.ColumnIndex)
                {
                    case 0:
                    case 2:
                        if (key != null)
                            filter = Filter.Has(key);
                        break;

                    case 1:
                        if (key != null && val != null)
                        {
                            int i;
                            if (int.TryParse(val, out i))
                                filter = Filter.Has(key, i);
                            else
                                filter = Filter.Has(key, val);
                        }
                        break;
                }

                if (filter != null)
                    eventHub.Send(new ShowFilter(filter));
            };

        _Active = false;
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
            System.Console.WriteLine($"Failed to parse summary hide-tags: {e.Message}");
        }
    }

    public void SetActive(bool active)
    {
        if (active != _Active)
        {
            _Active = active;
            if (active)
            {
                _EventHub.EntriesUpdated += ListenEntriesUpdated;
                RefreshEntries();
            }
            else
            {
                _EventHub.EntriesUpdated -= ListenEntriesUpdated;
            }
        }
    }

    private void LayoutComponents()
    {
        _Tags.ReadOnly = true;
        _Tags.ColumnCount = 3;
        _Tags.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
        _Tags.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _Tags.ColumnHeadersVisible = false;
        _Tags.RowHeadersVisible = false;

        _Tags.AllowUserToAddRows = false;
        _Tags.AllowUserToDeleteRows = false;
        _Tags.AllowUserToOrderColumns = false;
        _Tags.AllowUserToResizeRows = false;
        _Tags.EditMode = DataGridViewEditMode.EditProgrammatically;
        _Tags.SelectionMode = DataGridViewSelectionMode.CellSelect;
        _Tags.MultiSelect = false;

        _Tags.SelectionChanged += (_, _) => { _Tags.ClearSelection(); };

        _Tags.DefaultCellStyle.Font = new Font("Arial", 14);

        _Tags.Dock = DockStyle.Fill;
        Controls.Add(_Tags);
    }

    private void RefreshEntries()
    {
        var tags = new Dictionary<(string, string?), int>();

        // Collect tag data

        for (int index = 0; index < _EntryCollection.Size(); index++)
        {
            if (_EntryCollection.Get(index) is Entry entry)
            {
                foreach (var tag in entry.GetAllTags())
                {
                    if (_HideTags.Contains(tag))
                        continue;

                    var ints = entry.GetInts(tag);
                    var strings = entry.GetStrings(tag);

                    if (ints == null && strings == null)
                    {
                        var key = (tag, (string?)null);
                        tags[key] = tags.GetValueOrDefault(key) + 1;
                    }
                    else
                    {
                        foreach (var i in ints ?? [])
                        {
                            var key = (tag, i.ToString());
                            tags[key] = tags.GetValueOrDefault(key) + 1;
                        }

                        foreach (var s in strings ?? [])
                        {
                            var key = (tag, s);
                            tags[key] = tags.GetValueOrDefault(key) + 1;
                        }
                    }
                }
            }
        }

        // Display tags

        var tagOrder = new List<(string, string?)>(tags.Keys);
        tagOrder.Sort((a, b) =>
        {
            var diff = String.Compare(a.Item1, b.Item1);
            if (diff == 0)
                return String.Compare(a.Item2, b.Item2);
            return diff;
        });

        _Tags.SuspendLayout();
        _Tags.Rows.Clear();
        var previousTag = "";
        foreach (var key in tagOrder)
        {
            var tag = key.Item1;
            if (previousTag == tag)
                tag = "";
            else
                previousTag = tag;

            _Tags.Rows.Add(new string[] {tag,
                                         key.Item2 ?? "",
                                         tags.GetValueOrDefault(key).ToString()});
        }
        _Tags.ResumeLayout();
    }

    public void ListenEntriesUpdated(EntriesUpdated _)
    {
        RefreshEntries();
    }
}
