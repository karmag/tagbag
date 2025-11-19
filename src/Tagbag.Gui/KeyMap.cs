using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Tagbag.Gui;

public class KeyData
{
    public Mode? Mode { get; }
    public Keys Key { get; }
    public Action<Data> Action { get; }
    public Func<Data, bool>? IsValid { get; }

    public KeyData(Mode? mode,
                   Keys key,
                   Action<Data> action,
                   Func<Data, bool>? isValid = null)
    {
        Mode = mode;
        Key = key;
        Action = action;
        IsValid = isValid;
    }
}

public class KeyMap
{
    private List<KeyData> _RawKeys;

    private Mode? _Mode;
    private Dictionary<Mode, Dictionary<Keys, KeyData>> _Mapping;
    private Dictionary<Keys, KeyData> _Common;

    public KeyMap()
    {
        _RawKeys = new List<KeyData>();

        _Mode = null;
        _Mapping = new Dictionary<Mode, Dictionary<Keys, KeyData>>();
        _Common = new Dictionary<Keys, KeyData>();
    }

    private Dictionary<Keys, KeyData> GetOrCreateKeyMapping(Mode? modeOpt)
    {
        Dictionary<Keys, KeyData>? keyMapping = _Common;
        if (modeOpt is Mode mode)
        {
            _Mapping.TryGetValue(mode, out keyMapping);
            if (keyMapping is Dictionary<Keys, KeyData> existingKeyMapping)
            {
                return existingKeyMapping;
            }
            else
            {
                var newKeyMapping = new Dictionary<Keys, KeyData>();
                _Mapping[mode] = newKeyMapping;
                return newKeyMapping;
            }
        }
        return keyMapping;
    }

    public void Add(KeyData keyData)
    {
        _RawKeys.Add(keyData);
        GetOrCreateKeyMapping(keyData.Mode)[keyData.Key] = keyData;
    }

    public void SetMode(Mode? mode)
    {
        _Mode = mode;
    }

    public KeyData? Get(Keys keys)
    {
        KeyData? keyData;
        if (GetOrCreateKeyMapping(_Mode).TryGetValue(keys, out keyData))
            return keyData;
        GetOrCreateKeyMapping(null).TryGetValue(keys, out keyData);
        return keyData;
    }
}
