using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Tagbag.Gui;

public class ActionDef
{
    public string Id { get; }
    public Action<Data> Action { get; }
    public string Name { get; }

    public ActionDef(string id,
                     Action<Data> action,
                     string? name = null)
    {
        Id = id;
        Action = action;
        Name = name ?? id;
    }
}

public class KeyData
{
    public Mode? Mode { get; }
    public Keys Key { get; }
    public string ActionId { get; }
    public Func<Data, bool>? IsValid { get; }

    public KeyData(Mode? mode,
                   Keys key,
                   string actionId,
                   Func<Data, bool>? isValid = null)
    {
        Mode = mode;
        Key = key;
        ActionId = actionId;
        IsValid = isValid;
    }
}

public class KeyMap
{
    private List<KeyData> _RawKeys;
    private Dictionary<string, ActionDef> _ActionMapping;

    private Mode? _Mode;
    private Dictionary<Mode, Dictionary<Keys, KeyData>> _Mapping;
    private Dictionary<Keys, KeyData> _Common;

    public KeyMap()
    {
        _RawKeys = new List<KeyData>();
        _ActionMapping = new Dictionary<string, ActionDef>();

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

    public void Add(ActionDef actionDef)
    {
        if (_ActionMapping.ContainsKey(actionDef.Id))
            throw new InvalidOperationException($"Action with name '{actionDef.Id}' already exists");
        _ActionMapping.Add(actionDef.Id, actionDef);
    }

    public void Add(KeyData keyData)
    {
        if (!_ActionMapping.ContainsKey(keyData.ActionId))
            throw new InvalidOperationException($"Action with id '{keyData.ActionId}' doesn't exist");
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

    public ActionDef? Get(string actionId)
    {
        ActionDef? actionDef;
        _ActionMapping.TryGetValue(actionId, out actionDef);
        return actionDef;
    }

    public IEnumerable<KeyData> GetKeyData()
    {
        return _RawKeys;
    }

    public IEnumerable<ActionDef> GetActionDefs()
    {
        return _ActionMapping.Values;
    }
}
