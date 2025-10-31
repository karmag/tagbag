using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tagbag.Core;
using Tagbag.Tests;

[TestClass]
public class TestTagOperation
{
    [TestMethod]
    public void AddTags()
    {
        var entry = new Entry("a");

        TagOperation.Add("tag").Apply(entry);
        Assert.IsNotNull(entry.Get("tag"));

        TagOperation.Add("str-tag", "str").Apply(entry);
        Assert.IsTrue(entry.GetStrings("str-tag")?.SetEquals(["str"]));

        TagOperation.Add("int-tag", 10).Apply(entry);
        Assert.IsTrue(entry.GetInts("int-tag")?.SetEquals([10]));
    }

    [TestMethod]
    public void RemoveTags()
    {
        var entry = Tester.Entry([["str-tag", "str1"],
                                  ["str-tag", "str2"],
                                  ["int-tag", 10],
                                  ["tag"],
                                  ]);

        TagOperation.Remove("tag").Apply(entry);
        Assert.IsNull(entry.Get("tag"));

        TagOperation.Remove("str-tag", "str1").Apply(entry);
        Assert.IsTrue(entry.GetStrings("str-tag")?.SetEquals(["str2"]));

        TagOperation.Remove("int-tag", 10).Apply(entry);
        Assert.IsNull(entry.GetInts("int-tag"));
    }

    [TestMethod]
    public void SetTags()
    {
        var entry = Tester.Entry([["str-tag", "str1"],
                                  ["str-tag", "str2"],
                                  ["int-tag", 10],
                                  ["tag"],
                                  ]);

        TagOperation.Set("str-tag", "new-str").Apply(entry);
        Assert.IsTrue(entry.GetStrings("str-tag")?.SetEquals(["new-str"]));
    }

    [TestMethod]
    public void CombineTags()
    {
        var entry = new Entry("a");

        TagOperation.Combine(
            TagOperation.Add("tag", "str"),
            TagOperation.Remove("tag"),
            TagOperation.Add("tag", "new-str")).Apply(entry);

        Assert.IsTrue(entry.GetStrings("tag")?.SetEquals(["new-str"]));
    }
}
