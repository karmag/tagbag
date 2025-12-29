using System;
using System.Collections.Generic;
using System.Threading;
using Tagbag.Core;

namespace Tagbag.Gui;

public class EventHub
{
    private Stack<Event> _EventQueue;
    private Semaphore _Lock;

    public Action<Shutdown>? Shutdown;
    public Action<Log>? Log;
    public Action<TagbagFileSet>? TagbagFileSet;

    public Action<EntriesUpdated>? EntriesUpdated;
    public Action<ShowEntry>? ShowEntry;
    public Action<CursorMoved>? CursorMoved;
    public Action<MarkedChanged>? MarkedChanged;

    public Action<FilterCommand>? FilterCommand;
    public Action<TagCommand>? TagCommand;

    public Action<ViewChanged>? ViewChanged;

    public EventHub()
    {
        _EventQueue = new Stack<Event>();
        _Lock = new Semaphore(initialCount: 1, maximumCount: 1);
    }

    public void Send(Event? newEvent)
    {
        if (newEvent != null)
            _EventQueue.Push(newEvent);

        if (_Lock.WaitOne(0))
        {
            try
            {
                while (_EventQueue.Count > 0)
                    ProcessEvent(_EventQueue.Pop());
            }
            finally
            {
                _Lock.Release();
            }

            if (_EventQueue.Count > 0)
                Send(null);
        }
        else if (newEvent != null)
        {
            Send(null);
        }
    }

    private void ProcessEvent(Event ev)
    {
        System.Console.WriteLine($"Event: {ev}");

        switch (ev)
        {
            case Shutdown e: Shutdown?.Invoke(e); break;
            case Log e: Log?.Invoke(e); break;
            case TagbagFileSet e: TagbagFileSet?.Invoke(e); break;

            case EntriesUpdated e: EntriesUpdated?.Invoke(e); break;
            case ShowEntry e: ShowEntry?.Invoke(e); break;
            case CursorMoved e: CursorMoved?.Invoke(e); break;
            case MarkedChanged e: MarkedChanged?.Invoke(e); break;

            case FilterCommand e: FilterCommand?.Invoke(e); break;
            case TagCommand e: TagCommand?.Invoke(e); break;

            case ViewChanged e: ViewChanged?.Invoke(e); break;

            default:
                throw new ArgumentException($"Unknown event type {ev}");
        }
    }
}

public enum LogType
{
    Info,
    Error,
}

public abstract record class Event();

public record Shutdown() : Event();
public record Log(LogType Type, string Message) : Event();
public record TagbagFileSet(Tagbag.Core.Tagbag? Tagbag) : Event();

// The collection of entries have been changed. This may include
// cursor and filter changes.
public record EntriesUpdated() : Event();
// Highlight the given entry.
public record ShowEntry(Entry? Entry) : Event();
public record CursorMoved(int? Index) : Event();
public record MarkedChanged() : Event();

public record FilterCommand(IFilter Filter) : Event();
public record TagCommand(ITagOperation Operation) : Event();

// The entries in view has changed. StartIndex is the index in
// EntryCollection with the first visible entry. VisibleItems is the
// number of entries that are visible.
public record ViewChanged(int StartIndex, int VisibleItems) : Event();
