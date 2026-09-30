using System;
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
        public ConfigEntry(T value) { _value = value; }

        public T Value
        {
            get => _value;
            set
            {
                if (Equals(_value, value)) return;
                _value = value;
                SettingChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler SettingChanged;
    }

    public sealed class ConfigFile
    {
        public ConfigEntry<T> Bind<T>(
            string section, string key, T defaultValue, string description) =>
            new ConfigEntry<T>(defaultValue);

        public ConfigEntry<T> Bind<T>(
            string section, string key, T defaultValue, ConfigDescription description) =>
            new ConfigEntry<T>(defaultValue);
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
