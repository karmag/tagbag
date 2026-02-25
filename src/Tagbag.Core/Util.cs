using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Tagbag.Core;

public static class TagbagUtil
{
    private static HashSet<string> KnownFileExtensions =
        new HashSet<string>([".jpg", ".jpeg", ".png", ".bmp", ".gif"]);

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

    // Returns the relative (and normalized) path for the entry. Will
    // throw if the entry path given is not a sub-path to the tagbag.
    public static string GetEntryPath(Tagbag tb, string absolutePath)
    {
        var tbRootPath = GetRootDirectory(tb);
        var relativePath = Path.GetRelativePath(tbRootPath, absolutePath);
        if (relativePath == absolutePath)
        {
            throw new ArgumentException($"{absolutePath} is not a sub-path of {tbRootPath}");
        }
        return NormalizePath(relativePath);
    }

    public static string NormalizePath(string path)
    {
        return path.Replace("\\", "/");
    }

    public static bool PopulateImageTags(Tagbag tb, Entry entry)
    {
        try
        {
            #pragma warning disable CA1416
            var img = System.Drawing.Image.FromFile(GetPath(tb, entry.Path));
            entry.Set(Const.Width, img.Width);
            entry.Set(Const.Height, img.Height);
            img.Dispose();
            #pragma warning restore CA1416
            return true;
        }
        catch (OutOfMemoryException)
        {
            // Thrown for files that are not images
            return false;
        }
    }

    public static bool PopulateFileTags(Tagbag tb, Entry entry)
    {
        var path = GetPath(tb, entry.Path);
        entry.Set(Const.Size, (int)new FileInfo(path).Length);
        entry.Set(Const.Hash, GetFileHash(path));
        return true;
    }

    public static string GetFileHash(string path)
    {
        using (SHA256 alg = SHA256.Create())
        {
            using (FileStream stream = new FileInfo(path).OpenRead())
            {
                var hash = alg.ComputeHash(stream);
                var hashString = "";
                foreach (byte b in hash)
                    hashString += b.ToString("x2");
                return hashString;
            }
        }
    }

    public static bool IsKnownFileExtension(string path)
    {
        return KnownFileExtensions.Contains(Path.GetExtension(path).ToLower());
    }

    public static void MoveToTrash(Tagbag tb, Entry entry)
    {
        var path = GetPath(tb, entry.Path);
        if (File.Exists(path))
            FileSystem.DeleteFile(path,
                                  UIOption.OnlyErrorDialogs,
                                  RecycleOption.SendToRecycleBin);
    }
}
