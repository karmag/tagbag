using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Tagbag.Tests;

namespace Tagbag.Core.Tests;

[TestClass]
public class TestDuplicationDetection
{
    [TestMethod]
    public void TestDuplicateDetection()
    {
        var tt = new TagbagTestHelper();
        var Img = TagbagTestHelper.MakeImage;

        // Same image, different dimensions
        var a1 = tt.Add(new Item()
                        .SetPath("alpha.png")
                        .WithTags(["alpha", 1])
                        .WithGen(() => { return Img(50, 100, Color.Azure); }));
        var a2 = tt.Add(new Item()
                        .SetPath("alpha-longer-name.png")
                        .WithTags(["alpha", 2])
                        .WithGen(() => { return Img(100, 50, Color.Azure); }));

        // Slightly different images
        var o1 = tt.Add(new Item()
                        .SetPath("omega-1.png")
                        .WithTags(["omega", 10])
                        .WithGen(() => { return Img(10, 10, Color.FromArgb(255, 0, 0)); }));
        var o2 = tt.Add(new Item()
                        .SetPath("omega-2.png")
                        .WithTags(["omega", 20])
                        .WithGen(() => { return Img(50, 50, Color.FromArgb(250, 0, 0)); }));
        var o3 = tt.Add(new Item()
                        .SetPath("omega-3.png")
                        .WithTags(["omega", 30])
                        .WithGen(() => { return Img(100, 100, Color.FromArgb(245, 0, 0)); }));

        // Random other images that shouldn't be disturbed
        var other = new List<Guid?>();
        foreach (var color in new Color[]{Color.FromArgb(0, 50, 0),
                                          Color.FromArgb(0, 100, 0),
                                          Color.FromArgb(0, 250, 0)})
            other.Add(tt.Add(new Item()
                             .WithGen(() => { return Img(50, 50, color); })));

        foreach (var id in new Guid?[] { a1, a2, o1, o2, o3 })
            Assert.IsNotNull(id);

        var tb = tt.Get();

        // Run duplication detection

        var dd = new DuplicationDetection(tb);
        Assert.IsNotNull(dd.PopulateHashes());
        dd.Await();
        Assert.IsNotNull(dd.FindSimilarHashes(0.1f));
        dd.Await();
        Assert.IsNotNull(dd.DeleteDuplicates());
        dd.Await();

        // Assert entry existance

        Assert.IsNull(tb.Get(a1 ?? Guid.Empty));
        Assert.IsNotNull(tb.Get(a2 ?? Guid.Empty));

        Assert.IsNull(tb.Get(o1 ?? Guid.Empty));
        Assert.IsNull(tb.Get(o2 ?? Guid.Empty));
        Assert.IsNotNull(tb.Get(o3 ?? Guid.Empty));

        foreach (var id in other)
            Assert.IsNotNull(tb.Get(id ?? Guid.Empty));

        // Assert file existance

        var getPath = (Guid? id) =>
            {
                var path = tb.Get(id ?? Guid.Empty)?.Path;
                if (path == null || path.Length == 0)
                    return null;
                return TagbagUtil.GetPath(tb, path);
            };

        foreach (var id in new Guid?[] { a1, o1, o2 })
            Assert.IsFalse(File.Exists(getPath(id)));

        foreach (var id in new Guid?[] { a2, o3 })
            Assert.IsTrue(File.Exists(getPath(id)));

        foreach (var id in other)
            Assert.IsTrue(File.Exists(getPath(id)));

        // Assert tags - Non-builtin tags from deleted entries should
        // be added to the remaining entry.

        var countVals = (Guid? id, string tag) =>
            {
                if (tb.Get(id ?? Guid.Empty) is Entry entry && entry.Get(tag) is Value val)
                    return val.GetInts()?.Count ?? 0 + val.GetStrings()?.Count ?? 0;

                return -1;
            };

        foreach (var id in new Guid?[] { a2, o3 })
        {
            Assert.AreEqual(1, countVals(id, Const.Width));
            Assert.AreEqual(1, countVals(id, Const.Height));
            Assert.AreEqual(1, countVals(id, Const.Size));
            Assert.AreEqual(1, countVals(id, Const.Hash));
            Assert.AreEqual(1, countVals(id, Const.ColorHash));
        }

        Assert.IsTrue(tb.Get(a2 ?? Guid.Empty)?.GetInts("alpha")?.SetEquals([1, 2]));
        Assert.IsTrue(tb.Get(o3 ?? Guid.Empty)?.GetInts("omega")?.SetEquals([10, 20, 30]));
    }

