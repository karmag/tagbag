using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Tagbag.Core;

public class Check
{
    public enum State { None, Running, Stopping }

    public class ReportData
    {
        public int ProblemsFound = 0;
        public int ProblemsFixed = 0;
    }

    public Action<State, State>? StateChange;
    public Action? ReportChanged;
    public ReportData Report;

    private Tagbag _Tagbag;
    private State _State;
    private ConcurrentDictionary<Problem, bool> _Problems;

    private int _ActiveWorkerCount;

    public Check(Tagbag tb)
    {
        _Tagbag = tb;
        _State = State.None;
        _Problems = new ConcurrentDictionary<Problem, bool>();

        Report = new ReportData();
    }

    public State GetState()
    {
        return _State;
    }

    public List<Problem> GetProblems()
    {
        return new List<Problem>(_Problems.Keys);
    }

    private void ReportProblem(Problem problem)
    {
        _Problems[problem] = true;
        Report.ProblemsFound = _Problems.Count;
        ReportChanged?.Invoke();
    }

    private void ReportFix(Problem problem)
    {
        bool b;
        _Problems.Remove(problem, out b);

        Report.ProblemsFixed += 1;
        ReportChanged?.Invoke();
    }

    // Run checks for the tagbag. Return true if checking was started,
    // returns false if something is already running.
    public bool Scan()
    {
        if (CompareAndSet(State.None, State.Running))
        {
            _Problems = new ConcurrentDictionary<Problem, bool>();
            Report.ProblemsFound = 0;
            Report.ProblemsFixed = 0;

            var bc = new BlockingCollection<Action>();
            bc.Add(Check_FileChecks);
            bc.Add(Check_MissingsDefaultTags);
            bc.Add(Check_DuplicateEntries);
            bc.CompleteAdding();

            StartWorkers(bc);

            return true;
        }

        return false;
    }

    public bool Fix()
    {
        if (CompareAndSet(State.None, State.Running))
        {
            var bc = new BlockingCollection<Action>();
            bc.Add(Fix_All);
            bc.CompleteAdding();

            StartWorkers(bc);

            return true;
        }

        return false;
    }

    // Stops running checks or fixes. This method only indicates that
    // processing should be stopped and may not have happened when
    // this method returns. Use Await to wait for full termination.
    public void Stop()
    {
        CompareAndSet(State.Running, State.Stopping);
    }

    // Blocks the current thread until processing is complete
    public void Await()
    {
        var semaphore = new Semaphore(0, 1);

        var f = (State _, State newState) =>
        {
            if (newState == State.None)
                semaphore.Release();
        };

        StateChange += f;

        if (_State != State.None)
            semaphore.WaitOne();

        StateChange -= f;
    }

    private bool IsRunning() { return _State == State.Running; }

    // Change the state to newState if the current state matches oldState.
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

    // Starts the given number of workers to process Actions from the
    // collection. Workers will stop once the collection is exhausted
    // as indicated by CompleteAdding or Stop is called.
    private List<Task> StartWorkers(BlockingCollection<Action> bc)
    {
        var amount = Math.Min(10, bc.Count);

        lock (this)
        {
            if (!IsRunning())
                return [];

            Interlocked.Add(ref _ActiveWorkerCount, amount);
        }

        var tasks = new List<Task>();

        for (; amount > 0; amount--)
        {
            tasks.Add(
                Task.Run(() =>
                {
                    Action? action;
                    while (IsRunning())
                    {
                        try
                        {
                            if (bc.TryTake(out action, 250)) // 250 ms hang time
                                action.Invoke();

                            // TryTake will not throw
                            // InvalidOperationException as signalled by
                            // CompleteAdding when collection is empty and
                            // done so have to specifically check
                            // IsCompleted.
                            if (bc.IsCompleted)
                                break;
                        }
                        catch (Exception e)
                        {
                            System.Console.WriteLine(e);
                        }
                    }

                    lock (this)
                    {
                        var workers = Interlocked.Decrement(ref _ActiveWorkerCount);
                        if (workers == 0)
                        {
                            var old = _State;
                            _State = State.None;
                            if (old != _State)
                                StateChange?.Invoke(old, _State);
                        }
                    }
                })
            );
        }

        return tasks;
    }

