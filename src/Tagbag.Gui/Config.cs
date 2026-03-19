using System.Collections.Generic;
using Tagbag.Core;

namespace Tagbag.Gui;

public class Config
{
    public ImageConfig Image;
    public CacheConfig Cache;

    public Config()
    {
        Image = new ImageConfig();
        Cache = new CacheConfig();
    }

    public IEnumerable<ConfigValue<int>> GetValues()
    {
        var list = new List<ConfigValue<int>>();
        list.AddRange(Image.GetValues());
        list.AddRange(Cache.GetValues());
        return list;
    }

    public void Load()
    {
        ConfigFile.Load(GetValues());
    }

    public void Save()
    {
        ConfigFile.Save(GetValues());
    }
}

public class ImageConfig
{
    public ConfigValue<int> Rows;
    public ConfigValue<int> ThumbnailWidth;
    public ConfigValue<int> ThumbnailHeight;

    public ImageConfig()
    {
        Rows = new ConfigValue<int>(
            "Rows", 3,
            "Number of rows of thumbnails",
            ConfigValueContraint.Range(1, 10));

        ThumbnailWidth = new ConfigValue<int>(
            "ThumbnailWidth", 300,
            "Width of thumbnail images",
            ConfigValueContraint.Range(1, 10000));

        ThumbnailHeight = new ConfigValue<int>(
            "ThumbnailHeight", 300,
            "Height of thumbnail images",
            ConfigValueContraint.Range(1, 10000));
    }

    public IEnumerable<ConfigValue<int>> GetValues()
    {
        return [Rows, ThumbnailWidth, ThumbnailHeight];
    }
}

public class CacheConfig
{
    public ConfigValue<int> MaxImages;
    public ConfigValue<int> MaxThumbnails;

    public CacheConfig()
    {
        MaxImages = new ConfigValue<int>(
            "MaxImages", 20,
            "Max number of full images cached",
            ConfigValueContraint.Range(1, 100));

        MaxThumbnails = new ConfigValue<int>(
            "MaxThumbnails", 200,
            "Max number of thumbnails cached",
            ConfigValueContraint.Range(10, 1000));
    }

    public IEnumerable<ConfigValue<int>> GetValues()
    {
        return [MaxImages, MaxThumbnails];
    }
}
