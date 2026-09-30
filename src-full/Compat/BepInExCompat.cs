using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace BepInEx.Logging
{
    public sealed class ManualLogSource
    {
        private readonly string _name;
        internal ManualLogSource(string name) { _name = name; }

        public void LogInfo(object data) =>
            Write("INFO", data, false);
        public void LogWarning(object data) =>
            Write("WARN", data, false);
        public void LogError(object data) =>
            Write("ERROR", data, true);
        public void LogDebug(object data) =>
            Write("DEBUG", data, false);

        private void Write(string level, object data, bool error)
        {
            var message = "[CommNext][" + _name + "][" + level + "] " + (data ?? "null");
            var redux = CommNextRedux.CommNetBridge.Log;
            if (redux != null)
            {
                if (error) redux.LogError(message);
                else if (level == "WARN") redux.LogWarning(message);
                else redux.LogInfo(message);
                return;
            }

            if (error) Debug.LogError(message);
            else if (level == "WARN") Debug.LogWarning(message);
            else Debug.Log(message);
        }
    }

    public static class Logger
    {
        public static ManualLogSource CreateLogSource(string name) =>
            new ManualLogSource(name);
    }
}

namespace BepInEx.Configuration
{
    public sealed class ConfigEntry<T>
    {
        private T _value;
        private readonly Action<T> _persist;

        internal ConfigEntry(T value, Action<T> persist)
        {
            _value = value;
            _persist = persist;
        }

        public T Value
        {
            get => _value;
            set
            {
                if (Equals(_value, value)) return;
                _value = value;
                _persist?.Invoke(value);
                SettingChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler SettingChanged;
    }

    /// <summary>
    /// Small persistent compatibility implementation for the subset of BepInEx
    /// configuration used by CommNext. Values are stored as an INI-like file in
    /// mods/CommNextRedux/CommNextRedux.cfg.
    /// </summary>
    public sealed class ConfigFile
    {
        private readonly string _path;
        private readonly Dictionary<string, Dictionary<string, string>> _values =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public ConfigFile()
        {
            var gameRoot = Path.GetDirectoryName(Application.dataPath) ?? AppContext.BaseDirectory;
            _path = Path.Combine(gameRoot, "mods", "CommNextRedux", "CommNextRedux.cfg");
            Load();
        }

        public ConfigEntry<T> Bind<T>(
            string section, string key, T defaultValue, string description) =>
            BindInternal(section, key, defaultValue);

        public ConfigEntry<T> Bind<T>(
            string section, string key, T defaultValue, ConfigDescription description) =>
            BindInternal(section, key, defaultValue);

        private ConfigEntry<T> BindInternal<T>(string section, string key, T defaultValue)
        {
            T value = defaultValue;
            string raw;

            Dictionary<string, string> sectionValues;
            if (_values.TryGetValue(section, out sectionValues) &&
                sectionValues.TryGetValue(key, out raw))
            {
                T parsed;
                if (TryParse(raw, out parsed))
                    value = parsed;
            }

            SetRaw(section, key, Serialize(value), false);

            return new ConfigEntry<T>(value, newValue =>
            {
                SetRaw(section, key, Serialize(newValue), true);
            });
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_path)) return;

                var currentSection = "General";
                foreach (var rawLine in File.ReadAllLines(_path))
                {
                    var line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                        continue;

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentSection = line.Substring(1, line.Length - 2).Trim();
                        continue;
                    }

                    var separator = line.IndexOf('=');
                    if (separator <= 0) continue;

                    var key = line.Substring(0, separator).Trim();
                    var value = line.Substring(separator + 1).Trim();
                    SetRaw(currentSection, key, value, false);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[CommNextRedux] Could not read config: " + ex.Message);
            }
        }

        private void SetRaw(string section, string key, string value, bool save)
        {
            Dictionary<string, string> sectionValues;
            if (!_values.TryGetValue(section, out sectionValues))
            {
                sectionValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _values[section] = sectionValues;
            }

            sectionValues[key] = value;
            if (save) Save();
        }

        private void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                using (var writer = new StreamWriter(_path, false))
                {
                    writer.WriteLine("# CommNext Redux configuration");
                    writer.WriteLine("# Generated automatically. Values can also be changed in the Vessel Comms Report.");
                    writer.WriteLine();

                    foreach (var section in _values)
                    {
                        writer.WriteLine("[" + section.Key + "]");
                        foreach (var entry in section.Value)
                            writer.WriteLine(entry.Key + " = " + entry.Value);
                        writer.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[CommNextRedux] Could not save config: " + ex.Message);
            }
        }

        private static string Serialize<T>(T value)
        {
            if (value == null) return string.Empty;
            if (value is IFormattable formattable)
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            return value.ToString();
        }

        private static bool TryParse<T>(string raw, out T value)
        {
            try
            {
                var type = typeof(T);
                object parsed;

                if (type.IsEnum)
                    parsed = Enum.Parse(type, raw, true);
                else if (type == typeof(string))
                    parsed = raw;
                else
                    parsed = Convert.ChangeType(raw, type, CultureInfo.InvariantCulture);

                value = (T)parsed;
                return true;
            }
            catch
            {
                value = default(T);
                return false;
            }
        }
    }

    public sealed class ConfigDescription
    {
        public ConfigDescription(string description, object acceptableValues = null) { }
    }

    public sealed class AcceptableValueRange<T>
    {
        public AcceptableValueRange(T minValue, T maxValue) { }
    }
}
