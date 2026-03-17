using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace Tagbag.Core;

public class DuplicationDetection
{
    public const int MaxThreshold = 0xffff * 6;

    private const int BucketCount = 4;
    private static string MatchTag = "duplicate";

    private Tagbag _Tagbag;

    public enum Activity {
        PopulateHashes, MatchHashes, OrganizeDuplicates, DeleteDuplicates,
    }

    // Progress report
    // - activity-type
    // - current progress
    // - goal value (is 0 to indicate unknown goal)
    public Action<Activity, int, int>? ProgressReport;
    private Activity _ProgressActivity;
    private int _ProgressCurrent;
    private int _ProgressGoal;

    private Task? _Task;
    private CancellationTokenSource? _CancelSource;

    public DuplicationDetection(Tagbag tb)
    {
        _Tagbag = tb;
        _ProgressActivity = Activity.PopulateHashes;
    }

    public Task? PopulateHashes()
    {
        return LaunchWorker(PopulateHashesWorker);
    }

    public Task? FindSimilarHashes(float matchDegree)
    {
        var threshold = (int)((float)(0xffff * 6) * matchDegree);
        return LaunchWorker((ct) => ApplyMatchTagsWorker(threshold, ct));
    }

    public Task? FindSimilarHashes(int threshold)
    {
        return LaunchWorker((ct) => ApplyMatchTagsWorker(threshold, ct));
    }

    public Task? DeleteDuplicates()
    {
        return LaunchWorker(MergeMatchedEntriesWorker);
    }

    public void Stop()
    {
        _CancelSource?.Cancel();
    }

    public void Await()
    {
        if (_Task is Task task)
            Task.WaitAll(task);
    }

    private Task? LaunchWorker(Action<CancellationToken> action)
    {
        lock (this)
        {
            if (_Task != null)
                return null;

            _CancelSource = new CancellationTokenSource();
            _Task = Task.Run(() => action(_CancelSource.Token));
            _Task.ContinueWith(WorkCleanup);

            return _Task;
        }
    }

    private void WorkCleanup(Task _)
    {
        lock (this)
        {
            if (_Task == null)
                return;

            _CancelSource?.Cancel();
            _CancelSource?.Dispose();
            _CancelSource = null;
            _Task = null;
        }
    }

    private void PopulateHashesWorker(CancellationToken token)
    {
        var queue = new ConcurrentQueue<Entry>(_Tagbag.GetEntries());
        var workers = new List<Task>();

        ResetProgressGoal(Activity.PopulateHashes, queue.Count);

        for (int i = 0; i < 10; i++)
            workers.Add(Task.Run(() => {
                Entry? entry;
                while (!token.IsCancellationRequested)
                    if (queue.TryDequeue(out entry))
                    {
                        AddColorHash(entry);
                        IncrementCurrentProgress();
                    }
                    else
                        break;
            }));

        Task.WaitAll(workers);
    }

    // Tag entries that are similar according to their color-hash.
    //
    // Threshold indicates the maximum difference allowed for
    // something to be considered a match. This goes from 0 (exact
    // match) to 393210 (everything).
    //
    // Only entries that already have a color-hash tag are considered.
    //
    // If entries have previous match values those are removed before
    // assigning new ones.
    private void ApplyMatchTagsWorker(int threshold, CancellationToken token)
    {
        ResetProgressGoal(Activity.MatchHashes, 0);

        var data = PrepareMatchData();
        if (token.IsCancellationRequested)
            return;

        var queue = new ConcurrentQueue<int>();
        var workers = new List<Task>();

        for (int i = 0; i < data.Count - 1; i++)
            queue.Enqueue(i);

        ResetProgressGoal(Activity.MatchHashes, queue.Count);

        for (int i = 0; i < 10; i++)
                workers.Add(Task.Run(() =>
                {
                    int index;
                    while (!token.IsCancellationRequested)
                        if (queue.TryDequeue(out index))
                        {
                            ApplyMatchDataTagging(data, index, threshold);
                            IncrementCurrentProgress();
                        }
                        else
                            break;
                }));

        Task.WaitAll(workers);
    }

    private void ResetProgressGoal(Activity activity, int goal)
    {
        lock (this)
        {
            _ProgressActivity = activity;
            _ProgressCurrent = 0;
            _ProgressGoal = goal;
            ProgressReport?.Invoke(_ProgressActivity, _ProgressCurrent, _ProgressGoal);
        }
    }

    private void IncrementCurrentProgress()
    {
        lock (this)
        {
            _ProgressCurrent++;
            ProgressReport?.Invoke(_ProgressActivity, _ProgressCurrent, _ProgressGoal);
        }
    }

    // Adds color hash to the entry. Will not modify the entry if
    // there's already data there. Returns true if the entry is
    // modified, false otherwise.
    public bool AddColorHash(Entry entry)
    {
        switch (entry.GetStrings(Const.ColorHash)?.Count)
        {
            case 0:
                break;
            case 1:
                return false;
            default:
                entry.Remove(Const.ColorHash);
                break;
        }

        entry.Add(Const.ColorHash,
                  MakeColorHash(TagbagUtil.GetPath(_Tagbag, entry.Path)));

        return true;
    }

