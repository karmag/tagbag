using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tagbag.Core;

public class Scanner
{
    private Tagbag _Tagbag;
    private bool _Running;
    private int _WorkerCount;

    private ConcurrentQueue<string> _DirectoryQueue;
    private ConcurrentQueue<string> _FileQueue;
    private ConcurrentQueue<Entry> _EntryQueue;

    private HashSet<string> KnownFileExtensions =
        new HashSet<string>([".jpg", ".jpeg", ".png", ".bmp", ".gif"]);

    private Counter _Counter;
    public Action<Counter>? ProgressReport;

    public class Counter
    {
        public int DirectoriesQueued;
        public int DirectoriesRemaining;

        public int FilesQueued;
        public int FilesRemaining;

        public int EntriesQueued;
        public int EntriesRemaining;

        public bool Completed;
    }

    public Scanner(Tagbag tb, string? directory)
    {
        var rootDir = TagbagUtil.GetRootDirectory(tb);

        if (!Path.IsPathRooted(directory))
            directory = Path.Join(rootDir, directory);

        if (!Path.GetFullPath(directory).StartsWith(rootDir))
            throw new ArgumentException($"\"{directory}\" is not in Tagbag path \"{rootDir}\"");

        if (!Directory.Exists(directory))
            throw new ArgumentException($"Directory \"{directory}\" doesn't exist");

        _Tagbag = tb;
        _Running = false;
        _WorkerCount = 1;

        _DirectoryQueue = new ConcurrentQueue<string>();
        _FileQueue = new ConcurrentQueue<string>();
        _EntryQueue = new ConcurrentQueue<Entry>();

        _Counter = new Counter();

        _DirectoryQueue.Enqueue(directory);
        _Counter.DirectoriesQueued++;
    }

    public void Start()
    {
        lock (this)
        {
            if (!_Running)
            {
                _Running = true;
                for (int i = 0; i < _WorkerCount; i++)
                    Task.Run(StepContinuously);
            }
        }
    }

    public void Stop()
    {
        lock (this)
        {
            if (_Running)
            {
                _Running = false;
            }
        }
    }

    private void Report()
    {
        _Counter.DirectoriesRemaining = _DirectoryQueue.Count;
        _Counter.FilesRemaining = _FileQueue.Count;
        _Counter.EntriesRemaining = _EntryQueue.Count;
        ProgressReport?.Invoke(_Counter);
    }

    private void StepContinuously()
    {
        // TODO: this doesn't work with multiple workers. Work is
        // removed from queue and may repopulate queues, so work can
        // exist both in the queues and in the tasks. Tasks don't know
        // when there's no more work to be done.
        while (_Running)
        {
            if (StepOne())
            {
                Report();
            }
            else
            {
                Stop();
            }
        }
        _Counter.Completed = true;
        Report();
    }

    private bool StepOne()
    {
        return StepDirectory() || StepFile() || StepEntry();
    }

    private bool StepDirectory()
    {
        string? path;
        if (_DirectoryQueue.TryDequeue(out path))
        {
            foreach (var dirPath in Directory.EnumerateDirectories(path))
            {
                _DirectoryQueue.Enqueue(dirPath);
                _Counter.DirectoriesQueued++;
            }

            foreach (var filePath in Directory.EnumerateFiles(path))
            {
                _FileQueue.Enqueue(filePath);
                _Counter.FilesQueued++;
            }

            return true;
        }
        return false;
    }

    private bool StepFile()
    {
        string? path;
        if (_FileQueue.TryDequeue(out path))
        {
            var ext = Path.GetExtension(path).ToLower();
            if (KnownFileExtensions.Contains(ext))
            {
                var relativePath = Path.GetRelativePath(
                    TagbagUtil.GetRootDirectory(_Tagbag), path);
                var exists = false;
                foreach (var entry in _Tagbag.GetEntries())
                {
                    if (entry.Path == relativePath)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    _EntryQueue.Enqueue(new Entry(relativePath));
                    _Counter.EntriesQueued++;
                }
            }
            return true;
        }
        return false;
    }

    private bool StepEntry()
    {
        Entry? entry;
        if (_EntryQueue.TryDequeue(out entry))
        {
            _Tagbag.Add(entry);
            TagbagUtil.PopulateImageTags(_Tagbag, entry);
            TagbagUtil.PopulateFileTags(_Tagbag, entry);
            return true;
        }
        return false;
    }
}
