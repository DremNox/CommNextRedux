using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpaceWarp.API.Assets
{
    public static class AssetManager
    {
        private static AssetBundle _bundle;
        private static string[] _assetNames;

        public static T GetAsset<T>(string key) where T : UnityEngine.Object
        {
            EnsureBundle();
            if (_bundle == null) return null;

            var wanted = key.Replace('\\', '/').ToLowerInvariant();
            var slash = wanted.IndexOf("/commnext_ui/", StringComparison.Ordinal);
            if (slash >= 0) wanted = wanted.Substring(slash + 1);

            foreach (var name in _assetNames)
            {
                var normalized = name.Replace('\\', '/').ToLowerInvariant();
                if (normalized.EndsWith(wanted, StringComparison.Ordinal) ||
                    normalized.EndsWith(wanted.Replace("commnext_ui/", ""), StringComparison.Ordinal))
                    return _bundle.LoadAsset<T>(name);
            }

            CommNextRedux.CommNetBridge.Log?.LogWarning(
                "[CommNextRedux] Asset not found in bundle: " + key);
            return null;
        }

        private static void EnsureBundle()
        {
            if (_bundle != null) return;

            try
            {
                var folder = Path.GetDirectoryName(typeof(CommNextRedux.CommNextReduxPlugin).Assembly.Location);
                var path = Path.Combine(folder, "assets", "bundles", "commnext_ui.bundle");
                if (!File.Exists(path))
                {
                    CommNextRedux.CommNetBridge.Log?.LogWarning(
                        "[CommNextRedux] UI asset bundle not found: " + path);
                    return;
                }

                _bundle = AssetBundle.LoadFromFile(path);
                _assetNames = _bundle == null ? Array.Empty<string>() : _bundle.GetAllAssetNames();
                CommNextRedux.CommNetBridge.Log?.LogInfo(
                    "[CommNextRedux] Loaded original UI bundle; assets=" + _assetNames.Length);
            }
            catch (Exception ex)
            {
                CommNextRedux.CommNetBridge.Log?.LogError(
                    "[CommNextRedux] Loading UI bundle: " + ex);
            }
        }
    }
}

namespace SpaceWarp.API.SaveGameManager
{
    public static class ModSaves
    {
        private static readonly Dictionary<string, object> Registrations =
            new Dictionary<string, object>();

        public static void RegisterSaveLoadGameData<T>(
            string modGuid, Action<T> save, Action<T> load) where T : new()
        {
            Registrations[modGuid] = new Registration<T>(save, load);
        }

        private sealed class Registration<T>
        {
            internal readonly Action<T> Save;
            internal readonly Action<T> Load;
            internal Registration(Action<T> save, Action<T> load)
            {
                Save = save;
                Load = load;
            }
        }
    }
}
