using System;
using System.IO;

namespace Tagbag.Core;

public class TagbagUtil
{
    public static string GetRootDirectory(Tagbag tb)
    {
        var path = Path.GetDirectoryName(tb.Path);
        if (path == null)
            throw new InvalidOperationException("Can't resolve tagbag directory path");
        return path;
    }

    public static string GetPath(Tagbag tb, string? path)
    {
        var tbRootPath = GetRootDirectory(tb);

        var result = Path.GetFullPath(Path.Join(tbRootPath, path));

        if (!result.StartsWith(tbRootPath))
        {
            throw new ArgumentException($"\"{path}\" is not located under \"{tbRootPath}\"");
        }

        return result;
    }

    public static void PopulateImageTags(Tagbag tb, Entry entry)
    {
        try
        {
            #pragma warning disable CA1416
            var img = System.Drawing.Image.FromFile(GetPath(tb, entry.Path));
            entry.Set(Const.Width, img.Width);
            entry.Set(Const.Height, img.Height);
            img.Dispose();
            #pragma warning restore CA1416
        }
        catch (OutOfMemoryException)
        {
            // Thrown for files that are not images
        }
    }

    public static void PopulateFileTags(Tagbag tb, Entry entry)
    {
        var info = new FileInfo(GetPath(tb, entry.Path));
        entry.Set(Const.Size, (int)info.Length);
    }
}
