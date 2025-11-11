using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Tagbag.Core;

public static class Json
{
    public static Tagbag Read(string path)
    {
        var node = ReadFile(path);
        return DecodeTagbag(path, new JsonZipper(node));
    }

    public static void Write(Tagbag tb, string path)
    {
        var json = Encode(tb);
        using(var stream = File.Open(path, FileMode.Create))
        {
            JsonSerializer.Serialize(stream, json);
        }
    }

    public static void PrettyPrint(Tagbag tb)
    {
        System.Console.WriteLine(PrettyString(Encode(tb)));
    }

    public static string PrettyString(JsonNode node)
    {
        return JsonSerializer.Serialize(
            node,
            new JsonSerializerOptions{ WriteIndented = true });
    }

    public static JsonNode ReadFile(string path)
    {
        using (var stream = File.Open(path, FileMode.Open))
        {
            if (JsonNode.Parse(stream) is JsonNode node)
                return node;
        }
        throw new IOException($"Failed to read JSON from {path}");
    }

    public static JsonObject Encode(Tagbag tb)
    {
        var result = new JsonObject();

        var entries = new JsonArray();
        foreach (var entry in tb.GetEntries())
            entries.Add(Encode(entry));
        result.Add("Entries", entries);

        return result;
    }

    public static JsonObject Encode(Entry entry)
    {
        var result = new JsonObject();

        result.Add("Id", entry.Id);
        result.Add("Path", entry.Path);

        var tags = new JsonObject();
        foreach (var tag in entry.GetAllTags())
            if (entry.Get(tag) is Value value)
                tags.Add(tag, Encode(value));
        result.Add("Tags", tags);

        return result;
    }

    public static JsonArray Encode(Value value)
    {
        var result = new JsonArray();

        if (value.IsTag())
            result.Add(true);

        foreach (var str in value.GetStrings() ?? [])
            result.Add(str);

        foreach (var i in value.GetInts() ?? [])
            result.Add(i);

        return result;
    }

    public static Tagbag DecodeTagbag(string path, JsonZipper node)
    {
        var tb = new Tagbag(path);

        node.Get("Entries").MapArray((elem) => tb.Add(DecodeEntry(elem)));

        return tb;
    }

    public static Entry DecodeEntry(JsonZipper node)
    {
        var entry = new Entry(new Guid(node.Get("Id").As<string>()),
                              node.Get("Path").As<string>());

        node.Get("Tags").MapObject((key, val) => {
            entry.Set(key, DecodeValue(val));
        });

        return entry;
    }

    public static Value DecodeValue(JsonZipper node)
    {
        var value = new Value();

        node.MapArray((elem) => {
            switch (elem.GetKind())
            {
                case JsonValueKind.True:
                    value.SetTag(true);
                    break;

                case JsonValueKind.String:
                    value.Add(elem.As<string>());
                    break;

                case JsonValueKind.Number:
                    value.Add(elem.As<int>());
                    break;

                default:
                    throw new InvalidOperationException($"Unknown value type for {elem.ToString()}");
            }
        });

        return value;
    }
}

public class JsonZipper
{
    private JsonNode? _Node;

    public JsonZipper(JsonNode? node)
    {
        _Node = node;
    }

    private JsonNode Get()
    {
        if (_Node != null)
            return _Node;
        throw new InvalidOperationException("Node is null");
    }

    private JsonZipper AssertKind(JsonValueKind kind)
    {
        var nodeKind = Get().GetValueKind();
        if (nodeKind != kind)
        {
            throw new InvalidOperationException($"Expected JSON kind {kind} but was {nodeKind}: {Get().ToString()}");
        }
        return this;
    }

    public JsonZipper Get(string key)
    {
        AssertKind(JsonValueKind.Object);
        JsonNode? val;
        if (Get().AsObject().TryGetPropertyValue(key, out val) && val != null)
            return new JsonZipper(val);
        throw new InvalidOperationException($"Property '{key}' doesn't exist");
    }

    public T As<T>()
    {
        T? t;
        if (Get().AsValue().TryGetValue(out t) && t != null)
            return t;
        throw new InvalidOperationException($"Value is not of correct type: {ToString()}");
    }

    public void MapArray(Action<JsonZipper> f)
    {
        AssertKind(JsonValueKind.Array);
        foreach (var elem in Get().AsArray())
            f(new JsonZipper(elem));
    }

    public void MapObject(Action<string, JsonZipper> f)
    {
        AssertKind(JsonValueKind.Object);
        foreach (var kv in Get().AsObject())
            f(kv.Key, new JsonZipper(kv.Value));
    }

    public JsonValueKind GetKind()
    {
        return Get().GetValueKind();
    }

    override public string ToString()
    {
        if (_Node != null)
            return Json.PrettyString(_Node);
        return "<null value>";
    }
}
