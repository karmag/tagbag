using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tagbag.Tests;

namespace Tagbag.Core.Tests;

[TestClass]
public class TestFilter
{

    [TestMethod]
    public void TestExistence()
    {
        var entry = Tester.Entry([["key", null], ["str", "str"], ["int", 10]]);
        Assert.IsTrue(Filter.Has("key").Keep(entry));
        Assert.IsTrue(Filter.Has("str").Keep(entry));
        Assert.IsTrue(Filter.Has("int").Keep(entry));
        Assert.IsFalse(Filter.Has("other").Keep(entry));
    }

    [TestMethod]
    public void TestHasValue()
    {
        var entry = Tester.Entry([["key", "value"], ["key", "value 2"], ["number", 1]]);
        Assert.IsTrue(Filter.Has("key", "value").Keep(entry));
        Assert.IsTrue(Filter.Has("key", "value 2").Keep(entry));
        Assert.IsTrue(Filter.Has("number", 1).Keep(entry));
        Assert.IsFalse(Filter.Has("key", "not-value").Keep(entry));
        Assert.IsFalse(Filter.Has("number", 2).Keep(entry));
        Assert.IsFalse(Filter.Has("other", "value").Keep(entry));

        entry = new Entry("path/to/image");
        Assert.IsTrue(Filter.Has("path", "path/to/image").Keep(entry));
    }

    [TestMethod]
    public void TestLogic()
    {
        var entry = Tester.Entry([["alpha"], ["omega"]]);

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

    [TestMethod]
    public void TestRegex()
    {
        var entry = Tester.Entry([["start", "abcandmore"],
                                  ["end", "stuffandabc"],
                                  ["case", "HELLOABC!"]]);

        Assert.IsTrue(Filter.Regex("start", "abc").Keep(entry));
        Assert.IsTrue(Filter.Regex("end", "abc").Keep(entry));
        Assert.IsTrue(Filter.Regex("case", "abc").Keep(entry));
        Assert.IsTrue(Filter.Regex("start", "^abc").Keep(entry));
        Assert.IsFalse(Filter.Regex("end", "^abc").Keep(entry));

        entry = new Entry("path/to/image");
        Assert.IsTrue(Filter.Regex("path", "/to/").Keep(entry));
    }

    [TestMethod]
    public void TestMath()
    {
        var entry = Tester.Entry([["a", 10]]);

        Assert.IsTrue(Filter.Math("a", "<", 11).Keep(entry));
        Assert.IsFalse(Filter.Math("a", "<", 10).Keep(entry));

        Assert.IsTrue(Filter.Math("a", ">", 9).Keep(entry));
        Assert.IsFalse(Filter.Math("a", ">", 10).Keep(entry));

        Assert.IsFalse(Filter.Math("a", "<=", 9).Keep(entry));
        Assert.IsTrue(Filter.Math("a", "<=", 10).Keep(entry));
        Assert.IsTrue(Filter.Math("a", "<=", 11).Keep(entry));

        Assert.IsTrue(Filter.Math("a", ">=", 9).Keep(entry));
        Assert.IsTrue(Filter.Math("a", ">=", 10).Keep(entry));
        Assert.IsFalse(Filter.Math("a", ">=", 11).Keep(entry));
    }
}
