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
                            FindNonIndexedFiles,
                            FindMissingTags,
                            FindMovedOrDeletedFiles);

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

    private void FindMissingTags()
    {
        Progress?.Invoke("Finding missing tags", 0, 0);
        var failed = 0;

        var missing = new List<string>();
        foreach (var entry in _Tagbag.GetEntries())
        {
            foreach (var tag in Const.BuiltinTags)
                if (entry.Get(tag) == null)
                    missing.Add(tag);

            if (missing.Count > 0)
            {
                _FoundProblems.AddLast(new MissingDefaultTags(entry, missing));
                missing = new List<string>();
                failed++;
                Progress?.Invoke("Finding missing tags", failed, 0);
            }
        }

        Progress?.Invoke("Finding missing tags", failed, failed);
    }

    private void FindMovedOrDeletedFiles()
    {
        Progress?.Invoke("Finding moved/deleted files", 0, 0);
        var found = 0;

        var fileData = new List<(string path, int size, string hash)>();

        foreach (var entry in _Tagbag.GetEntries())
        {
            var entryPath = TagbagUtil.GetPath(_Tagbag, entry.Path);

            if (!File.Exists(entryPath))
            {
                // Setup data
                if (fileData.Count == 0)
                {
                    foreach (var path in _FoundFiles.Values)
                    {
                        var fileInfo = new FileInfo(path);
                        fileData.Add((path, (int)fileInfo.Length, ""));
                    }
                }

                var moved = false;

                var entrySize = entry.GetInts(Const.Size);
                var entryHash = entry.GetStrings(Const.Hash);
                if (entrySize != null && entryHash != null)
                {
                    for (int i = 0; i < fileData.Count; i++)
                    {
                        var (path, size, hash) = fileData[i];
                        if (entrySize.Contains(size))
                        {
                            // Create hash as needed
                            if (hash.Length == 0)
                            {
                                hash = TagbagUtil.GetFileHash(path);
                                fileData[i] = (path, size, hash);
                            }

                            // When size and hash matches it's assumed
                            // to be the file originally pointed to be
                            // the entry.
                            if (entryHash.Contains(hash))
                            {
                                moved = true;
                                _FoundProblems.AddLast(new FileMoved(entry, path));
                                found++;
                            }
                        }
                    }
                }

                if (!moved)
                {
                    _FoundProblems.AddLast(new FileMissing(entry));
                    found++;
                }
            }

            Progress?.Invoke("Finding moved/deleted files", found, 0);
        }

        Progress?.Invoke("Finding moved/deleted files", found, found);
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

public class MissingDefaultTags : AbstractProblem
{
    private Entry _Entry;
    private List<string> _MissingTags;

    public MissingDefaultTags(Entry entry, List<string> missingTags)
    {
        _Entry = entry;
        _MissingTags = missingTags;

        _Cause = "Missing default tags";
        _Details = $"Missing tags [{String.Join(", ", missingTags)}] for entry {entry.Path}";
        AddEntry(entry);
    }

    override public void Fix(FixData fix)
    {
        var image = false;
        var file = false;

        foreach (var tag in _MissingTags)
        {
            switch (tag)
            {
                case Const.Width:
                case Const.Height:
                    image = true;
                    break;

                case Const.Size:
                case Const.Hash:
                    file = true;
                    break;
            }
        }

        if (image)
            TagbagUtil.PopulateImageTags(fix.GetTagbag(), _Entry);

        if (file)
            TagbagUtil.PopulateFileTags(fix.GetTagbag(), _Entry);
    }
}

public class FileMoved : AbstractProblem
{
    private Entry _Entry;
    private string _NewPath;

    public FileMoved(Entry entry, string newAbsolutePath)
    {
        _Entry = entry;
        _NewPath = newAbsolutePath;

        _Cause = "File moved";
        _Details = $"File moved from {entry.Path} to {newAbsolutePath}";

        AddEntry(entry);
        AddFile(newAbsolutePath);
    }

    override public void Fix(FixData fix)
    {
        var entry = new Entry(_Entry.Id, _NewPath);

        foreach (var key in _Entry.GetAllTags())
            if (_Entry.Get(key) is Value value)
                entry.Set(key, value.Clone());

        fix.UpdateTagbag(tb =>
        {
            tb.Remove(_Entry.Id);
            tb.Add(entry);
        });
    }
}

public class FileMissing : AbstractProblem
{
    private Entry _Entry;

    public FileMissing(Entry entry)
    {
        _Entry = entry;

        _Cause = "File missing";
        _Details = $"File {entry.Path} is missing for entry {entry.Id}";

        AddEntry(entry);
    }

    override public void Fix(FixData fix)
    {
        fix.UpdateTagbag(tb => tb.Remove(_Entry.Id));
    }
}
