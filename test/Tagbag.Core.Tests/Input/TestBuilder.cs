using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Tagbag.Core.Input;
using Tagbag.Tests;

namespace Tagbag.Core.Test.Input;

[TestClass]
public class TestBuilder
{
    [TestMethod]
    public void TestTagBuilder()
    {
        VerifyTagBuild([],
                       "alpha",
                       [["alpha"]]);

        VerifyTagBuild([],
                       "+alpha",
                       [["alpha"]]);

        VerifyTagBuild([["alpha"]],
                       "-alpha",
                       []);

        VerifyTagBuild([],
                       "alpha | beta",
                       [["alpha"], ["beta"]]);

        VerifyTagBuild([],
                       "alpha + 10 | beta + 20 | alpha + 30",
                       [["alpha", 10, 30], ["beta", 20]]);

        VerifyTagBuild([],
                       "tag value",
                       [["tag", "value"]]);

        VerifyTagBuild([["tag", "value"]],
                       "tag + other",
                       [["tag", "value", "other"]]);

        VerifyTagBuild([["tag", "alpha", "beta", "gamma"]],
                       "tag - alpha",
                       [["tag", "beta", "gamma"]]);

        VerifyTagBuild([["tag", "alpha", "beta", "gamma"]],
                       "tag = omega",
                       [["tag", "omega"]]);
    }

    private void VerifyTagBuild(Object?[][] startKvs,
                                string input,
                                Object?[][] expectedKvs)
    {
        var entry = Tester.Entry(startKvs);
        TagBuilder.Build(input).Apply(entry);
        Tester.AssertTagsMatch(entry, expectedKvs);
    }
}
