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
        private bool _isOperationalScene;
        private bool _showWindowScene;
        private string _lastRouteDebugKey = "";
        private float _nextRefresh;

        public override void OnPreInitialized()
        {
            _log = SWLogger;
            CommNetBridge.Log = _log;
            _log.LogInfo("[CommNextRedux] 0.0.15 pre-initialized");
        }

        public override void OnInitialized()
        {
            _harmony = new Harmony("DremNox.CommNextRedux");
            _harmony.PatchAll(typeof(CommNextReduxPlugin).Assembly);
            _log.LogInfo("[CommNextRedux] 0.0.15 initialized; KSC source-active fix and transmitter diagnostics active");
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

                _isOperationalScene = state != null &&
                    !state.IsObjectAssembly &&
                    (state.GameState == GameState.FlightView ||
                     state.GameState == GameState.Map3DView);

                _showWindowScene = state != null &&
                    state.GameState == GameState.FlightView &&
                    !state.IsObjectAssembly;

                _vessel = !_isOperationalScene || game == null || game.ViewController == null
                    ? null
                    : game.ViewController.GetActiveSimVessel(false);

                _nodeCount = CommNetBridge.NodeCount;
                _route = _isOperationalScene && _vessel != null
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

                LogRouteStateIfChanged();
            }
            catch (Exception ex)
            {
                _connectionStatus = "Error leyendo CommNet";
                _log.LogError("[CommNextRedux] RefreshState: " + ex);
            }
        }

        private void OnGUI()
        {
            if (!_visible || !_showWindowScene || _vessel == null) return;
            _window = GUI.Window(728431, _window, DrawWindow, "CommNext Redux 0.0.15");
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
            GUILayout.Label("Antenas: " + CommNetBridge.GetTransmitterSummary(_vessel));
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

        private void LogRouteStateIfChanged()
        {
            if (_vessel == null || CommNetBridge.Manager == null)
                return;

            try
            {
                var source = CommNetBridge.Manager.GetSourceNode();
                var target = _vessel.SimulationObject == null
                    ? null
                    : _vessel.SimulationObject.Telemetry == null
                        ? null
                        : _vessel.SimulationObject.Telemetry.CommNetNode;

                var sourceRange = source == null ? 0d : source.MaxRange;
                var targetRange = target == null ? 0d : target.MaxRange;
                var sourceActive = source != null && source.IsActive;
                var sourceControl = source != null && source.IsControlSource;
                var targetActive = target != null && target.IsActive;
                var bands = CommNetBridge.GetBandSummary(_vessel);
                var transmitters = CommNetBridge.GetTransmitterSummary(_vessel);

                var key = (_route.Connected ? "C" : "D") + "|" +
                    _route.Reason + "|" +
                    sourceRange.ToString("F0") + "|" +
                    targetRange.ToString("F0") + "|" +
                    sourceActive + "|" +
                    sourceControl + "|" +
                    targetActive + "|" +
                    bands;

                if (key == _lastRouteDebugKey)
                    return;

                _lastRouteDebugKey = key;
                _log.LogInfo(
                    "[CommNextRedux][Route] connected=" + _route.Connected +
                    " reason=" + _route.Reason +
                    " sourceRange=" + sourceRange.ToString("F0") +
                    " targetRange=" + targetRange.ToString("F0") +
                    " sourceActive=" + sourceActive +
                    " sourceControl=" + sourceControl +
                    " targetActive=" + targetActive +
                    " bands=" + bands +
                    " transmitters=" + transmitters +
                    " directDistance=" + _distanceMeters.ToString("F0") +
                    " nodes=" + _nodeCount);
            }
            catch (Exception ex)
            {
                _log.LogError("[CommNextRedux] Route diagnostics: " + ex);
            }
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
