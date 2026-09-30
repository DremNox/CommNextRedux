using System;
using System.Reflection;
using KSP.Game;
using KSP.Sim.impl;
using Redux.ExtraModTypes;
using ReduxLib.Logging;
using UnityEngine;
using ILogger = ReduxLib.Logging.ILogger;

namespace CommNextRedux
{
    public sealed class CommNextReduxPlugin : KerbalMod
    {
        private ILogger _log;
        private Rect _window = new Rect(25f, 80f, 420f, 210f);
        private bool _visible = true;
        private VesselComponent _vessel;
        private string _commNetStatus = "Pendiente";
        private float _nextRefresh;

        public override void OnPreInitialized()
        {
            _log = SWLogger;
            _log.LogInfo("[CommNextRedux] 0.0.2 pre-initialized");
        }

        public override void OnInitialized()
        {
            _log.LogInfo("[CommNextRedux] 0.0.2 initialized");
            RefreshState();
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.C))
                _visible = !_visible;

            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 1f;
                RefreshState();
            }
        }

        private void RefreshState()
        {
            try
            {
                var game = GameManager.Instance == null ? null : GameManager.Instance.Game;
                _vessel = game == null || game.ViewController == null
                    ? null
                    : game.ViewController.GetActiveSimVessel(false);

                var property = game == null
                    ? null
                    : game.GetType().GetProperty("CommNetManager",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                var field = game == null
                    ? null
                    : game.GetType().GetField("_commNetManager",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                var manager = property != null
                    ? property.GetValue(game, null)
                    : (field == null ? null : field.GetValue(game));

                _commNetStatus = manager == null
                    ? "CommNetManager no expuesto directamente"
                    : "CommNetManager detectado: " + manager.GetType().FullName;
            }
            catch (Exception ex)
            {
                _commNetStatus = "Error inspeccionando CommNet";
                _log.LogError("[CommNextRedux] RefreshState: " + ex);
            }
        }

        private void OnGUI()
        {
            if (!_visible || _vessel == null) return;
            _window = GUI.Window(728431, _window, DrawWindow, "CommNext Redux 0.0.2");
        }

        private void DrawWindow(int id)
        {
            GUILayout.Label("Port inicial KSP2 Redux");
            GUILayout.Label(_vessel == null ? "Nave activa: ninguna" : "Nave activa: " + _vessel.DisplayName);
            GUILayout.Label(_commNetStatus);
            GUILayout.Space(5f);
            GUILayout.Label("Alt+C: mostrar / ocultar");
            GUI.DragWindow(new Rect(0f, 0f, _window.width, 25f));
        }
    }
}
