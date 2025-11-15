using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Tagbag.Gui;

public class KeyMap
{
    private Mode? _Mode;
    private Dictionary<Mode, Dictionary<Keys, Action<Data>>> _Mapping;
    private Dictionary<Keys, Action<Data>> _Common;

    public KeyMap()
    {
        _Mode = Mode.BrowseMode;
        _Mapping = new Dictionary<Mode, Dictionary<Keys, Action<Data>>>();
        _Common = new Dictionary<Keys, Action<Data>>();
    }

    private Dictionary<Keys, Action<Data>> GetOrCreateKeyMapping(Mode? mode)
    {
        Dictionary<Keys, Action<Data>>? keyMapping = _Common;
        if (mode is Mode totallyMode)
        {
            _Mapping.TryGetValue(totallyMode, out keyMapping);
            if (keyMapping is Dictionary<Keys, Action<Data>> existingKeyMapping)
            {
                return existingKeyMapping;
            }
            else
            {
                var newKeyMapping = new Dictionary<Keys, Action<Data>>();
                _Mapping[totallyMode] = newKeyMapping;
                return newKeyMapping;
            }
        }
        return keyMapping;
    }

    public void SwapMode(Mode? mode)
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
        if (GetOrCreateKeyMapping(_Mode).TryGetValue(keys, out action))
            return action;
        GetOrCreateKeyMapping(null).TryGetValue(keys, out action);
        return action;
    }
}
