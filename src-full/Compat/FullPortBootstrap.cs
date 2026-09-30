using System;
using CommNext.Managers;
using CommNext.Network;
using CommNext.Rendering;
using CommNext.Rendering.Behaviors;
using CommNext.UI;
using CommNext.Utils;
using CommNext.Unity.Runtime.Controls;
using SpaceWarp.API.Assets;
using UitkForKsp2.API;
using UnityEngine;

namespace CommNextRedux
{
    internal static class FullPortBootstrap
    {
        internal static bool IsInitialized { get; private set; }
        private static GameObject _providers;

        internal static void Initialize()
        {
            if (IsInitialized) return;

            try
            {
                PluginSettings.SetupConfig();

                // The original custom UXML controls are compiled into this assembly.
                // Current UI Toolkit discovers their UxmlFactory types automatically.
                LoadRenderingAssets();

                _providers = new GameObject("CommNextRedux_FullPort_Providers");
                UnityEngine.Object.DontDestroyOnLoad(_providers);

                var renderer = _providers.AddComponent<ConnectionsRenderer>();
                renderer.Initialize();

                MainUIManager.Instance.Initialize();
                SaveManager.Instance.Register();
                MessageListener.StartListening();

                if (CommNetBridge.Manager != null)
                    NetworkManager.Instance.Initialize(CommNetBridge.Manager);

                IsInitialized = true;
                CommNetBridge.Log?.LogInfo("[CommNextRedux] Full CommNext UI/rendering bootstrap initialized");
            }
            catch (Exception ex)
            {
                CommNetBridge.Log?.LogError("[CommNextRedux] Full port bootstrap failed: " + ex);
            }
        }

        private static void LoadRenderingAssets()
        {
            try
            {
                ConnectionsRenderer.RulerSpherePrefab =
                    AssetManager.GetAsset<GameObject>(
                        "CommNextRedux/commnext_ui/meshes/rulersphere.prefab");

                ConnectionsRenderer.TestSpherePrefab =
                    AssetManager.GetAsset<GameObject>(
                        "CommNextRedux/commnext_ui/meshes/testsphere.prefab");

                MapConnectionComponent.LineMaterial =
                    AssetManager.GetAsset<Material>(
                        "CommNextRedux/commnext_ui/shaders/commconnectionmat.mat");

                if (MapConnectionComponent.LineMaterial == null)
                {
                    var shader = Shader.Find("Sprites/Default");
                    if (shader != null)
                        MapConnectionComponent.LineMaterial = new Material(shader);
                }

                CommNetBridge.Log?.LogInfo(
                    "[CommNextRedux] Original rendering assets loaded: " +
                    "ruler=" + (ConnectionsRenderer.RulerSpherePrefab != null) +
                    " material=" + (MapConnectionComponent.LineMaterial != null));
            }
            catch (Exception ex)
            {
                CommNetBridge.Log?.LogError("[CommNextRedux] Loading original rendering assets: " + ex);
            }
        }
    }
}
