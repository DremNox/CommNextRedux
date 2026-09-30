using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using CommNextRedux.Network;
using KSP.Game;
using KSP.Sim;
using KSP.Sim.impl;
using ReduxLib.Logging;

namespace CommNextRedux
{
    internal static class CommNetBridge
    {
        private static readonly FieldInfo AllNodesField =
            typeof(CommNetManager).GetField("_allNodes",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        internal static CommNetManager Manager { get; private set; }
        internal static ILogger Log { get; set; }
        internal static readonly Dictionary<IGGuid, NetworkNode> Nodes = new Dictionary<IGGuid, NetworkNode>();

        internal static bool IsAttached => Manager != null;

        internal static double SourceRangeMeters
        {
            get
            {
                try
                {
                    var source = Manager == null ? null : Manager.GetSourceNode();
                    return source == null ? 0d : source.MaxRange;
                }
                catch
                {
                    return 0d;
                }
            }
        }

        internal static int NodeCount => Nodes.Count;

        internal static void Attach(CommNetManager manager)
        {
            Manager = manager;
            Nodes.Clear();

            try
            {
                var enumerable = AllNodesField == null ? null : AllNodesField.GetValue(manager) as IEnumerable;
                if (enumerable != null)
                {
                    foreach (var item in enumerable)
                    {
                        var node = item as ConnectionGraphNode;
                        if (node != null) RegisterOrUpdateNode(node);
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogError("[CommNextRedux] Initial CommNet node import: " + ex);
            }

            Log?.LogInfo("[CommNextRedux] Attached to CommNetManager; nodes=" + Nodes.Count);
        }

        internal static void Detach(CommNetManager manager)
        {
            if (ReferenceEquals(Manager, manager))
            {
                Manager = null;
                Nodes.Clear();
                Log?.LogInfo("[CommNextRedux] Detached from CommNetManager");
            }
        }

        internal static void RegisterOrUpdateNode(ConnectionGraphNode graphNode, string forcedName = null)
        {
            if (graphNode == null) return;

            try
            {
                NetworkNode node;
                if (!Nodes.TryGetValue(graphNode.Owner, out node))
                {
                    node = new NetworkNode(graphNode.Owner);
                    Nodes[graphNode.Owner] = node;
                }

                node.SetVanillaRange(graphNode.MaxRange);

                if (!string.IsNullOrWhiteSpace(forcedName))
                {
                    node.VesselName = forcedName;
                    return;
                }

                if (GameManager.Instance == null || GameManager.Instance.Game == null) return;
                var vessel = GameManager.Instance.Game.UniverseModel.FindVesselComponent(graphNode.Owner);
                if (vessel != null)
                    node.VesselName = vessel.Name;
            }
            catch (Exception ex)
            {
                Log?.LogError("[CommNextRedux] Register node: " + ex);
            }
        }

        internal static void UnregisterNode(ConnectionGraphNode graphNode)
        {
            if (graphNode == null) return;
            Nodes.Remove(graphNode.Owner);
        }

        internal static string GetBandSummary(VesselComponent vessel)
        {
            if (vessel == null) return "Sin bandas";

            NetworkNode node;
            if (!Nodes.TryGetValue(vessel.GlobalId, out node))
                return "X: pendiente";

            var range = node.GetBandRange(NetworkBands.DefaultBand);
            return "X: " + FormatDistance(range);
        }

        private static string FormatDistance(double meters)
        {
            if (meters >= 1_000_000_000d) return (meters / 1_000_000_000d).ToString("F2") + " Gm";
            if (meters >= 1_000_000d) return (meters / 1_000_000d).ToString("F2") + " Mm";
            if (meters >= 1_000d) return (meters / 1_000d).ToString("F1") + " km";
            return meters.ToString("F0") + " m";
        }

        internal static bool TryGetVesselInfo(
            VesselComponent vessel,
            out string connectionStatus,
            out double rangeMeters,
            out double distanceMeters)
        {
            connectionStatus = "Sin datos";
            rangeMeters = 0d;
            distanceMeters = -1d;

            if (vessel == null) return false;

            try
            {
                var telemetry = vessel.SimulationObject == null
                    ? null
                    : vessel.SimulationObject.Telemetry;

                if (telemetry != null)
                {
                    connectionStatus = telemetry.CommNetConnectionStatus.ToString();
                    rangeMeters = telemetry.CommNetRangeMeters;
                }

                if (Manager != null && telemetry != null && telemetry.CommNetNode != null)
                {
                    var source = Manager.GetSourceNode();
                    if (source != null)
                    {
                        var a = telemetry.CommNetNode.Position;
                        var b = source.Position;
                        var dx = a.x - b.x;
                        var dy = a.y - b.y;
                        var dz = a.z - b.z;
                        distanceMeters = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    }
                }

                return telemetry != null;
            }
            catch (Exception ex)
            {
                Log?.LogError("[CommNextRedux] CommNet vessel info: " + ex);
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(CommNetManager), nameof(CommNetManager.Initialize))]
    internal static class CommNetInitializePatch
    {
        [HarmonyPostfix]
        private static void Postfix(CommNetManager __instance)
        {
            CommNetBridge.Attach(__instance);
        }
    }

    [HarmonyPatch(typeof(CommNetManager), nameof(CommNetManager.Shutdown))]
    internal static class CommNetShutdownPatch
    {
        [HarmonyPrefix]
        private static void Prefix(CommNetManager __instance)
        {
            CommNetBridge.Detach(__instance);
        }
    }

    [HarmonyPatch(typeof(CommNetManager), nameof(CommNetManager.RegisterNode))]
    internal static class CommNetRegisterNodePatch
    {
        [HarmonyPostfix]
        private static void Postfix(ConnectionGraphNode __0)
        {
            CommNetBridge.RegisterOrUpdateNode(__0);
        }
    }

    [HarmonyPatch(typeof(CommNetManager), nameof(CommNetManager.UnregisterNode))]
    internal static class CommNetUnregisterNodePatch
    {
        [HarmonyPostfix]
        private static void Postfix(ConnectionGraphNode __0)
        {
            CommNetBridge.UnregisterNode(__0);
        }
    }

    [HarmonyPatch(typeof(CommNetManager), nameof(CommNetManager.SetSourceNode), new Type[] { typeof(ConnectionGraphNode) })]
    internal static class CommNetSourceNodePatch
    {
        private const string KerbinCommNetOriginName = "kerbin_CommNetOrigin";
        private const string KerbinSpaceCenterName = "kerbin_KSC_Object";
        private const double DefaultKscRangeMeters = 2_000_000_000d;

        [HarmonyPostfix]
        private static void Postfix(ConnectionGraphNode newSourceNode)
        {
            try
            {
                if (newSourceNode == null || GameManager.Instance == null || GameManager.Instance.Game == null)
                    return;

                var universe = GameManager.Instance.Game.UniverseModel;
                var sourceObject = universe.FindSimObject(newSourceNode.Owner);
                if (sourceObject == null || sourceObject.Name != KerbinCommNetOriginName)
                    return;

                var kscObject = universe.FindSimObjectByNameKey(KerbinSpaceCenterName);
                if (kscObject == null)
                {
                    CommNetBridge.Log?.LogWarning("[CommNextRedux] KSC SimObject not found");
                    return;
                }

                sourceObject.transform.Position = kscObject.transform.Position;
                newSourceNode.MaxRange = DefaultKscRangeMeters;
                CommNetBridge.RegisterOrUpdateNode(newSourceNode, "KSC");
                CommNetBridge.Log?.LogInfo("[CommNextRedux] KSC CommNet source corrected; range 2 Gm");
            }
            catch (Exception ex)
            {
                CommNetBridge.Log?.LogError("[CommNextRedux] KSC source patch: " + ex);
            }
        }
    }
}
