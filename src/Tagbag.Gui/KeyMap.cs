using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Tagbag.Gui;

public class KeyMap
{
    private Mode _Mode;
    private Dictionary<Mode, Dictionary<Keys, Action<Data>>> _Mapping;

    public KeyMap()
    {
        _Mode = Mode.GridMode;
        _Mapping = new Dictionary<Mode, Dictionary<Keys, Action<Data>>>();
    }

    private Dictionary<Keys, Action<Data>> GetOrCreateKeyMapping(Mode mode)
    {
        Dictionary<Keys, Action<Data>>? keyMapping;
        _Mapping.TryGetValue(mode, out keyMapping);
        if (keyMapping is Dictionary<Keys, Action<Data>> existingKeyMapping)
        {
            return existingKeyMapping;
        }
        else
        {
            var newKeyMapping = new Dictionary<Keys, Action<Data>>();
            _Mapping[mode] = newKeyMapping;
            return newKeyMapping;
        }
    }

    public void SwapMode(Mode mode)
    {
        _Mode = mode;
    }

    public void Register(Keys keys, Action<Data> action)
    {
        GetOrCreateKeyMapping(_Mode)[keys] = action;
    }

    public Action<Data>? Get(Keys keys)
    {
        Action<Data>? action;
        GetOrCreateKeyMapping(_Mode).TryGetValue(keys, out action);
        return action;
    }
}
