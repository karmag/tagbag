using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Tagbag.Core.Tests;

[TestClass]
public class TestFilter
{
    private Entry MakeEntry(params Object?[][] kvArgs)
    {
        var entry = new Entry("a");

        foreach (var kv in kvArgs)
        {
            if (kv[0] is string k)
            {
                if (kv.Length == 1 || kv[1] is null)
                    entry.Add(k);
                else if (kv[1] is string v)
                    entry.Add(k, v);
                else if (kv[1] is int i)
                    entry.Add(k, i);
                else
                    throw new ArgumentException($"Unknown value type for key {k}: {kv[1]}");
            }
            else
            {
                throw new ArgumentException($"Keys must be strings: {kv[0]}");
            }
        }

        return entry;
    }

    [TestMethod]
    public void TestExistence()
    {
        var entry = MakeEntry([["key", null], ["str", "str"], ["int", 10]]);
        Assert.IsTrue(Filter.Has("key").Keep(entry));
        Assert.IsTrue(Filter.Has("str").Keep(entry));
        Assert.IsTrue(Filter.Has("int").Keep(entry));
        Assert.IsFalse(Filter.Has("other").Keep(entry));
    }

    [TestMethod]
    public void TestHasValue()
    {
        var entry = MakeEntry([["key", "value"], ["key", "value 2"], ["number", 1]]);
        Assert.IsTrue(Filter.Has("key", "value").Keep(entry));
        Assert.IsTrue(Filter.Has("key", "value 2").Keep(entry));
        Assert.IsTrue(Filter.Has("number", 1).Keep(entry));
        Assert.IsFalse(Filter.Has("key", "not-value").Keep(entry));
        Assert.IsFalse(Filter.Has("number", 2).Keep(entry));
        Assert.IsFalse(Filter.Has("other", "value").Keep(entry));
    }

    [TestMethod]
    public void TestLogic()
    {
        var entry = MakeEntry([["alpha"], ["omega"]]);

        Assert.IsTrue(Filter.Not(Filter.Has("epsilon")).Keep(entry));
        Assert.IsFalse(Filter.Not(Filter.Has("alpha")).Keep(entry));

        Assert.IsTrue(Filter.And([Filter.Has("alpha"),
                                  Filter.Has("omega")]).Keep(entry));
        Assert.IsFalse(Filter.And([Filter.Has("epsilon"),
                                   Filter.Has("omega")]).Keep(entry));
        Assert.IsFalse(Filter.And([Filter.Has("epsilon"),
                                   Filter.Has("gamma")]).Keep(entry));
        Assert.IsTrue(Filter.And([]).Keep(entry));

        Assert.IsTrue(Filter.Or([Filter.Has("alpha"),
                                 Filter.Has("omega")]).Keep(entry));
        Assert.IsTrue(Filter.Or([Filter.Has("alpha"),
                                 Filter.Has("epsilon")]).Keep(entry));
        Assert.IsFalse(Filter.Or([Filter.Has("epsilon"),
                                  Filter.Has("gamma")]).Keep(entry));
        Assert.IsFalse(Filter.Or([]).Keep(entry));
    }
}