    private List<(Entry, ComparisonData)> PrepareMatchData()
    {
        var data = new List<(Entry, ComparisonData)>();

        foreach (var entry in _Tagbag.GetEntries())
        {
            if (ComparisonData.Parse(entry) is ComparisonData cd)
                data.Add((entry, cd));

            if (entry.Get(MatchTag) != null)
                entry.Remove(MatchTag);
        }

        return data;
    }

    private void ApplyMatchDataTagging(List<(Entry, ComparisonData)> data,
                                       int startIndex,
                                       int threshold)
    {
        var primary = data[startIndex];
        for (int i = startIndex + 1; i < data.Count; i++)
            if (primary.Item2.GetAbsoluteDistance(data[i].Item2) <= threshold)
            {
                lock (primary.Item1)
                    primary.Item1.Add(MatchTag, startIndex);

                lock (data[i].Item1)
                    data[i].Item1.Add(MatchTag, startIndex);
            }
    }

    // Merges duplicate entries by deleting all but one of them.
    public void MergeMatchedEntriesWorker(CancellationToken token)
    {
        var matched = new Dictionary<int, List<Entry>>();

        ResetProgressGoal(Activity.OrganizeDuplicates, 0);

        foreach (var entry in _Tagbag.GetEntries())
            if (entry.Get(MatchTag) is Value val)
                foreach (var matchId in val.GetInts() ?? [])
                {
                    var list = matched.GetValueOrDefault(matchId);
                    if (list == null)
                    {
                        list = new List<Entry>();
                        matched[matchId] = list;
                    }
                    list.Add(entry);

                    IncrementCurrentProgress();
                }

        if (token.IsCancellationRequested)
            return;

        var keep = new HashSet<Entry>();
        var remove = new HashSet<Entry>();

        foreach (var list in matched.Values)
        {
            if (list.Count <= 1)
                continue;

            list.Sort(CompareEntries);
            var primary = list[0];
            keep.Add(primary);

            for (int i = 1; i < list.Count; i++)
            {
                remove.Add(list[i]);
                primary.CopyTagsFrom(list[i]);
            }

            IncrementCurrentProgress();
        }

        if (token.IsCancellationRequested)
            return;

        remove.ExceptWith(keep);

        ResetProgressGoal(Activity.DeleteDuplicates, remove.Count);

        foreach (var entry in remove)
        {
            _Tagbag.Remove(entry.Id);
            TagbagUtil.MoveToTrash(_Tagbag, entry);
            IncrementCurrentProgress();

            if (token.IsCancellationRequested)
                return;
        }
    }

    // Comparer for ordering entry by importance. Puts the most
    // important entry first.
    private static int CompareEntries(Entry a, Entry b)
    {
        // Highest resolution
        var aSize = a.GetIntBy(Const.Width, int.Max) * a.GetIntBy(Const.Height, int.Max);
        var bSize = b.GetIntBy(Const.Width, int.Max) * b.GetIntBy(Const.Height, int.Max);
        if (aSize != bSize)
            return bSize - aSize;

        // Biggest file size
        aSize = a.GetIntBy(Const.Size, int.Max);
        bSize = b.GetIntBy(Const.Size, int.Max);
        if (aSize != bSize)
            return bSize - aSize;

        // Deepest directory
        aSize = a.Path.Split("/").Length;
        bSize = b.Path.Split("/").Length;
        if (aSize != bSize)
            return bSize - aSize;

        // Longest path
        aSize = a.Path.Length;
        bSize = b.Path.Length;
        if (aSize != bSize)
            return bSize - aSize;

        return 0;
    }

    // Returns the color-hash for the given image file path.
    private static string MakeColorHash(string path)
    {
        int[] rgb;
        var pixelCount = 0;

        using (var image = new Bitmap(path))
        {
            pixelCount = image.Width * image.Height;

            var fast = MakeColorBandHistogramFast(image);
            if (fast != null)
                rgb = fast;
            else
            {
                System.Console.WriteLine($"[WARN] Slow hash for {path} {image.PixelFormat}");
                rgb = MakeColorBandHistogramSimple(image);
            }
        }

        // Normalize color band information so that the sum of all
        // buckets for a color lays in the range 0-65535. This
        // guarantees that each bucket can fit into two bytes.

        double multiplier = 1.0 / (double)pixelCount * 0xffff;
        for (int i = 0; i < rgb.Length; i++)
            rgb[i] = (int)((double)rgb[i] * multiplier);

        var bytes = new byte[2 * 3 * BucketCount];
        for (int i = 0; i < rgb.Length; i++)
        {
            bytes[i * 2] = (byte)(rgb[i] >> 8);
            bytes[i * 2 + 1] = (byte)(rgb[i] & 0xff);
        }

        return Convert.ToBase64String(bytes);
    }