    [TestMethod]
    public void TestColorHash()
    {
        var Img = TagbagTestHelper.MakeImageWeighted;

        var tt = new TagbagTestHelper();
        var black = tt.Add(new Item().WithGen(() => Img(50, 50, (1, Color.Black))));
        var white = tt.Add(new Item().WithGen(() => Img(50, 50, (1, Color.White))));
        var red = tt.Add(new Item()
                         .WithGen(() => Img(50, 50,
                                            (1, Color.FromArgb(0, 0, 0)),
                                            (2, Color.FromArgb(64, 0, 0)),
                                            (4, Color.FromArgb(128, 0, 0)),
                                            (8, Color.FromArgb(192, 0, 0)))));

        var dd = new DuplicationDetection(tt.Get());
        dd.PopulateHashes();
        dd.Await();

        var getBands = (Guid? id) =>
            {
                var entry = tt.Get().Get(id ?? Guid.Empty);
                Assert.IsNotNull(entry);
                return DuplicationDetection.DebugGetBands(entry);
            };

        // Verify black

        var rgb = getBands(black);
        CollectionAssert.AreEqual(rgb.Item1, new int[] { 0xffff, 0, 0, 0 });
        CollectionAssert.AreEqual(rgb.Item2, new int[] { 0xffff, 0, 0, 0 });
        CollectionAssert.AreEqual(rgb.Item3, new int[] { 0xffff, 0, 0, 0 });

        // Verify white

        rgb = getBands(white);
        CollectionAssert.AreEqual(rgb.Item1, new int[] { 0, 0, 0, 0xffff });
        CollectionAssert.AreEqual(rgb.Item2, new int[] { 0, 0, 0, 0xffff });
        CollectionAssert.AreEqual(rgb.Item3, new int[] { 0, 0, 0, 0xffff });

        // Verify red band distribution

        rgb = getBands(red);
        Assert.AreEqual(0xffff / 15, rgb.Item1[0]);
        Assert.AreEqual(0xffff / 15 * 2, rgb.Item1[1]);
        Assert.AreEqual(0xffff / 15 * 4, rgb.Item1[2]);
        Assert.AreEqual(0xffff / 15 * 8, rgb.Item1[3]);

        // Verify distances

        var getDistance = (Guid? a, Guid? b) =>
            {
                if (tt.Get().Get(a ?? Guid.Empty) is Entry aa)
                    if (tt.Get().Get(b ?? Guid.Empty) is Entry bb)
                        return DuplicationDetection.DebugGetDistance(aa, bb);
                throw new ArgumentException("Wrong args");
            };

        Assert.AreEqual(0xffff * 3 * 2,
                        getDistance(black, white));
        Assert.AreEqual((0xffff - 0xffff/15) + 0xffff/15*14,
                        getDistance(black, red));
        Assert.AreEqual(0xffff * 4 + 0xffff/15*7 + (0xffff - 0xffff/15*8),
                        getDistance(white, red));
    }

    [TestMethod]
    public void TestHashing()
    {
        var Img = TagbagTestHelper.MakeImageWeighted;

        var tt = new TagbagTestHelper();
        var black = tt.Add(new Item().WithGen(() => Img(50, 50, (1, Color.Black))));
        var white = tt.Add(new Item().WithGen(() => Img(50, 50, (1, Color.White))));
        var mix = tt.Add(new Item()
                         .WithGen(() => Img(50, 50,
                                            (1, Color.FromArgb(0, 192, 34)),
                                            (2, Color.FromArgb(64, 128, 45)),
                                            (4, Color.FromArgb(128, 64, 111)),
                                            (8, Color.FromArgb(192, 0, 4)))));

        var withPixelFormat = (Bitmap image, PixelFormat format) =>
        {
            var newImage = new Bitmap(image.Width, image.Height, format);
            using (var g = Graphics.FromImage(newImage))
                g.DrawImage(image, 0, 0, image.Width, image.Height);
            return newImage;
        };

        foreach (var id in new Guid?[]{black, white, mix})
        {
            Entry entry = tt.Get().Get(id ?? Guid.Empty) ?? throw new Exception("");
            using (var image = new Bitmap(TagbagUtil.GetPath(tt.Get(), entry.Path)))
            {
                var basic = DuplicationDetection.MakeColorBandHistogramSimple(image);

                foreach (var format in new PixelFormat[] {PixelFormat.Format24bppRgb,
                                                          PixelFormat.Format32bppArgb})
                {
                    using (var imageWithFormat = withPixelFormat(image, format))
                    {
                        var simple = DuplicationDetection.MakeColorBandHistogramSimple(imageWithFormat);
                        Assert.AreEqual(String.Join(", ", basic),
                                        String.Join(", ", simple));

                        var fast = DuplicationDetection.MakeColorBandHistogramFast(imageWithFormat);
                        Assert.IsNotNull(fast, $"Fast should handle {format}");
                        Assert.AreEqual(String.Join(", ", basic),
                                        String.Join(", ", fast));
                    }
                }
            }
        }
    }
}