    private void Check_FileChecks()
    {
        // entry path -> full path
        Dictionary<string, string> newFiles = new Dictionary<string, string>();

        var dirsToCheck = new Stack<string>();
        dirsToCheck.Push(TagbagUtil.GetRootDirectory(_Tagbag));

        while (dirsToCheck.Count > 0 && IsRunning())
        {
            var path = dirsToCheck.Pop();
            if (Directory.Exists(path))
            {
                foreach (var filePath in Directory.EnumerateFiles(path))
                {
                    if (TagbagUtil.IsKnownFileExtension(filePath))
                        newFiles.Add(TagbagUtil.GetEntryPath(_Tagbag, filePath),
                                     filePath);
                }

                foreach (var dirPath in Directory.EnumerateDirectories(path))
                    dirsToCheck.Push(dirPath);
            }
        }

        HashSet<Entry> entriesWithMissingFiles = new HashSet<Entry>();
        foreach (var entry in _Tagbag.GetEntries())
        {
            if (!File.Exists(TagbagUtil.GetPath(_Tagbag, entry.Path)))
                entriesWithMissingFiles.Add(entry);

            newFiles.Remove(entry.Path);
        }

        // The value type of matchLookup is a ConcurrentDictionary to
        // support arbitrary removal of items, the bool value in that
        // type is not significant.
        Dictionary<int, ConcurrentDictionary<NewFileData, bool>> matchLookup =
            new Dictionary<int, ConcurrentDictionary<NewFileData, bool>>();
        foreach (var kv in newFiles)
        {
            var data = new NewFileData(kv.Key, kv.Value);
            ConcurrentDictionary<NewFileData, bool>? value;
            if (matchLookup.TryGetValue(data.GetSize(), out value))
            {
                value[data] = true;
            }
            else
            {
                value = new ConcurrentDictionary<NewFileData, bool>();
                value[data] = true;
                matchLookup.Add(data.GetSize(), value);
            }
        }

        // find moved files

        ConcurrentQueue<Entry> removedEntries = new ConcurrentQueue<Entry>();

        var bc = new BlockingCollection<Action>();
        foreach (var entry in entriesWithMissingFiles)
            bc.Add(() => Check_FindMovedFiles(entry, matchLookup, removedEntries));
        bc.CompleteAdding();

        var tasks = StartWorkers(bc);
        Task.WaitAll(tasks);

        foreach (var entry in removedEntries)
            entriesWithMissingFiles.Remove(entry);

        // find deleted files

        foreach (var entry in entriesWithMissingFiles)
            ReportProblem(new FileMissing(entry));

        // find new files

        foreach (var sizeMatch in matchLookup)
            foreach (var dataKv in sizeMatch.Value)
                ReportProblem(new NonIndexedFile(dataKv.Key.FullPath));
    }

    private void Check_FindMovedFiles(
        Entry entry,
        Dictionary<int, ConcurrentDictionary<NewFileData, bool>> matchLookup,
        ConcurrentQueue<Entry> removedEntries)
    {
        var allSizes = entry.GetInts(Const.Size);
        var allHashes = entry.GetStrings(Const.Hash);

        if (allSizes == null || allHashes == null)
            return;

        ConcurrentDictionary<NewFileData, bool>? fileData;
        foreach (var size in allSizes)
        {
            if (matchLookup.TryGetValue(size, out fileData))
            {
                NewFileData? selectedFile = null;
                foreach (var data in fileData.Keys)
                {
                    if (allHashes.Contains(data.GetHash()))
                    {
                        selectedFile = data;
                        break;
                    }
                }

                if (selectedFile is NewFileData)
                {
                    bool _;
                    if (fileData.TryRemove(selectedFile, out _))
                    {
                        removedEntries.Enqueue(entry);
                        ReportProblem(new FileMoved(entry, selectedFile.FullPath));
                        break;
                    }
                }
            }
        }
    }

    private class NewFileData
    {
        public string EntryPath;
        public string FullPath;
        private int? _Size;
        private string? _Hash;

        public NewFileData(string entryPath, string fullPath)
        {
            EntryPath = entryPath;
            FullPath = fullPath;
        }

        public int GetSize()
        {
            if (_Size == null)
                lock (this)
                    if (_Size == null)
                        _Size = (int)new FileInfo(FullPath).Length;
            return _Size ?? 0;
        }

        public string GetHash()
        {
            if (_Hash == null)
                lock (this)
                    if (_Hash == null)
                        _Hash = TagbagUtil.GetFileHash(FullPath);
            return _Hash ?? "";
        }
    }

    private void Check_MissingsDefaultTags()
    {
        var missing = new List<string>();
        foreach (var entry in _Tagbag.GetEntries())
        {
            foreach (var tag in Const.BuiltinTags)
                if (entry.Get(tag) == null)
                    missing.Add(tag);

            if (missing.Count > 0)
            {
                ReportProblem(new MissingDefaultTags(entry, missing));
                missing = new List<string>();
            }
        }
    }

