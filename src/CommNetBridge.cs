using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
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

        internal static int NodeCount
        {
            get
            {
                try
                {
                    if (Manager == null || AllNodesField == null) return 0;
                    var collection = AllNodesField.GetValue(Manager) as ICollection;
                    return collection == null ? 0 : collection.Count;
                }
                catch
                {
                    return 0;
                }
            }
        }

        internal static void Attach(CommNetManager manager)
        {
            Manager = manager;
            Log?.LogInfo("[CommNextRedux] Attached to CommNetManager");
        }

        internal static void Detach(CommNetManager manager)
        {
            if (ReferenceEquals(Manager, manager))
            {
                Manager = null;
                Log?.LogInfo("[CommNextRedux] Detached from CommNetManager");
            }
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

                if (Manager != null)
                    distanceMeters = Manager.GetConnectionDistance(vessel.GlobalId);

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
                CommNetBridge.Log?.LogInfo("[CommNextRedux] KSC CommNet source corrected; range 2 Gm");
            }
            catch (Exception ex)
            {
                CommNetBridge.Log?.LogError("[CommNextRedux] KSC source patch: " + ex);
            }
        }
    }
}
