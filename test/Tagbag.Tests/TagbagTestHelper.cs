using System;
using System.Drawing;
using System.IO;
using Tagbag.Core;

namespace Tagbag.Tests;

// Helper class for building Tagbag objects with their corresponding
// image files on disk.
public class TagbagTestHelper
{
    private string _TempDir;
    private Tagbag.Core.Tagbag _Tagbag;
    private int _Counter;

    public TagbagTestHelper()
    {
        _TempDir = Directory.CreateTempSubdirectory("tagbag_test_setup_").FullName;
        _Tagbag = Tagbag.Core.Tagbag.New(_TempDir);
        _Counter = 100;
    }

    public Tagbag.Core.Tagbag Get() { return _Tagbag; }

    public Guid? Add(Item item)
    {
        if (item.Path.Length == 0)
            item.Path = $"image_{_Counter++}.png";

        if (item.Image)
        {
            Bitmap image;

            if (item.ImageGenerator is Func<Bitmap> gen)
                image = gen();
            else
            {
                Color color = Color.FromArgb(_Counter++ % 255,
                                             (_Counter++ * 7) % 255,
                                             (_Counter++ * 31) % 255);

                if (item.ImageSeed > 0)
                    color = Color.FromArgb((item.ImageSeed * 109) % 255,
                                           (item.ImageSeed * 463) % 255,
                                           (item.ImageSeed * 877) % 255);

                image = MakeImage(50, 50, color);
            }

            image.Save(TagbagUtil.GetPath(_Tagbag, item.Path));
        }

        if (item.Add)
        {
            var entry = new Entry(item.Path);

            if (item.PopulateTags)
            {
                TagbagUtil.PopulateFileTags(_Tagbag, entry);
                TagbagUtil.PopulateImageTags(_Tagbag, entry);
            }

            if (item.Tags is Object?[][] kvArgs)
                Tester.AddTags(entry, kvArgs);

            _Tagbag.Add(entry);
            return entry.Id;
        }

        return null;
    }

    public static Bitmap MakeImage(int w, int h, Color color)
    {
        var image = new Bitmap(w, h);
        using (Graphics g = Graphics.FromImage(image))
        {
            g.FillRectangle(new SolidBrush(color), 0, 0, w, h);
        }
        return image;
    }

    // Returns an image that is w * (h * totalWeight) large. Each
    // color is present to a degree related to its weight.
    public static Bitmap MakeImageWeighted(int w, int h, params (int, Color)[] colorWeights)
    {
        var totalWeight = 0;
        foreach (var cw in colorWeights)
            totalWeight += cw.Item1;

        var image = new Bitmap(w * totalWeight, h);
        using (Graphics g = Graphics.FromImage(image))
        {
            var counter = 0;
            foreach (var cw in colorWeights)
            {
                g.FillRectangle(new SolidBrush(cw.Item2),
                                counter * w, 0, w * cw.Item1, h);
                counter += cw.Item1;
            }
        }
        return image;
    }
}

public class Item
{
    public string Path = "";
    public bool Add = true;
    public bool Image = true;
    public bool PopulateTags = true;
    public int ImageSeed = -1;
    public Func<Bitmap>? ImageGenerator;
    public Object?[][]? Tags;

    public Item() { }

    // Explicitly set the relative path of the image.
    public Item SetPath(string path) { Path = path; return this; }

    // Don't add an entry to the Tagbag.
    public Item NoAdd() { Add = false; return this; }

    // Don't generate an image file.
    public Item NoImage() { Image = false; return this; }

    // Don't add common tags to the entry.
    public Item NoTags() { PopulateTags = false; return this; }

    // If set generates a repeatable image. The seed must be a
    // positive integer. Items with the same seed creates the same
    // image. If seed is not set a random image will be generated for
    // the entry.
    public Item Seed(int seed) { ImageSeed = seed; return this; }

    public Item WithGen(Func<Bitmap> gen) { ImageGenerator = gen; return this; }

    public Item WithTags(params Object?[][] kvArgs) { Tags = kvArgs; return this; }
}
