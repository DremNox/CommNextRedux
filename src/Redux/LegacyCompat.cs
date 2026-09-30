using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace BepInEx.Logging
{
    public sealed class ManualLogSource
    {
        private readonly string _name;
        public ManualLogSource(string name) { _name = name ?? "CommNextRedux"; }
        public void LogInfo(object data) => Debug.Log("[" + _name + "] " + data);
        public void LogWarning(object data) => Debug.LogWarning("[" + _name + "] " + data);
        public void LogError(object data) => Debug.LogError("[" + _name + "] " + data);
        public void LogDebug(object data) => Debug.Log("[" + _name + "][DEBUG] " + data);
        public void LogFatal(object data) => Debug.LogError("[" + _name + "][FATAL] " + data);
    }

    public static class Logger
    {
        public static ManualLogSource CreateLogSource(string name) => new ManualLogSource(name);
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
                if (EqualityComparer<T>.Default.Equals(_value, value)) return;
                _value = value;
                SettingChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SettingChanged;
    }

    public sealed class ConfigDescription
    {
        public string Description { get; }
        public object AcceptableValues { get; }
        public ConfigDescription(string description, object acceptableValues = null)
        {
            Description = description;
            AcceptableValues = acceptableValues;
        }
    }

    public sealed class AcceptableValueRange<T> where T : IComparable
    {
        public T MinValue { get; }
        public T MaxValue { get; }
        public AcceptableValueRange(T min, T max) { MinValue = min; MaxValue = max; }
    }

    public sealed class ConfigFile
    {
        private readonly Dictionary<string, object> _entries = new Dictionary<string, object>();

        public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string description)
            => Bind(section, key, defaultValue, new ConfigDescription(description));

        public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, ConfigDescription description)
        {
            var id = section + "/" + key;
            object existing;
            if (_entries.TryGetValue(id, out existing) && existing is ConfigEntry<T> typed)
                return typed;
            var entry = new ConfigEntry<T>(defaultValue);
            _entries[id] = entry;
            return entry;
        }
    }
}

namespace SpaceWarp.API.SaveGameManager
{
    public static class ModSaves
    {
        public static void RegisterSaveLoadGameData<T>(
            string modGuid,
            Action<T> saveCallback,
            Action<T> loadCallback) where T : new()
        {
            CommNextRedux.Compat.LegacySaveRegistry.Register(modGuid, saveCallback, loadCallback);
        }
    }
}

namespace SpaceWarp.API.Assets
{
    public static class AssetManager
    {
        public static T GetAsset<T>(string path) where T : UnityEngine.Object
            => CommNextRedux.Compat.LegacyAssetLoader.Get<T>(path);
    }
}

namespace UitkForKsp2
{
    public static class Configuration
    {
        public static bool IsAutomaticScalingEnabled => true;
        public static float ManualUiScale => 1f;
    }

    public static class ReferenceResolution
    {
        public static float Width => 1920f;
        public static float Height => 1080f;
    }
}

namespace UitkForKsp2.API
{
    public sealed class MoveOptions
    {
        public bool IsMovingEnabled { get; set; }
        public bool CheckScreenBounds { get; set; }
    }

    public sealed class WindowOptions
    {
        public string WindowId { get; set; }
        public object Parent { get; set; }
        public bool IsHidingEnabled { get; set; }
        public bool DisableGameInputForTextFields { get; set; }
        public MoveOptions MoveOptions { get; set; } = new MoveOptions();
    }

    public static class Window
    {
        public static UIDocument Create(WindowOptions options, VisualTreeAsset asset)
            => CommNextRedux.Compat.LegacyWindowFactory.Create(options, asset);
    }
}

namespace CommNextRedux
{
    public sealed class CommNextPlugin
    {
        public const string ModGuid = "CommNextRedux";
        public const string ModName = "CommNext Redux";
        public const string ModVer = "0.1.0-alpha";
        public static CommNextPlugin Instance { get; } = new CommNextPlugin();
        public BepInEx.Configuration.ConfigFile Config { get; } = new BepInEx.Configuration.ConfigFile();
        private CommNextPlugin() { }
    }
}

