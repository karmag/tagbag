using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace Tagbag.Core.Tests;

[TestClass]
public class TestConfigFile
{
    [TestMethod]
    public void TestSaveLoad()
    {
        var one = GenValues();
        one[0].SetRaw(10);
        one[2].SetRaw("hello");

        var path = Path.GetTempFileName();
        ConfigFile.Save(path, one);

        var two = GenValues();
        ConfigFile.Load(path, two);

        Assert.AreEqual(10, two[0].GetRaw());
        Assert.AreEqual("hello", two[2].GetRaw());

        for (int i = 0; i < one.Count; i++)
        {
            var a = one[i];
            var b = two[i];

            Assert.AreEqual(a.GetRaw(), b.GetRaw());
            Assert.AreEqual(a.IsDefault(), b.IsDefault());
        }
    }

    private List<ConfigValue> GenValues()
    {
        return [
            new ConfigValue<int>("Alpha", 3, ConfigValue.IntParse, "Desc"),
            new ConfigValue<int>("Beta", 5600, ConfigValue.IntParse, "Desc"),
            new ConfigValue<string>("Gamma", "abc", ConfigValue.StringParse, "Desc"),
            new ConfigValue<string>("Delta", "123", ConfigValue.StringParse, "Desc"),
        ];
    }
}
