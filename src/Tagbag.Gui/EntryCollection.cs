using System;
using System.IO;
using System.Collections.Generic;
using Tagbag.Core;

namespace Tagbag.Gui;

public class EntryCollection
{
    private EventHub _EventHub;

    private List<Entry> _BaseEntries;
    private List<Entry> _Entries;
    private int? _CursorIndex;
    private Stack<IFilter> _Filters;
    private HashSet<Guid> _Marked;
    private Comparison<Entry> _SortOrder;

    public EntryCollection(EventHub eventHub)
    {
        _EventHub = eventHub;

        _BaseEntries = [];
        _Entries = new List<Entry>();
        _CursorIndex = null;
        _Filters = new Stack<IFilter>();
        _Marked = new HashSet<Guid>();

        _SortOrder = (a, b) => {
            var dirDiff = String.Compare(Path.GetDirectoryName(a.Path),
                                         Path.GetDirectoryName(b.Path));
            if (dirDiff == 0)
                return String.Compare(a.Path, b.Path, ignoreCase: true);
            return dirDiff;
        };
    }

    public void SetBaseEntries(ICollection<Entry>? entries)
    {
        _BaseEntries = new List<Entry>(entries ?? []);
        _BaseEntries.Sort(_SortOrder);
        _Entries = new List<Entry>(_BaseEntries.Count);
        RefreshEntries();
    }

    // The number of currently visible entries.
    public int Size()
    {
        return _Entries.Count;
    }

    // The number of entries in the tagbag.
    public int MaxSize()
    {
        return _BaseEntries.Count;
    }

    public Entry? Get(int index)
    {
        if (0 <= index && index < _Entries.Count)
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
            return _Entries[index];
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
        _EventHub.Send(new ShowEntry(GetEntryAtCursor()));
    }

    public void MoveCursor(int offset)
    {
        if (GetCursor() is int index)
            SetCursor(index + offset);
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

    public Stack<IFilter> GetFilters()
    {
        return _Filters;
    }

    public void SetMarked(Guid id, bool marked)
    {
        if (marked != IsMarked(id))
        {
            if (marked)
                _Marked.Add(id);
            else
                _Marked.Remove(id);

            _EventHub.Send(new MarkedChanged());
        }
    }

    public HashSet<Guid> GetMarked()
    {
        return _Marked;
    }

    public bool IsMarked(Guid id)
    {
        return _Marked.Contains(id);
    }

    public void ClearMarked()
    {
        _Marked.Clear();
        _EventHub.Send(new MarkedChanged());
    }

    public void MarkVisible()
    {
        foreach (var entry in _Entries)
            _Marked.Add(entry.Id);
        _EventHub.Send(new MarkedChanged());
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

                index++;
            }
        }

        if (_CursorIndex == null && _Entries.Count > 0)
            _CursorIndex = 0;

        _EventHub.Send(new EntriesUpdated());
        _EventHub.Send(new CursorMoved(GetCursor()));
        _EventHub.Send(new ShowEntry(GetEntryAtCursor()));
    }
}
