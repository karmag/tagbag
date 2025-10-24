using System.Collections.Generic;

public class CircularList<E>
{
    private List<E?> _Elements;
    private HashSet<E> _Lookup;
    private int _Size;
    private int _Counter;

    public CircularList(int size)
    {
        _Elements = new List<E?>(size);
        _Lookup = new HashSet<E>();
        _Size = size;
        _Counter = 0;

        for (int i = 0; i < size; i++)
            _Elements.Add(default);
    }

    // Adds the element to this list. Returns the ejected element if
    // any. Returns null if no element is ejected.
    public E? Add(E element)
    {
        var old = _Elements[_Counter % _Size];
        _Elements[_Counter % _Size] = element;
        return old;
    }
}
