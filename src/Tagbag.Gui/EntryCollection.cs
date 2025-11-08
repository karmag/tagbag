using System;
using System.Collections.Generic;
using Tagbag.Core;

namespace Tagbag.Gui;

public class EntryCollection
{
    private EventHub _EventHub;

    private ICollection<Entry> _BaseEntries;
    private List<Entry> _Entries;
    private int? _CursorIndex;
    private Stack<IFilter> _Filters;

    public EntryCollection(EventHub eventHub)
    {
        _EventHub = eventHub;

        _BaseEntries = [];
        _Entries = new List<Entry>();
        _CursorIndex = null;
        _Filters = new Stack<IFilter>();
    }

    public void SetBaseEntries(ICollection<Entry>? entries)
    {
        _BaseEntries = entries ?? [];
        _Entries = new List<Entry>(_BaseEntries.Count);
        RefreshEntries();
    }

    public int Size()
    {
        return _Entries.Count;
    }

    public Entry? Get(int index)
    {
        if (index >= 0 && index < _Entries.Count)
            return _Entries[index];
        return null;
    }

    // Returns the cursor index. Returns null if there are currently
    // no entries visible.
    public int? GetCursor()
    {
        return _CursorIndex;
    }

    public Entry? GetEntryAtCursor()
    {
        if (_CursorIndex is int index)
            return Get(index);
        return null;
    }

    public void SetCursor(int index)
    {
        if (_Entries.Count == 0)
            return;

        if (index < 0)
            index = 0;

        if (index >= _Entries.Count)
            index = _Entries.Count - 1;

        if (index == _CursorIndex)
            return;

        _CursorIndex = index;
        _EventHub.Send(new CursorMoved(index));
    }

    public void PushFilter(IFilter filter)
    {
        _Filters.Push(filter);
        RefreshEntries();
    }

    public bool PopFilter()
    {
        if (_Filters.Count > 0)
        {
            _Filters.Pop();
            RefreshEntries();
            return true;
        }
        return false;
    }

    public void ClearFilters()
    {
        if (_Filters.Count > 0)
        {
            _Filters.Clear();
            RefreshEntries();
        }
    }

    private void RefreshEntries()
    {
        var cursorId = GetEntryAtCursor()?.Id;
        var filter = Filter.And(_Filters);
        _Entries.Clear();
        _CursorIndex = null;

        int index = 0;
        bool pickEntry = false;

        foreach (var entry in _BaseEntries)
        {
            if (entry.Id == cursorId)
                pickEntry = true;

            if (filter.Keep(entry))
            {
                _Entries.Add(entry);
                if (pickEntry)
                {
                    _CursorIndex = index;
                    pickEntry = false;
                }
            }

            index++;
        }

        if (_CursorIndex == null && _Entries.Count > 0)
            _CursorIndex = 0;

        _EventHub.Send(new EntriesUpdated());
    }
}
