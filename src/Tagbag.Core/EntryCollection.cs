using System.Collections.Generic;

namespace Tagbag.Core;

public class EntryCollection
{
    private Tagbag _Tagbag;
    private Stack<IFilter> _Filters;

    private List<Entry> _Entries;
    private int _EntryCount;

    public EntryCollection(Tagbag tb)
    {
        _Tagbag = tb;
        _Filters = new Stack<IFilter>();

        _Entries = new List<Entry>();
        _EntryCount = 0;

        Refresh();
    }

    // Refresh entries by fetching all from the Tagbag instance and
    // applying all filters.
    private void Refresh()
    {
        var entries = _Tagbag.GetEntries();
        _EntryCount = entries.Count;

        if (_Entries.Capacity >= entries.Count)
        {
            int i = 0;
            foreach (var entry in entries)
            {
                _Entries[i] = entry;
                i++;
            }
        }
        else
        {
            _Entries = new List<Entry>(entries);
        }

        if (_Filters.Count > 0)
            ApplyFilter(Filter.And(_Filters));
    }

    // Only filter the current entries with the given filter. No other
    // effect.
    private void ApplyFilter(IFilter filter)
    {
        var searchIndex = 0;
        var sizeIndex = 0;

        for (; searchIndex < _EntryCount; searchIndex++)
        {
            if (filter.Keep(_Entries[searchIndex]))
            {
                _Entries[sizeIndex] = _Entries[searchIndex];
                sizeIndex++;
            }
        }

        _EntryCount = sizeIndex;
    }

    public int Size()
    {
        return _EntryCount;
    }

    public Entry? Get(int index)
    {
        if (index < _EntryCount)
            return _Entries[index];
        return null;
    }

    public void PushFilter(IFilter filter)
    {
        _Filters.Push(filter);
        ApplyFilter(filter);
    }

    public bool PopFilter()
    {
        if (_Filters.Count > 0)
        {
            _Filters.Pop();
            Refresh();
            return true;
        }
        return false;
    }

    public void ClearFilters()
    {
        _Filters.Clear();
        Refresh();
    }
}
