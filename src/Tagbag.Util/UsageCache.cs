using System;
using System.Collections.Generic;

public class UsageCache<TKey, TValue> where TKey : notnull
{
    private int _MaxSize;
    private Func<TKey, TValue> _Constructor;
    private Action<TKey, TValue> _Destructor;

    private PriorityQueue<TKey, int> _Queue;
    private Dictionary<TKey, TValue> _Lookup;
    private int _Priority;

    public UsageCache(int maxSize,
                      Func<TKey, TValue> constructor,
                      Action<TKey, TValue> destructor)
    {
        _MaxSize = maxSize;
        _Constructor = constructor;
        _Destructor = destructor;

        _Queue = new PriorityQueue<TKey, int>();
        _Lookup = new Dictionary<TKey, TValue>();
        _Priority = 0;
    }

    public TValue Get(TKey key)
    {
        TValue? oldValue;
        if (_Lookup.TryGetValue(key, out oldValue) && oldValue is TValue)
        {
            RefreshKey(key, oldValue);
            return oldValue;
        }

        TValue value = _Constructor(key);
        Add(key, value);
        return value;
    }

    public void Clear()
    {
        _Queue.Clear();
        _Lookup.Clear();
        _Priority = 0;
    }

    private void Add(TKey key, TValue value)
    {
        _Queue.Enqueue(key, _Priority);
        _Lookup[key] = value;
        _Priority++;

        if (_Queue.Count > _MaxSize)
        {
            TKey ejectKey = _Queue.Dequeue();
            TValue? ejectValue;
            if (_Lookup.Remove(ejectKey, out ejectValue))
                _Destructor(ejectKey, ejectValue);
        }
    }

    private void RefreshKey(TKey key, TValue value)
    {
        TKey? oldKey;
        int oldPrio;
        _Queue.Remove(key, out oldKey, out oldPrio);
        Add(key, value);
    }
}
