using System;
using System.Collections.Generic;
using System.IO;

namespace Tagbag.Core;

public static class Const
{
    public const string Filename = ".tagbag";
    public const string Width = "width";
    public const string Height = "height";
    public const string Size = "size";
    public const string Hash = "hash/sha256";

    public static IReadOnlySet<string> BuiltinTags =
        new HashSet<string>(new string[]{Width, Height, Size, Hash});
}

public class Tagbag
{
    public string Path { get; set; }
    private Dictionary<Guid, Entry> Entries;

    // Use static methods New or Open to create a Tagbag object.
    public Tagbag(string path)
    {
        Path = System.IO.Path.GetFullPath(path);
        Entries = new Dictionary<Guid, Entry>();
    }

    // Create a new tagbag file in the given directory. Returns the
    // corresponding Tagbag object. The operation will fail if the
    // directory doesn't exist or if the tagbag file does.
    //
    // Passing null will use the current working directory.
    public static Tagbag New(string? directory)
    {
        if (directory == null)
            directory = Directory.GetCurrentDirectory();

        if (!Directory.Exists(directory))
            throw new ArgumentException($"Directory doesn't exist {directory}");

        var tagbagPath = System.IO.Path.GetFullPath(
            System.IO.Path.Join(directory, Const.Filename));

        if (System.IO.Path.Exists(tagbagPath))
            throw new ArgumentException($"{Const.Filename} file already exists in {directory}");

        var tb = new Tagbag(tagbagPath);
        tb.Save();
        return tb;
    }

    // Opens the given tagbag file. If path directly names a file it
    // is opened. If the named file is not a tagbag file an exception
    // is raised. If not naming a file Locate is used to find a tagbag
    // file. path can be null to use the current working directory.
    //
    // Throws an exception if not tagbag file can be found.
    public static Tagbag Open(string? path)
    {
        if (File.Exists(path))
        {
            if (System.IO.Path.GetFileName(path) == Const.Filename)
                return Load(path);
            else
                throw new ArgumentException($"{path} is not a tagbag file");
        }

        var tagbagPath = Locate(path);
        if (tagbagPath != null)
            return Load(tagbagPath);

        if (path == null)
            path = Directory.GetCurrentDirectory();
        throw new ArgumentException($"No {Const.Filename} file found from {path}");
    }

    // Locate finds the path to the nearest tagbag file given a
    // starting path. Parent directories are searched recursively
    // until either a tagbag file is found or the root is reached.
    //
    // Returns null if no tagbag file is found.
    //
    // If null is passed to Locate the current working directory is
    // used as starting path.
    public static string? Locate(string? startPath)
    {
        if (startPath == null)
            startPath = Directory.GetCurrentDirectory();

        var path = System.IO.Path.GetFullPath(startPath);
        if (System.IO.Path.GetFileName(path) == Const.Filename && File.Exists(path))
            return path;

        while (path != null)
        {
            var tbPath = System.IO.Path.Join(path, Const.Filename);
            if (File.Exists(tbPath))
                return tbPath;

            path = System.IO.Path.GetDirectoryName(path);
        }

        return null;
    }

    private static Tagbag Load(string path)
    {
        var tb = Json.Read(path);
        tb.Path = path;
        return tb;
    }

    public void Save()
    {
        Json.Write(this, Path);
    }

    // Saves the tagbag with a file extension that indicates the
    // current date and time.
    public void Backup()
    {
        var now = DateTime.Now;
        Json.Write(this, $"{Path}_{now:yyyyMMdd_HHmmss}");
    }

    public void Add(Entry entry)
    {
        Entries.Add(entry.Id, entry);
    }

    public Entry? Get(Guid id)
    {
        Entry? entry;
        Entries.TryGetValue(id, out entry);
        return entry;
    }

    public void Remove(Guid id)
    {
        Entries.Remove(id);
    }

    public ICollection<Entry> GetEntries()
    {
        return Entries.Values;
    }
}

public class Entry
{
    public readonly Guid Id;
    public readonly string Path;
    private Dictionary<string, Value> Tags;

    public Entry(string path)
    {
        Id = Guid.NewGuid();
        Path = TagbagUtil.NormalizePath(path);
        Tags = new Dictionary<string, Value>();
    }

    public Entry(Guid id, string path)
    {
        Id = id;
        Path = TagbagUtil.NormalizePath(path);
        Tags = new Dictionary<string, Value>();
    }

