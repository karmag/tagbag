using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Tagbag.Core;

namespace Tagbag.Gui.Components;

public class TagTable : DataGridView
{
    private Entry? _Entry;

    public TagTable()
    {
        ColumnCount = 2;
        AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        ColumnHeadersVisible = false;
        RowHeadersVisible = false;

        AllowUserToAddRows = false;
        AllowUserToDeleteRows = false;
        AllowUserToOrderColumns = false;
        AllowUserToResizeRows = false;
        EditMode = DataGridViewEditMode.EditProgrammatically;
        SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        MultiSelect = false;

        TabStop = false;
    }

    public void SetEntry(Entry? entry)
    {
        _Entry = entry;

        Rows.Clear();

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
                Rows.Add(row);
        }
    }

    public void RefreshEntry()
    {
        SetEntry(_Entry);
    }
}
