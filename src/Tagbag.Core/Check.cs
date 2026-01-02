using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Tagbag.Core;

public class Check
{
    public enum State { None, Running, Stopping }
    public Action<State, State>? StateChange;
    public Action<string, int, int>? Progress;

    private Tagbag _Tagbag;
    // maps entry's relative path to absolute path
    private Dictionary<string, string> _FoundFiles;
    private LinkedList<Problem> _FoundProblems;

    private State _State;

    public Check(Tagbag tb)
    {
        _Tagbag = tb;
        _FoundFiles = new Dictionary<string, string>();
        _FoundProblems = new LinkedList<Problem>();
    }

    public bool Scan()
    {
        if (CompareAndSet(State.None, State.Running))
        {
            Task.Run(RunScan);
            return true;
        }
        return false;
    }

    public bool Fix()
    {
        if (CompareAndSet(State.None, State.Running))
        {
            Task.Run(RunFix);
            return true;
        }
        return false;
    }

    public void Stop()
    {
        CompareAndSet(State.Running, State.Stopping);
    }

    public State GetState()
    {
        return _State;
    }

    public LinkedList<Problem> GetProblems()
    {
        return _FoundProblems;
    }

    private bool CompareAndSet(State oldState, State newState)
    {
        lock (this)
        {
            if (_State == oldState)
            {
                _State = newState;
                StateChange?.Invoke(oldState, newState);
                return true;
            }
            return false;
        }
    }

    private void ForceSet(State state)
    {
        lock (this)
        {
            if (_State != state)
            {
                var old = _State;
                _State = state;
                StateChange?.Invoke(old, state);
            }
        }
    }

    private void RunScan()
    {
        _FoundFiles.Clear();
        _FoundProblems.Clear();

        var operations = new List<Action>();
        operations.AddRange(UpdateFoundFiles,
                            FindNonIndexedFiles);

        try
        {
            foreach (var op in operations)
            {
                op();
                if (_State != State.Running)
                    break;
            }
        }
        finally
        {
            ForceSet(State.None);
        }
    }

    private void UpdateFoundFiles()
    {
        Progress?.Invoke("Collecting files", 0, 0);

        var toCheck = new Stack<string>();
        toCheck.Push(TagbagUtil.GetRootDirectory(_Tagbag));

        while (toCheck.Count > 0 && _State == State.Running)
        {
            var path = toCheck.Pop();
            if (Directory.Exists(path))
            {
                foreach (var filePath in Directory.EnumerateFiles(path))
                {
                    if (TagbagUtil.IsKnownFileExtension(filePath))
                        _FoundFiles.Add(TagbagUtil.GetEntryPath(_Tagbag, filePath),
                                        filePath);
                }

                foreach (var dirPath in Directory.EnumerateDirectories(path))
                    toCheck.Push(dirPath);
            }

            Progress?.Invoke("Collecting files", _FoundFiles.Count, 0);
        }

        Progress?.Invoke("Collecting files", _FoundFiles.Count, _FoundFiles.Count);
    }

    private void FindNonIndexedFiles()
    {
        Progress?.Invoke("Locating missing files", 0, 0);

        var missing = new Dictionary<string, string>(_FoundFiles);

        foreach (var entry in _Tagbag.GetEntries())
            missing.Remove(entry.Path);

        foreach (var kv in missing)
            _FoundProblems.AddLast(new NonIndexedFile(kv.Value));

        Progress?.Invoke("Locating missing files", missing.Count, missing.Count);
    }

    private void RunFix()
    {
        var total = _FoundProblems.Count;
        var node = _FoundProblems.First;
        var fixData = new FixData(_Tagbag);

        Progress?.Invoke("Fixing", 0, total);

        try
        {
            while (node != null)
            {
                var next = node.Next;

                try
                {
                    var problem = node.Value;
                    problem.Fix(fixData);
                    _FoundProblems.Remove(node);
                }
                catch (Exception)
                {
                    // TODO: report?
                }

                node = next;
                Progress?.Invoke("Fixing", total - _FoundProblems.Count, total);
            }
        }
        finally
        {
            ForceSet(State.None);
        }

        Progress?.Invoke("Fixed", total - _FoundProblems.Count, total);
    }
}

public interface Problem
{
    string GetCause();
    string GetDetails();
    HashSet<string> GetFiles();
    HashSet<Entry> GetEntries();
    void Fix(FixData fix);
}

public class FixData
{
    private Tagbag _Tagbag;

    public FixData(Tagbag tb)
    {
        _Tagbag = tb;
    }

    public Tagbag GetTagbag()
    {
        return _Tagbag;
    }

    public void UpdateTagbag(Action<Tagbag> f)
    {
        lock (this)
        {
            f(_Tagbag);
        }
    }
}

public abstract class AbstractProblem : Problem
{
    protected string _Cause = "";
    protected string _Details = "";
    protected HashSet<string> _Files = new HashSet<string>();
    protected HashSet<Entry> _Entries = new HashSet<Entry>();

    public string GetCause() { return _Cause; }
    public string GetDetails() { return _Details; }
    public HashSet<string> GetFiles() { return _Files; }
    public HashSet<Entry> GetEntries() { return _Entries; }
    public abstract void Fix(FixData fix);

    protected void AddFile(string file) { _Files.Add(file); }
    protected void AddEntry(Entry entry) { _Entries.Add(entry); }

    public override string ToString() { return _Details; }
}

public class NonIndexedFile : AbstractProblem
{
    public NonIndexedFile(string file)
    {
        _Cause = "Non indexed file";
        _Details = $"Non indexed file {file}";
        AddFile(file);
    }

    override public void Fix(FixData fix)
    {
        foreach (var file in _Files)
        {
            Entry entry = new Entry(TagbagUtil.GetEntryPath(fix.GetTagbag(), file));
            TagbagUtil.PopulateFileTags(fix.GetTagbag(), entry);
            TagbagUtil.PopulateImageTags(fix.GetTagbag(), entry);
            fix.UpdateTagbag(tb => tb.Add(entry));
        }
    }
}