    private Value GetOrCreateValue(string tag)
    {
        Value? val = null;
        if (!Tags.TryGetValue(tag, out val))
        {
            val = new Value();
            Tags[tag] = val;
        }
        return val;
    }

    // Update the value of tag by applying the function. If create is
    // true and the value doesn't exist it is created first. If the
    // value is not set after the function is applied it is removed.
    private void UpdateValue(string tag, bool create, Action<Value> f)
    {
        Value? val = null;
        if (Tags.TryGetValue(tag, out val))
        {
            f(val);
            if (!val.IsSet())
                Tags.Remove(tag);
        }
        else if (create)
        {
            val = new Value();
            f(val);
            if (val.IsSet())
                Tags[tag] = val;
        }
    }

    private Value? GetEx(string tag)
    {
        Value? val = null;
        Tags.TryGetValue(tag, out val);
        return val;
    }

    public void Add(string tag)               { GetOrCreateValue(tag).SetTag(true); }
    public void Add(string tag, string value) { GetOrCreateValue(tag).Add(value); }
    public void Add(string tag, int value)    { GetOrCreateValue(tag).Add(value); }

    public void Remove(string tag)               { Tags.Remove(tag); }
    public void Remove(string tag, string value) { UpdateValue(tag, false, v => v.Remove(value)); }
    public void Remove(string tag, int value)    { UpdateValue(tag, false, v => v.Remove(value)); }

    public void Set(string tag, string value) { Remove(tag); Add(tag, value); }
    public void Set(string tag, int value)    { Remove(tag); Add(tag, value); }
    public void Set(string tag, Value value)  {
        if (value.IsSet())
            Tags[tag] = value;
        else
            Remove(tag);
    }

    public Value? Get(string tag)                  { return GetEx(tag); }
    public HashSet<string>? GetStrings(string tag) { return GetEx(tag)?.GetStrings(); }
    public HashSet<int>? GetInts(string tag)       { return GetEx(tag)?.GetInts(); }

    public IEnumerable<string> GetAllTags()
    {
        return Tags.Keys;
    }
}

public class Value
{
    private bool Tag;
    private HashSet<string>? Strings;
    private HashSet<int>? Ints;

    // Returns true if this value is set in some way.
    public bool IsSet()
    {
        return Tag || Strings != null || Ints != null;
    }

    // Returns true if only tag is set.
    public bool IsMarker()
    {
        return Tag && Strings == null && Ints == null;
    }

    public void SetTag(bool isTag)
    {
        Tag = isTag;
    }

    // Returns true if tag is set.
    public bool IsTag()
    {
        return Tag;
    }

    public void Add(string s)
    {
        Strings ??= new HashSet<string>();
        Strings.Add(s);
    }

    public void Remove(string s)
    {
        if (Strings != null)
            if (Strings.Remove(s))
                if (Strings.Count == 0)
                    Strings = null;
    }

    public HashSet<string>? GetStrings()
    {
        return Strings;
    }

    public void Add(int i)
    {
        Ints ??= new HashSet<int>();
        Ints.Add(i);
    }

    public void Remove(int i)
    {
        if (Ints != null)
            if (Ints.Remove(i))
                if (Ints.Count == 0)
                    Ints = null;
    }

    public HashSet<int>? GetInts()
    {
        return Ints;
    }

    override public bool Equals(Object? other)
    {
        if (other is Value val)
        {
            if (Tag != val.Tag)
                return false;

            if (Strings == null && val.Strings != null)
                return false;

            if (!Strings?.SetEquals(val.Strings ?? []) ?? false)
                return false;

            if (Ints == null && val.Ints != null)
                return false;

            if (!Ints?.SetEquals(val.Ints ?? []) ?? false)
                return false;

            return true;
        }

        return false;
    }

    override public int GetHashCode()
    {
        return (ToString() ?? "").GetHashCode();
    }

    override public string? ToString()
    {
        var sb = new System.Text.StringBuilder();

        var write = (string x) =>
        {
            if (sb.Length == 0)
                sb.Append("[");
            else
                sb.Append(", ");
            sb.Append(x);
        };

        if (Tag)
            write("true");

        if (Strings != null)
            foreach (var s in Strings)
                write($"\"{s}\"");

        if (Ints != null)
            foreach (var i in Ints)
                write(i.ToString());

        sb.Append("]");
        return sb.ToString();
    }

    public Value Clone()
    {
        var value = new Value();
        value.SetTag(IsTag());
        foreach (string str in Strings ?? [])
            value.Add(str);
        foreach (int i in Ints ?? [])
            value.Add(i);
        return value;
    }
}
