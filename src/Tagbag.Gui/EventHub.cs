using System;
using System.Collections.Generic;
using System.Threading;
using Tagbag.Core;

namespace Tagbag.Gui;

public class EventHub
{
    private Stack<Event> _EventQueue;
    private Semaphore _Lock;

    public Action<EntriesReset>? EntriesReset;
    public Action<ShowEntry>? ShowEntry;

    public Action<FilterCommand>? FilterCommand;
    public Action<TagCommand>? TagCommand;

    public EventHub()
    {
        _EventQueue = new Stack<Event>();
        _Lock = new Semaphore(0, 1);
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
        System.Console.WriteLine($"Processing event: {ev}");

        switch (ev)
        {
            case EntriesReset e: EntriesReset?.Invoke(e); break;
            case ShowEntry e: ShowEntry?.Invoke(e); break;

            case FilterCommand e: FilterCommand?.Invoke(e); break;
            case TagCommand e: TagCommand?.Invoke(e); break;

            default:
                throw new ArgumentException($"Unknown event type {ev}");
        }
    }
}

public abstract record class Event();

// Entries have been changed in a way that previous entries are no
// longer valid.
public record EntriesReset() : Event();
// Highlight the given entry.
public record ShowEntry(Entry? entry) : Event();

public record FilterCommand(string command) : Event();
public record TagCommand(string command) : Event();
