using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using Tagbag.Tests;

namespace Tagbag.Core.Tests;

[TestClass]
public class TestCheck
{
    [TestMethod]
    public void FindNonIndexedFiles()
    {
        var tt = new TagbagTestHelper();
        tt.Add(new Item());
        tt.Add(new Item().NoAdd().SetPath("a.png"));

        var chk = new Check(tt.Get());
        chk.Scan();
        chk.Await();
        Assert.HasCount(1, chk.GetProblems());
        Assert.HasCount(1, tt.Get().GetEntries());

        string? path = null;
        if (chk.GetProblems()[0] is NonIndexedFile problem)
        {
            Assert.HasCount(1, problem.GetFiles());
            foreach (var s in problem.GetFiles())
                path = TagbagUtil.GetEntryPath(tt.Get(), s);
        }
        Assert.AreEqual("a.png", path);

        chk.Fix();
        chk.Await();
        Assert.HasCount(2, tt.Get().GetEntries());

        chk = new Check(tt.Get());
        chk.Scan();
        chk.Await();
        Assert.IsEmpty(chk.GetProblems());
    }

    [TestMethod]
    public void FindMissingDefaultTags()
    {
        var tt = new TagbagTestHelper();
        tt.Add(new Item());
        var id = tt.Add(new Item().NoTags());

        var chk = new Check(tt.Get());
        chk.Scan();
        chk.Await();
        Assert.HasCount(1, chk.GetProblems());
        Assert.HasCount(2, tt.Get().GetEntries());

        Entry? entry = null;
        if (chk.GetProblems()[0] is MissingDefaultTags problem)
        {
            Assert.HasCount(1, problem.GetEntries());
            foreach (var e in problem.GetEntries())
                entry = e;
        }
        Assert.AreEqual(id, entry?.Id);

        chk.Fix();
        chk.Await();

        chk = new Check(tt.Get());
        chk.Scan();
        chk.Await();
        Assert.IsEmpty(chk.GetProblems());
    }

    [TestMethod]
    public void FindFileMoved()
    {
        var tt = new TagbagTestHelper();
        tt.Add(new Item());
        tt.Add(new Item().SetPath("a.png"));

        File.Move(TagbagUtil.GetPath(tt.Get(), "a.png"),
                  TagbagUtil.GetPath(tt.Get(), "renamed.png"));

        var chk = new Check(tt.Get());
        chk.Scan();
        chk.Await();
        Assert.HasCount(1, chk.GetProblems());
        Assert.HasCount(2, tt.Get().GetEntries());


        FileMoved? fileMoved = null;
        string? newPath = null;
        foreach (var problem in chk.GetProblems())
            if (problem is FileMoved)
            {
                fileMoved = problem as FileMoved;
                Assert.HasCount(1, fileMoved?.GetFiles() ?? []);
                foreach (var s in fileMoved?.GetFiles() ?? [])
                    newPath = s;
            }

        Assert.AreEqual(newPath, TagbagUtil.GetPath(tt.Get(), "renamed.png"));

        chk.Fix();
        chk.Await();

        chk = new Check(tt.Get());
        chk.Scan();
        chk.Await();
        Assert.IsEmpty(chk.GetProblems());
    }

    [TestMethod]
    public void FindFileMissing()
    {
        var tt = new TagbagTestHelper();
        tt.Add(new Item());
        var expectedId = tt.Add(new Item().SetPath("delete_me.png"));

        File.Delete(TagbagUtil.GetPath(tt.Get(), "delete_me.png"));

        var chk = new Check(tt.Get());
        chk.Scan();
        chk.Await();
        Assert.HasCount(1, chk.GetProblems());
        Assert.HasCount(2, tt.Get().GetEntries());

        Guid? id = null;
        if (chk.GetProblems()[0] is FileMissing problem)
        {
            Assert.HasCount(1, problem.GetEntries());
            foreach (var e in problem.GetEntries())
                id = e.Id;
        }
        Assert.AreEqual(expectedId, id);

        chk.Fix();
        chk.Await();
        Assert.HasCount(1, tt.Get().GetEntries());

        chk = new Check(tt.Get());
        chk.Scan();
        chk.Await();
        Assert.IsEmpty(chk.GetProblems());
    }

    [TestMethod]
    public void DuplicateFiles()
    {
        var tt = new TagbagTestHelper();
        var alphaId = tt.Add(new Item().SetPath("alpha.png").Seed(1));
        var gammaId = tt.Add(new Item().SetPath("gamma.png").Seed(1));
        var omegaId = tt.Add(new Item().SetPath("omega_more.png").Seed(1));
        var otherId = tt.Add(new Item());

        var alpha = tt.Get().Get(alphaId ?? Guid.Empty);
        var gamma = tt.Get().Get(gammaId ?? Guid.Empty);
        var omega = tt.Get().Get(omegaId ?? Guid.Empty);
        var other = tt.Get().Get(otherId ?? Guid.Empty);

        var key = "key";

        alpha?.Add(key, "alpha");
        gamma?.Add(key, "gamma");
        omega?.Add(key, "omega");
        other?.Add(key, "other");

        var chk = new Check(tt.Get());
        chk.Scan();
        chk.Await();
        Assert.HasCount(1, chk.GetProblems());
        Assert.HasCount(4, tt.Get().GetEntries());

        var prob = (DuplicateFiles)chk.GetProblems()[0];
        Assert.HasCount(3, prob.GetEntries());

        CollectionAssert.AreEquivalent(
            new List<Entry>(prob.GetEntries()),
            new List<Entry?>([alpha, gamma, omega]));

        chk.Fix();
        chk.Await();
        Assert.HasCount(2, tt.Get().GetEntries());

        Assert.IsNotNull(tt.Get().Get(omegaId ?? Guid.Empty));
        Assert.IsNotNull(tt.Get().Get(otherId ?? Guid.Empty));
        Assert.IsNull(tt.Get().Get(alphaId ?? Guid.Empty));
        Assert.IsNull(tt.Get().Get(gammaId ?? Guid.Empty));

        Assert.IsTrue(File.Exists(TagbagUtil.GetPath(tt.Get(), omega?.Path)));
        Assert.IsTrue(File.Exists(TagbagUtil.GetPath(tt.Get(), other?.Path)));
        Assert.IsFalse(File.Exists(TagbagUtil.GetPath(tt.Get(), alpha?.Path)));
        Assert.IsFalse(File.Exists(TagbagUtil.GetPath(tt.Get(), gamma?.Path)));

        CollectionAssert.AreEquivalent(
            new List<string>(omega?.GetStrings(key) ?? []),
            new List<string>(["alpha", "gamma", "omega"]));
    }
}
