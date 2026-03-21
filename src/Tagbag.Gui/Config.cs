using System;
using System.Collections.Generic;
using Tagbag.Core;

namespace Tagbag.Gui;

public class Config
{
    public UiConfig Ui;
    public ImageConfig Image;
    public CacheConfig Cache;

    public Config()
    {
        Ui = new UiConfig();
        Image = new ImageConfig();
        Cache = new CacheConfig();
    }

    public IEnumerable<ConfigValue> GetValues()
    {
        var list = new List<ConfigValue>();
        list.AddRange(Ui.GetValues());
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

public class UiConfig
{
    public ConfigValue<string> HideTags;
    public ConfigValue<string> HideSummaryTags;

    public UiConfig()
    {
        var tags = new List<string>(Const.BuiltinTags);
        tags.Sort();

        HideTags = new ConfigValue<string>(
            "HideTags", String.Join(" ", tags), ConfigValue.StringParse,
            "Tags to hide from the tag-table",
            ConfigValue.TokenizeConstraint);

        HideSummaryTags = new ConfigValue<string>(
            "HideSummaryTags", String.Join(" ", tags), ConfigValue.StringParse,
            "Tags to hide from the tag-summary-table",
            ConfigValue.TokenizeConstraint);
    }

    public IEnumerable<ConfigValue> GetValues()
    {
        return [HideTags, HideSummaryTags];
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
            "Rows", 3, ConfigValue.IntParse,
            "Number of rows of thumbnails",
            ConfigValue.RangeConstraint(1, 10));

        ThumbnailWidth = new ConfigValue<int>(
            "ThumbnailWidth", 300, ConfigValue.IntParse,
            "Width of thumbnail images",
            ConfigValue.RangeConstraint(1, 10000));

        ThumbnailHeight = new ConfigValue<int>(
            "ThumbnailHeight", 300, ConfigValue.IntParse,
            "Height of thumbnail images",
            ConfigValue.RangeConstraint(1, 10000));
    }

    public IEnumerable<ConfigValue> GetValues()
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
            "MaxImages", 20, ConfigValue.IntParse,
            "Max number of full images cached",
            ConfigValue.RangeConstraint(1, 100));

        MaxThumbnails = new ConfigValue<int>(
            "MaxThumbnails", 200, ConfigValue.IntParse,
            "Max number of thumbnails cached",
            ConfigValue.RangeConstraint(10, 1000));
    }

    public IEnumerable<ConfigValue> GetValues()
    {
        return [MaxImages, MaxThumbnails];
    }
}