    // Same as MakeColorBandHistogramSimple but processes pixel data
    // in bulk so performs much better.
    //
    // Returns null if unable to process the image due to
    // unimplemented pixel-format support.
    public static int[]? MakeColorBandHistogramFast(Bitmap image)
    {
        var rgb = new int[3 * BucketCount];

        int rOffset;
        int gOffset;
        int bOffset;
        int pixelSize;

        switch (image.PixelFormat)
        {
            case PixelFormat.Format24bppRgb:
                rOffset = 2;
                gOffset = 1;
                bOffset = 0;
                pixelSize = 3;
                break;
            case PixelFormat.Format32bppArgb:
                rOffset = 2;
                gOffset = 1;
                bOffset = 0;
                pixelSize = 4;
                break;
            default:
                return null;
        }

        var bitmapData = image.LockBits(
            new Rectangle(0, 0, image.Width, image.Height),
            ImageLockMode.ReadOnly,
            image.PixelFormat);

        try
        {
            if (bitmapData.Stride < 0)
                return null;

            byte[] raw = new byte[int.Abs(bitmapData.Stride) * bitmapData.Height];

            System.Runtime.InteropServices.Marshal.Copy(
                bitmapData.Scan0, raw, 0, raw.Length);

            const int bucketSize = 256 / BucketCount;
            int stridePadding = int.Abs(bitmapData.Stride) - bitmapData.Width * pixelSize;
            int index = 0;
            for (int y = 0; y < bitmapData.Height; y++)
            {
                for (int x = 0; x < bitmapData.Width; x++)
                {
                    rgb[raw[index + rOffset] / bucketSize]++;
                    rgb[raw[index + gOffset] / bucketSize + BucketCount]++;
                    rgb[raw[index + bOffset] / bucketSize + BucketCount * 2]++;
                    index += pixelSize;
                }
                index += stridePadding;
            }
        }
        finally
        {
            image.UnlockBits(bitmapData);
        }

        return rgb;
    }

    // Makes a histogram over color from the image. 4 buckets are used
    // per color band.
    //
    // This method is slow due to simplistic image handling but works
    // for any image. Prefer the fast version when possible.
    //
    // The returned array is structured as
    //          +-------------------------------------------------+
    //  index   | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 |
    //          +---------------+---------------+-----------------+
    //  color   | red           |  green        |  blue           |
    //          +-------------------------------------------------+
    public static int[] MakeColorBandHistogramSimple(Bitmap image)
    {
        var rgb = new int[3 * BucketCount];
        const int divider = 256 / BucketCount;

        for (int y = 0; y < image.Height; y++)
            for (int x = 0; x < image.Width; x++)
            {
                var color = image.GetPixel(x, y);
                rgb[color.R / divider]++;
                rgb[color.G / divider + BucketCount]++;
                rgb[color.B / divider + BucketCount * 2]++;
            }

        return rgb;
    }

    // Returns the normalized histogram for the RGB color bands, if
    // present in the entry.
    public static Tuple<int[], int[], int[]>? DebugGetBands(Entry entry)
    {
        if (ComparisonData.Parse(entry) is ComparisonData cd)
            return new Tuple<int[], int[], int[]>(cd.R, cd.G, cd.B);
        return null;
    }

    // Returns the distance between the entries or int.MaxValue if
    // color-hash data is not present.
    public static int DebugGetDistance(Entry a, Entry b)
    {
        if (ComparisonData.Parse(a) is ComparisonData cda)
            if (ComparisonData.Parse(b) is ComparisonData cdb)
                return cda.GetAbsoluteDistance(cdb);
        return int.MaxValue;
    }

    private class ComparisonData
    {
        public int[] R;
        public int[] G;
        public int[] B;

        private ComparisonData()
        {
            R = new int[BucketCount];
            G = new int[BucketCount];
            B = new int[BucketCount];
        }

        public static ComparisonData? Parse(Entry entry)
        {
            var strs = entry.GetStrings(Const.ColorHash);
            if (strs != null && strs.Count == 1)
            {
                foreach (var s in strs)
                    return Parse(s);
            }

            return null;
        }

        public static ComparisonData? Parse(string hash)
        {
            var bytes = new byte[2 * 3 * BucketCount];
            int size;
            if (Convert.TryFromBase64String(hash, bytes, out size) &&
                size == 2 * 3 * BucketCount)
            {
                var cd = new ComparisonData();
                var index = 0;
                foreach (var band in new[] { cd.R, cd.G, cd.B })
                    for (int bucket = 0; bucket < BucketCount; bucket++)
                    {
                        band[bucket] = (bytes[index*2] << 8) | bytes[index*2 + 1];
                        index++;
                    }

                return cd;
            }

            return null;
        }

        // Returns the distance between this and the other. Larger
        // number means further away, or less alike. A result of 0
        // means that the Data objects represent the same data.
        //
        // A result of 0 does not mean that the underlaying images are
        // the same.
        public int GetAbsoluteDistance(ComparisonData other)
        {
            var result = 0;

            for (int i = 0; i < BucketCount; i++)
            {
                result += int.Abs(R[i] - other.R[i]);
                result += int.Abs(G[i] - other.G[i]);
                result += int.Abs(B[i] - other.B[i]);
            }

            return result;
        }
    }
}
