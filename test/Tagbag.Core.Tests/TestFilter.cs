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
}
