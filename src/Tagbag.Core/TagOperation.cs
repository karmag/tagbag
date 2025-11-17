using System;
using System.Collections.Generic;

namespace Tagbag.Core;

public interface ITagOperation
{
    public void Apply(Entry entry);
}

public static class TagOperation
{
    public static ITagOperation Add(string tag)                         { return new AddTag(tag, null, null); }
    public static ITagOperation Add(string tag, string value)           { return new AddTag(tag, value, null); }
    public static ITagOperation Add(string tag, int value)              { return new AddTag(tag, null, value); }
    public static ITagOperation Remove(string tag)                      { return new RemoveTag(tag, null, null); }
    public static ITagOperation Remove(string tag, string value)        { return new RemoveTag(tag, value, null); }
    public static ITagOperation Remove(string tag, int value)           { return new RemoveTag(tag, null, value); }
    public static ITagOperation Set(string tag)                         { return new SetTag(tag, null, null); }
    public static ITagOperation Set(string tag, string value)           { return new SetTag(tag, value, null); }
    public static ITagOperation Set(string tag, int value)              { return new SetTag(tag, null, value); }
    public static ITagOperation Combine(params ITagOperation[] ops)     { return new CombineOperations(ops); }
    public static ITagOperation Combine(IEnumerable<ITagOperation> ops) { return new CombineOperations(ops); }

    private class AddTag(string tag, string? str, int? i) : ITagOperation
    {
        public void Apply(Entry entry)
        {
            int count = 0;

            if (str is string strValue)
            {
                entry.Add(tag, strValue);
                count++;
            }

            if (i is int intValue)
            {
                entry.Add(tag, intValue);
                count++;
            }

            if (count == 0)
                entry.Add(tag);
        }

        override public string? ToString()
        {
            if (str is null && i is null)
                return $"+{tag}";
            return $"{tag} + {ValueToString(str, i)}";
        }
    }

    private class RemoveTag(string tag, string? str, int? i) : ITagOperation
    {
        public void Apply(Entry entry)
        {
            int count = 0;

            if (str is string strValue)
            {
                entry.Remove(tag, strValue);
                count++;
            }

            if (i is int intValue)
            {
                entry.Remove(tag, intValue);
                count++;
            }

            if (count == 0)
                entry.Remove(tag);
        }

        override public string? ToString()
        {
            if (str is null && i is null)
                return $"-{tag}";
            return $"{tag} - {ValueToString(str, i)}";
        }
    }

    private class SetTag(string tag, string? str, int? i) : ITagOperation
    {
        public void Apply(Entry entry)
        {
            int count = 0;

            if (str is string strValue)
            {
                entry.Set(tag, strValue);
                count++;
            }

            if (i is int intValue)
            {
                entry.Set(tag, intValue);
                count++;
            }

            if (count == 0)
            {
                entry.Remove(tag);
                entry.Add(tag);
            }
        }

        override public string? ToString()
        {
            if (str is null && i is null)
                return $"={tag}";
            return $"{tag} = {ValueToString(str, i)}";
        }
    }

    private class CombineOperations(IEnumerable<ITagOperation> operations) : ITagOperation
    {
        public void Apply(Entry entry)
        {
            foreach (var tagOp in operations)
                tagOp.Apply(entry);
        }

        override public string? ToString()
        {
            return String.Join(", ", operations);
        }
    }

    private static string ValueToString(string? strOpt, int? intOpt)
    {
        if (strOpt is string str)
        {
            var whitespace = "";
            for (int index = 0; index < str.Length; index++)
            {
                if (Char.IsWhiteSpace(str, index))
                {
                    whitespace = "\"";
                    break;
                }
            }

            return $"{whitespace}{str}{whitespace}";
        }

        if (intOpt is int i)
        {
            return i.ToString();
        }

        return "";
    }
}