namespace CommNextRedux.Compat
{
    internal static class LegacyAssetLoader
    {
        private static AssetBundle _bundle;
        private static string[] _names;

        internal static T Get<T>(string requestedPath) where T : UnityEngine.Object
        {
            try
            {
                EnsureBundle();
                if (_bundle == null) return null;

                var normalized = (requestedPath ?? string.Empty).Replace('\\', '/').ToLowerInvariant();
                var marker = "/commnext_ui/";
                var index = normalized.IndexOf(marker, StringComparison.Ordinal);
                var suffix = index >= 0 ? normalized.Substring(index + marker.Length) : normalized;

                string assetName = null;
                foreach (var name in _names ?? Array.Empty<string>())
                {
                    var lower = name.ToLowerInvariant();
                    if (lower.EndsWith(suffix, StringComparison.Ordinal) ||
                        lower.EndsWith("/" + suffix, StringComparison.Ordinal))
                    {
                        assetName = name;
                        break;
                    }
                }

                if (assetName == null)
                {
                    Debug.LogWarning("[CommNextRedux] Asset not found in bundle: " + requestedPath);
                    return null;
                }

                return _bundle.LoadAsset<T>(assetName);
            }
            catch (Exception ex)
            {
                Debug.LogError("[CommNextRedux] Asset load error " + requestedPath + ": " + ex);
                return null;
            }
        }

        private static void EnsureBundle()
        {
            if (_bundle != null) return;
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(assemblyDir)) return;

            var candidates = new[]
            {
                Path.Combine(assemblyDir, "assets", "bundles", "commnext_ui.bundle"),
                Path.Combine(assemblyDir, "commnext_ui.bundle")
            };

            var bundlePath = candidates.FirstOrDefault(File.Exists);
            if (bundlePath == null)
            {
                Debug.LogWarning("[CommNextRedux] commnext_ui.bundle not found beside mod.");
                return;
            }

            _bundle = AssetBundle.LoadFromFile(bundlePath);
            _names = _bundle?.GetAllAssetNames();
            Debug.Log("[CommNextRedux] Legacy UI bundle loaded: " + bundlePath);
        }
    }

    internal static class LegacyWindowFactory
    {
        private static PanelSettings _panelSettings;

        internal static UIDocument Create(UitkForKsp2.API.WindowOptions options, VisualTreeAsset asset)
        {
            var go = new GameObject(string.IsNullOrEmpty(options?.WindowId) ? "CommNextReduxWindow" : options.WindowId);
            UnityEngine.Object.DontDestroyOnLoad(go);

            var document = go.AddComponent<UIDocument>();
            document.panelSettings = GetPanelSettings();
            document.visualTreeAsset = asset;
            document.sortingOrder = 250;
            return document;
        }

        private static PanelSettings GetPanelSettings()
        {
            if (_panelSettings != null) return _panelSettings;

            _panelSettings = Resources.FindObjectsOfTypeAll<PanelSettings>().FirstOrDefault();
            if (_panelSettings != null) return _panelSettings;

            _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            _panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            _panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            return _panelSettings;
        }
    }

    internal static class LegacySaveRegistry
    {
        private sealed class Entry
        {
            internal Action<object> Save;
            internal Action<object> Load;
            internal Func<object> Factory;
        }

        private static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>();

        internal static void Register<T>(string modGuid, Action<T> save, Action<T> load) where T : new()
        {
            Entries[modGuid] = new Entry
            {
                Save = o => save((T)o),
                Load = o => load((T)o),
                Factory = () => new T()
            };
            Debug.Log("[CommNextRedux] Legacy save callbacks registered for " + modGuid);
        }
    }
}

namespace CommNextRedux.Patches
{
    // Compatibility namespace retained for original source imports.
    internal static class ReduxCompatibilityMarker { }
}
