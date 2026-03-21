using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tagbag.Core.Input;

namespace Tagbag.Core;

public static class ConfigFile
{
    private const string Filename = "tagbag.cfg";

    public static string GetConfigPath()
    {
        var programPath = Environment.GetCommandLineArgs()[0];
        if (Path.GetDirectoryName(programPath) is string dir)
            return Path.Join(dir, Filename);
        throw new InvalidOperationException("Unable to determine config path");
    }

    public static void Save(IEnumerable<ConfigValue> values)
    {
        Save(GetConfigPath(), values);
    }

    // Saves the non-default values of the values into the given file.
    public static void Save(string path, IEnumerable<ConfigValue> values)
    {
        var data = new Dictionary<string, Object>();
        foreach (var cv in values)
            if (!cv.IsDefault())
                data[cv.Name] = cv.GetRaw();

        using (var stream = File.Open(path, FileMode.Create))
            JsonSerializer.Serialize(stream, data);
    }

    public static bool Load(IEnumerable<ConfigValue> values)
    {
        return Load(GetConfigPath(), values);
    }

    // Populates the values with data found in the config file.
    public static bool Load(string path, IEnumerable<ConfigValue> values)
    {
        if (!File.Exists(path))
            return false;

        using (var stream = File.Open(path, FileMode.Open))
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonValue>>(stream);
            if (data != null)
            {
                foreach (var cv in values)
                {
                    JsonValue? json;
                    if (data.TryGetValue(cv.Name, out json))
                    {
                        Object value = cv;
                        switch (json.GetValueKind())
                        {
                            case JsonValueKind.String:
                                value = json.GetValue<string>();
                                break;
                            case JsonValueKind.Number:
                                value = json.GetValue<int>();
                                break;
                            default:
                                System.Console.WriteLine(
                                    $"[WARN] Unknown config value type {json.GetValueKind()} for {cv.Name}");
                                break;
                        }

                        if (value != cv && cv.SetRaw(value) is string error)
                            System.Console.WriteLine(
                                $"[WARN] Loading config for {cv.Name} failed with: {error}");
                    }
                }
            }
        }

        return true;
    }
}

public abstract class ConfigValue
{
    public Action<Object, Object>? ChangedRaw;
    public string Name { get; }
    public string Description { get; }

    public abstract Object GetRaw();
    public abstract string? SetRaw(Object value);
    public abstract void Reset();
    public abstract bool IsDefault();
    public abstract string? Check(Object value);

    public ConfigValue(string name,
                       string description)
    {
        Name = name;
        Description = description;
    }

    public static (bool, int) IntParse(Object obj)
    {
        switch (obj)
        {
            case int i:
                return (true, i);
            case string s:
                int intVal;
                if (int.TryParse(s, out intVal))
                    return (true, intVal);
                break;
        }
        return (false, default);
    }

    public static (bool, string) StringParse(Object obj)
    {
        if (obj == null)
            return (false, "");
        else
            return (true, obj.ToString() ?? "");
    }

    public static Func<int, string?> RangeConstraint(int min, int max)
    {
        return (int i) =>
        {
            if (min <= i && i <= max)
                return null;
            return $"Must be in range {min} < x < {max}";
        };
    }

    public static Func<string, string?> TokenizeConstraint = (string s) =>
    {
        try
        {
            Tokenizer.GetTokens(s);
            return null;
        }
        catch (TokenizerException e)
        {
            return $"Invalid token sequence; {e.Message}";
        }
    };
}

public class ConfigValue<T> : ConfigValue where T : IEquatable<T>
{
    private T _DefaultValue;
    private T _CurrentValue;
    private Func<Object, (bool, T)> _ParseFn;
    private List<Func<T, string?>> _Constraints;

    public Action<T, T>? Changed; // Called with old-value, new-value

    public ConfigValue(string name,
                       T defaultValue,
                       Func<Object, (bool, T)> parseFn,
                       string description,
                       params Func<T, string?>[] constraints) : base(name, description)
    {
        _DefaultValue = defaultValue;
        _CurrentValue = defaultValue;
        _ParseFn = parseFn;
        _Constraints = new List<Func<T, string?>>(constraints);

        Changed += (a, b) => ChangedRaw?.Invoke(a, b);

        if (Set(defaultValue) is string error)
            throw new InvalidOperationException(
                $"Default value doesn't pass constraints; {error}");
    }

    public T Get()
    {
        return _CurrentValue;
    }

    override public Object GetRaw()
    {
        return _CurrentValue;
    }

    // Sets the value. Returns null if value was set successfully,
    // returns a string indicating the error with the value
    // otherwise.
    public string? Set(T value)
    {
        foreach (var c in _Constraints)
            if (c.Invoke(value) is String error)
                return error;

        var oldValue = _CurrentValue;
        _CurrentValue = value;
        if (!oldValue.Equals(value))
            Changed?.Invoke(oldValue, value);

        return null;
    }

    override public string? SetRaw(Object value)
    {
        var (ok, val) = _ParseFn(value);
        if (ok)
            return Set(val);
        return "Unparsable value type";
    }

    override public void Reset()
    {
        Set(_DefaultValue);
    }

    override public bool IsDefault()
    {
        return _DefaultValue.Equals(_CurrentValue);
    }

    public override string? Check(object value)
    {
        var (ok, val) = _ParseFn(value);
        if (!ok)
            return "Parsing value failed";

        foreach (var c in _Constraints)
            if (c.Invoke(val) is String error)
                return error;

        return null;
    }
}