    private void Check_DuplicateEntries()
    {
        Dictionary<(int, string), List<Entry>> grouped =
            new Dictionary<(int, string), List<Entry>>();

        foreach (var entry in _Tagbag.GetEntries())
            foreach (var size in entry.GetInts(Const.Size) ?? [])
                foreach (var hash in entry.GetStrings(Const.Hash) ?? [])
                {
                    var key = (size, hash);
                    List<Entry>? entries;
                    if (grouped.TryGetValue(key, out entries))
                    {
                        entries.Add(entry);
                    }
                    else
                    {
                        entries = new List<Entry>();
                        entries.Add(entry);
                        grouped.Add(key, entries);
                    }
                }

        var bc = new BlockingCollection<Action>();
        foreach (var entries in grouped.Values)
            if (entries.Count > 1)
                bc.Add(() => Check_DuplicateEntriesCheck(entries));
        bc.CompleteAdding();

        StartWorkers(bc);
    }

    private void Check_DuplicateEntriesCheck(List<Entry> entries)
    {
        while (entries.Count > 1 && IsRunning())
        {
            var equal = new List<Entry>();
            equal.Add(entries[entries.Count - 1]);
            entries.RemoveAt(entries.Count - 1);

            for (int index = 0; index < entries.Count && IsRunning();)
            {
                if (AreFilesIdentical(equal[0], entries[index]))
                {
                    equal.Add(entries[index]);
                    entries.RemoveAt(index);
                }
                else
                    index++;
            }

            if (equal.Count > 1)
                ReportProblem(new DuplicateFiles(equal));
        }
    }

    // Returns true if the given entries point to binary equivalent
    // files.
    //
    // Returns false if the entries are pointing to the same file.
    // This is to avoid accidentally deleting files that have been
    // indexed multiple times.
    private bool AreFilesIdentical(Entry a, Entry b)
    {
        var aBytes = new byte[10 * 1024];
        var bBytes = new byte[10 * 1024];

        var aPath = TagbagUtil.GetPath(_Tagbag, a.Path);
        var bPath = TagbagUtil.GetPath(_Tagbag, b.Path);

        if (aPath == bPath || !File.Exists(aPath) || !File.Exists(bPath))
            return false;

        using (FileStream aStream = File.Open(aPath, FileMode.Open))
        {
            using (FileStream bStream = File.Open(bPath, FileMode.Open))
            {
                while (true)
                {
                    var aCount = aStream.Read(aBytes);
                    var bCount = bStream.Read(bBytes);

                    if (aCount != bCount)
                        return false;

                    if (aCount == 0) // End of file
                        break;

                    if (!aBytes.SequenceEqual<byte>(bBytes))
                        return false;
                }
            }
        }

        return true;
    }

    private void Fix_All()
    {
        var fixData = new FixData(_Tagbag);
        var bc = new BlockingCollection<Action>();

        foreach (var problem in _Problems.Keys)
            bc.Add(() => {
                problem.Fix(fixData);
                ReportFix(problem);
            });
        bc.CompleteAdding();

        StartWorkers(bc);
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
        var entry = new Entry(_Entry.Id,
                              TagbagUtil.GetEntryPath(fix.GetTagbag(), _NewPath));

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

public class DuplicateFiles : AbstractProblem
{
    public DuplicateFiles(IEnumerable<Entry> entries)
    {
        _Entries = new HashSet<Entry>(entries);
        _Cause = "Duplicate files";
        _Details = $"Duplicate files [{String.Join(", ", entries.Select((entry) => entry.Path))}]";
    }

    override public void Fix(FixData fix)
    {
        var entries = new List<Entry>(_Entries);
        entries.Sort((Entry a, Entry b) => { return String.Compare(a.Path, b.Path); });

        var master = entries[0];
        for (int i = 1; i < entries.Count; i++)
            if (entries[i].Path.Length > master.Path.Length)
                master = entries[i];

        entries.Remove(master);

        foreach (var other in entries)
        {
            foreach (var key in other.GetAllTags())
            {
                if (other.Get(key) is Value val)
                {
                    if (val.IsTag())
                        master.Add(key);

                    foreach (var i in val.GetInts() ?? [])
                        master.Add(key, i);

                    foreach (var s in val.GetStrings() ?? [])
                        master.Add(key, s);
                }
            }

            TagbagUtil.MoveToTrash(fix.GetTagbag(), other);

            fix.UpdateTagbag((tb) => tb.Remove(other.Id));
        }
    }
}
