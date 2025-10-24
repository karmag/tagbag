using System.Collections.Generic;
using System.IO;

namespace Tagbag.Core;

public class Scanner
{
    private Tagbag _tb;
    private bool _recursive;
    private bool _populateImageTags;
    private bool _populateFileTags;

    private HashSet<string> KnownFileExtensions =
        new HashSet<string>([".jpg", ".jpeg", ".png", ".bmp", ".gif"]);

    public Scanner(Tagbag tb)
    {
        _tb = tb;
    }

    public Scanner Recursive() { _recursive = true; return this; }
    public Scanner PopulateAllTags() { _populateImageTags = true; _populateFileTags = true; return this; }
    public Scanner PopulateImageTags() { _populateImageTags = true; return this; }
    public Scanner PopulateFileTags() { _populateFileTags = true; return this; }

    // Returns files that match known image extensions. The full path
    // of the file is returned.
    private LinkedList<string> ListFiles(string? rootPath)
    {
        var result = new LinkedList<string>();
        var stack = new Stack<string>([TagbagUtil.GetPath(_tb, rootPath)]);

        while (stack.Count > 0)
        {
            var path = stack.Pop();

            if (File.Exists(path))
            {
                var ext = Path.GetExtension(path).ToLower();
                if (KnownFileExtensions.Contains(ext))
                    result.AddLast(path);
            }
            else if (Directory.Exists(path))
            {
                foreach (var file in Directory.EnumerateFiles(path))
                    stack.Push(file);

                if (_recursive)
                    foreach (var dir in Directory.EnumerateDirectories(path))
                        stack.Push(dir);
            }
        }

        return result;
    }

    private bool PathExistsInTagbag(string entryPath)
    {
        foreach (var entry in _tb.GetEntries())
            if (entry.Path == entryPath)
                return true;
        return false;
    }

    public LinkedList<Entry> Scan(string? startPath)
    {
        var result = new LinkedList<Entry>();
        var tbRoot = TagbagUtil.GetRootDirectory(_tb);

        foreach (var path in ListFiles(startPath))
        {
            var relative = Path.GetRelativePath(tbRoot, path);
            if (!PathExistsInTagbag(relative))
            {
                var entry = new Entry(relative);
                _tb.Add(entry);
                result.AddLast(entry);

                if (_populateImageTags)
                    TagbagUtil.PopulateImageTags(_tb, entry);

                if (_populateFileTags)
                    TagbagUtil.PopulateFileTags(_tb, entry);
            }
        }

        return result;
    }
}
