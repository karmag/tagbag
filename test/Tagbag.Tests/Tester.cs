using System;
using System.Collections.Generic;
using System.IO;
using Tagbag.Core;

namespace Tagbag.Tests;

public static class Tester
{
    public static Entry Entry(params Object?[][] kvArgs)
    {
        var entry = new Entry("a");

        foreach (var kv in kvArgs)
        {
            if (kv[0] is string k)
            {
                if (kv.Length == 1 || kv[1] is null)
                    entry.Add(k);
                else
                {
                    for (int index = 1; index < kv.Length; index++)
                    {
                        if (kv[index] is string v)
                            entry.Add(k, v);
                        else if (kv[index] is int i)
                            entry.Add(k, i);
                        else
                            throw new ArgumentException($"Unknown value type for key {k}: {kv[1]}");
                    }
                }
            }
            else
            {
                throw new ArgumentException($"Keys must be strings: {kv[0]}");
            }
        }

        return entry;
    }

    public static void AssertTagsMatch(Entry entry, params Object?[][] kvArgs)
    {
        var other = Entry(kvArgs);
        var tags = new HashSet<string>(entry.GetAllTags());
        tags.UnionWith(other.GetAllTags());

        foreach (var tag in tags)
        {
            var a = entry.Get(tag);
            var b = other.Get(tag);

            if (a is Value aVal)
            {
                if (!aVal.Equals(b))
                    throw new ValidationException(
                        $"{tag} -> {aVal.ToString()} != {b?.ToString()}");
            }
            else if (b is Value bVal)
            {
                if (!bVal.Equals(a))
                    throw new ValidationException(
                        $"{tag} -> {a?.ToString()} != {bVal.ToString()}");
            }
            else
            {
                throw new InvalidOperationException("What?");
            }
        }
    }

    private class ValidationException(string msg) : Exception(msg);
}
