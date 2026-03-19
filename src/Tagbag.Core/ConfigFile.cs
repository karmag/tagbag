using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

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

    public static void Save<T>(IEnumerable<ConfigValue<T>> values) where T : IEquatable<T>
    {
        Save(GetConfigPath(), values);
    }

    // Saves the non-default values of the values into the given file.
    public static void Save<T>(string path,
                               IEnumerable<ConfigValue<T>> values) where T : IEquatable<T>
    {
        var data = new Dictionary<string, Object>();
        foreach (var cv in values)
            if (!cv.IsDefault())
                data[cv.Name] = cv.Get();

        using (var stream = File.Open(path, FileMode.Create))
            JsonSerializer.Serialize(stream, data);
    }

    public static bool Load<T>(IEnumerable<ConfigValue<T>> values) where T : IEquatable<T>
    {
        return Load(GetConfigPath(), values);
    }

    // Populates the values with data found in the config file.
    public static bool Load<T>(string path,
                               IEnumerable<ConfigValue<T>> values) where T : IEquatable<T>
    {
        if (!File.Exists(path))
            return false;

        using (var stream = File.Open(path, FileMode.Open))
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, int>>(stream);
            if (data != null)
            {
                foreach (var cv in values)
                {
                    int val;
                    if (data.TryGetValue(cv.Name, out val) &&
                        cv is ConfigValue<int> intConfigValue &&
                        val is int i)
                    {
                        intConfigValue.Set(i);
                    }
                }
            }
        }

        return true;
    }
}

public class ConfigValue<T> where T : IEquatable<T>
{
    public string Name { get; }
    public string Description { get; }
    private T _DefaultValue;
    private T _CurrentValue;
    private List<Func<T, string?>> _Constraints;

    public Action<T, T>? Changed; // Called with old-value, new-value

    public ConfigValue(string name,
                       T defaultValue,
                       string description,
                       params Func<T, string?>[] constraints)
    {
        Name = name;
        Description = description;
        _DefaultValue = defaultValue;
        _CurrentValue = defaultValue;
        _Constraints = new List<Func<T, string?>>(constraints);

        if (Set(defaultValue) is string error)
            throw new InvalidOperationException(
                $"Default value doesn't pass constraints; {error}");
    }

    public T Get()
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

    public void Reset()
    {
        Set(_DefaultValue);
    }

    public bool IsDefault()
    {
        return _DefaultValue.Equals(_CurrentValue);
    }
}

public static class ConfigValueContraint
{
    public static Func<int, string?> Range(int min, int max)
    {
        return (int i) =>
        {
            if (min <= i && i <= max)
                return null;
            return $"Must be in range {min} < x < {max}";
        };
    }
}
