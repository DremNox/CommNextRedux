using System;
using HarmonyLib;
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
        private Harmony _harmony;
        private Rect _window = new Rect(25f, 80f, 460f, 260f);
        private bool _visible = true;
        private VesselComponent _vessel;
        private string _connectionStatus = "Sin datos";
        private double _rangeMeters;
        private double _distanceMeters = -1d;
        private int _nodeCount;
        private CommNextRouteResult _route = new CommNextRouteResult();
        private bool _isFlightScene;
        private float _nextRefresh;

        public override void OnPreInitialized()
        {
            _log = SWLogger;
            CommNetBridge.Log = _log;
            _log.LogInfo("[CommNextRedux] 0.0.11 pre-initialized");
        }

        public override void OnInitialized()
        {
            _harmony = new Harmony("DremNox.CommNextRedux");
            _harmony.PatchAll(typeof(CommNextReduxPlugin).Assembly);
            _log.LogInfo("[CommNextRedux] 0.0.11 initialized; map route rendering active");
            RefreshState();
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.C))
                _visible = !_visible;

            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 0.5f;
                RefreshState();
            }

            ReduxMapRouteRenderer.Update(_vessel, _route);
        }

        private void RefreshState()
        {
            try
            {
                var game = GameManager.Instance == null ? null : GameManager.Instance.Game;
                var state = game == null || game.GlobalGameState == null
                    ? null
                    : game.GlobalGameState.GetGameState();

                _isFlightScene = state != null && state.IsFlightMode && !state.IsObjectAssembly;

                _vessel = !_isFlightScene || game == null || game.ViewController == null
                    ? null
                    : game.ViewController.GetActiveSimVessel(false);

                _nodeCount = CommNetBridge.NodeCount;
                _route = _isFlightScene && _vessel != null
                    ? ManagedCommNextGraph.BuildRoute(_vessel)
                    : new CommNextRouteResult();

                string status;
                double range;
                double distance;
                if (CommNetBridge.TryGetVesselInfo(_vessel, out status, out range, out distance))
                {
                    _connectionStatus = status;
                    _rangeMeters = range;
                    _distanceMeters = distance;
                }
                else
                {
                    _connectionStatus = _vessel == null ? "Sin nave activa" : "CommNet no disponible";
                    _rangeMeters = 0d;
                    _distanceMeters = -1d;
                }
            }
            catch (Exception ex)
            {
                _connectionStatus = "Error leyendo CommNet";
                _log.LogError("[CommNextRedux] RefreshState: " + ex);
            }
        }

        private void OnGUI()
        {
            if (!_visible || !_isFlightScene || _vessel == null) return;
            _window = GUI.Window(728431, _window, DrawWindow, "CommNext Redux 0.0.11");
        }

        private void DrawWindow(int id)
        {
            GUILayout.Label("Nucleo CommNet Redux");
            GUILayout.Label("Nave: " + _vessel.DisplayName);
            GUILayout.Label("Conexion: " + _connectionStatus);
            GUILayout.Label("Rango antena: " + FormatDistance(_rangeMeters));
            GUILayout.Label("Distancia directa a KSC: " +
                (_distanceMeters < 0d ? "sin datos" : FormatDistance(_distanceMeters)));
            GUILayout.Label("Nodos CommNet: " + _nodeCount);
            GUILayout.Label("Rango origen/KSC: " + FormatDistance(CommNetBridge.SourceRangeMeters));
            GUILayout.Label("Bandas nave: " + CommNetBridge.GetBandSummary(_vessel));
            GUILayout.Label("Ruta CommNext: " + (_route.Connected ? "conectada" : _route.Reason));
            if (_route.Connected)
            {
                GUILayout.Label("Saltos: " + _route.Hops +
                    "   Recorrido: " + FormatDistance(_route.TotalDistanceMeters));
                GUILayout.Label(_route.Path);
            }
            GUILayout.Label("Manager: " + (CommNetBridge.IsAttached ? "conectado" : "pendiente"));
            GUILayout.Space(5f);
            GUILayout.Label("Alt+C: mostrar / ocultar");
            GUI.DragWindow(new Rect(0f, 0f, _window.width, 25f));
        }

        private static string FormatDistance(double meters)
        {
            if (meters < 0d) return "-";
            if (meters >= 1_000_000_000d) return (meters / 1_000_000_000d).ToString("F2") + " Gm";
            if (meters >= 1_000_000d) return (meters / 1_000_000d).ToString("F2") + " Mm";
            if (meters >= 1_000d) return (meters / 1_000d).ToString("F1") + " km";
            return meters.ToString("F0") + " m";
        }
    }
}
