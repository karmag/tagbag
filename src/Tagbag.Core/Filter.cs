using System;
using System.Collections.Generic;

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
            return false;
        }

        protected bool AnyInt(Entry entry, Func<int, bool> f)
        {
            foreach (var i in entry.GetInts(_tag) ?? [])
                if (f(i))
                    return true;
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
    }

    private class Logic : IFilter
    {
        private int _mode;
        private IEnumerable<IFilter> _filters;

        private Logic(int mode, IEnumerable<IFilter> filters)
        {
            _mode = mode;
            _filters = filters;
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
                    foreach (var filter in _filters)
                        return !filter.Keep(entry);
                    throw new InvalidOperationException("No filter for 'not'");

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
    }
}
