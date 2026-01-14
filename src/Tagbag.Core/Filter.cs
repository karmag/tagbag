using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Tagbag.Core;

public interface IFilter
{
    public bool Keep(Entry entry);
}

public class Filter
{
    public static IFilter Has(string tag)                   { return new Existence(tag); }
    public static IFilter Has(string tag, string value)     { return new HasValue(tag, value); }
    public static IFilter Has(string tag, int value)        { return new HasValue(tag, value); }
    public static IFilter Not(IFilter filter)               { return Logic.Not(filter); }
    public static IFilter And(params IFilter[] filters)     { return Logic.And(filters); }
    public static IFilter And(IEnumerable<IFilter> filters) { return Logic.And(filters); }
    public static IFilter Or(params IFilter[] filters)      { return Logic.Or(filters); }
    public static IFilter Or(IEnumerable<IFilter> filters)  { return Logic.Or(filters); }
    public static IFilter Regex(string tag, string pattern) { return new RegexFilter(tag, pattern); }
    public static IFilter Math(string tag, string op, int value) { return new MathFilter(tag, op, value); }

    private abstract class BaseFilter : IFilter
    {
        protected string _tag;

        protected BaseFilter(string tag)
        {
            _tag = tag;
        }

        public abstract bool Keep(Entry entry);

        protected bool AnyString(Entry entry, Func<string, bool> f)
        {
            foreach (var s in entry.GetStrings(_tag) ?? [])
                if (f(s))
                    return true;

            if (_tag == "path")
                return f(entry.Path);

            return false;
        }

        protected bool AnyInt(Entry entry, Func<int, bool> f)
        {
            foreach (var i in entry.GetInts(_tag) ?? [])
                if (f(i))
                    return true;

            if (_tag == "tag-count")
            {
                var count = 0;
                foreach (var tag in entry.GetAllTags())
                    if (!Const.BuiltinTags.Contains(tag))
                        count++;
                return f(count);
            }

            return false;
        }
    }

    private class Existence : IFilter
    {
        private string _tag;
        public Existence(string tag)
        {
            _tag = tag;
        }

        public bool Keep(Entry entry)
        {
            return entry.Get(_tag) != null;
        }

        override public string? ToString()
        {
            return _tag;
        }
    }

    private class HasValue : BaseFilter
    {
        private bool _is_string;
        private string _string;
        private int _int;

        public HasValue(string tag, string value) : base(tag)
        {
            _is_string = true;
            _string = value;
        }

        public HasValue(string tag, int value) : base(tag)
        {
            _is_string = false;
            _string = default!;
            _int = value;
        }

        override public bool Keep(Entry entry)
        {
            if (_is_string)
                return AnyString(entry, s => String.Compare(s, _string, true) == 0);
            else
                return AnyInt(entry, i => i == _int);
        }

        override public string? ToString()
        {
            if (_is_string)
                return $"{_tag} = \"{_string}\"";
            else
                return $"{_tag} = {_int}";
        }
    }

    private class Logic : IFilter
    {
        private int _mode;
        private List<IFilter> _filters;

        private Logic(int mode, IEnumerable<IFilter> filters)
        {
            _mode = mode;
            _filters = new List<IFilter>(filters);
        }

        public static Logic Not(IFilter filter)
        {
            return new Logic(0, [filter]);
        }

        public static Logic And(IEnumerable<IFilter> filters)
        {
            return new Logic(1, filters);
        }

        public static Logic Or(IEnumerable<IFilter> filters)
        {
            return new Logic(2, filters);
        }

        public bool Keep(Entry entry)
        {
            switch (_mode)
            {
                case 0: // not
                    return !_filters[0].Keep(entry);

                case 1: // and
                    foreach (var filter in _filters)
                        if (!filter.Keep(entry))
                            return false;
                    return true;

                case 2: // or
                    foreach (var filter in _filters)
                        if (filter.Keep(entry))
                            return true;
                    return false;
            }

            throw new InvalidOperationException("Unknown boolean logic mode");
        }

        override public string? ToString()
        {
            switch (_mode)
            {
                case 0: // not
                    return $"not {_filters[0]}";

                case 1: // and
                    return String.Join(" and ", _filters);

                case 2: // or
                    return String.Join(" or ", _filters);

                default:
                    return "Unknown logic filter type";
            }
        }
    }

    private class RegexFilter : BaseFilter
    {
        private string _Pattern;
        private Regex _Regex;

        public RegexFilter(string tag, string pattern) : base(tag)
        {
            _Pattern = pattern;
            _Regex = new Regex(pattern, RegexOptions.IgnoreCase);
        }

        override public bool Keep(Entry entry)
        {
            return AnyString(entry, _Regex.IsMatch);
        }

        override public string ToString()
        {
            return $"{_tag} ~= \"{_Pattern}\"";
        }
    }

    private class MathFilter : BaseFilter
    {
        private string _OpText;
        private int _Value;
        private Func<int, bool> _Op;

        public MathFilter(string tag, string op, int value) : base(tag)
        {
            _OpText = op;
            _Value = value;
            switch (op)
            {
                case "<": _Op = i => { return i < value; }; break;
                case ">": _Op = i => { return i > value; }; break;
                case "<=": _Op = i => { return i <= value; }; break;
                case ">=": _Op = i => { return i >= value; }; break;
                default:
                    throw new ArgumentException($"Unknown math operator: {op}");
            }
        }

        override public bool Keep(Entry entry)
        {
            return AnyInt(entry, _Op);
        }

        override public string ToString()
        {
            return $"{_tag} {_OpText} {_Value}";
        }
    }
}
