using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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
        if (chk.GetProblems().First?.Value is NonIndexedFile problem)
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
        if (chk.GetProblems().First?.Value is MissingDefaultTags problem)
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
        if (chk.GetProblems().First?.Value is FileMissing problem)
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
}
