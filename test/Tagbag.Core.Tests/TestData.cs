[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]

namespace Tagbag.Core.Tests;

[TestClass]
public class TestTagbag
{
    [TestMethod]
    public void NewOpenSaveTagbag()
    {
        var tempDir = Directory.CreateTempSubdirectory("tagbag_test_").FullName;
        List<Entry> entries = [new Entry("a"), new Entry("b"), new Entry("c")];
        entries[0].Add("tag");
        entries[1].Add("str", "text");
        entries[2].Add("num", 123);

        var tb = Tagbag.New(tempDir);
        foreach (var entry in entries)
            tb.Add(entry);
        tb.Save();

        var tb2 = Tagbag.Open(tempDir);

        foreach (var entry in entries)
        {
            var a = tb.Get(entry.Id);
            var b = tb2.Get(entry.Id);

            Assert.IsNotNull(a);
            Assert.IsNotNull(b);

            Assert.AreEqual(a.Id, b.Id);
            Assert.AreEqual(a.Path, b.Path);

            Assert.HasCount(1, a.GetAllTags());
            Assert.HasCount(1, b.GetAllTags());

            foreach (var tag in a.GetAllTags())
            {
                var aval = a.Get(tag);
                var bval = b.Get(tag);

                Assert.IsNotNull(aval);
                Assert.IsNotNull(bval);

                Assert.AreEqual(aval.IsTag(), bval.IsTag());

                var aStr = aval.GetStrings();
                var bStr = bval.GetStrings();
                if (aStr == null || bStr == null)
                {
                    Assert.IsNull(aStr);
                    Assert.IsNull(bStr);
                }
                else
                    Assert.IsTrue(aStr.SetEquals(bStr));

                var aInt = aval.GetInts();
                var bInt = bval.GetInts();
                if (aInt == null || bInt == null)
                {
                    Assert.IsNull(aInt);
                    Assert.IsNull(bInt);
                }
                else
                    Assert.IsTrue(aInt.SetEquals(bInt));
            }
        }
    }
}

[TestClass]
public class TestEntry
{
    [TestMethod]
    public void GetSetRemoveTags()
    {
        var entry = new Entry("x");

        // Tag

        entry.Add("simple");
        Assert.IsTrue(entry.Get("simple")?.IsTag());

        entry.Remove("simple");
        Assert.IsNull(entry.Get("simple"));

        // String

        entry.Add("name", "alpha");
        entry.Add("name", "omega");
        Assert.IsTrue(entry.GetStrings("name")?.SetEquals(["alpha", "omega"]));

        entry.Set("name", "gamma");
        Assert.IsTrue(entry.GetStrings("name")?.SetEquals(["gamma"]));

        entry.Remove("name", "gamma");
        Assert.IsNull(entry.GetStrings("name"));

        // Int

        entry.Add("count", 10);
        entry.Add("count", 10);
        entry.Add("count", 10);
        Assert.IsTrue(entry.GetInts("count")?.SetEquals([10]));

        entry.Remove("count", 10);
        Assert.IsNull(entry.GetInts("count"));
    }
}

[TestClass]
public class TestValue
{
    [TestMethod]
    public void IsSet()
    {
        IEnumerable<Action<Value>> functions = [
            v => v.SetTag(true),
            v => v.Add("tag"),
            v => v.Add(10),
        ];

        foreach (var f in functions)
        {
            var value = new Value();
            Assert.IsFalse(value.IsSet());
            f(value);
            Assert.IsTrue(value.IsSet());
        }
    }

    [TestMethod]
    public void SettingMultipleValues()
    {
        var value = new Value();
        value.Add("one");
        value.Add("two");
        value.Add("two");
        Assert.IsTrue(value.GetStrings()?.SetEquals(["one", "two"]));

        value.Remove("two");
        Assert.IsTrue(value.GetStrings()?.SetEquals(["one"]));

        value.Remove("one");
        Assert.IsNull(value.GetStrings());
    }
}
