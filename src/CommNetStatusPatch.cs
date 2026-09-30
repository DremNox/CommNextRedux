using HarmonyLib;
using KSP.Game;
using KSP.Sim;
using KSP.Sim.impl;

namespace CommNextRedux
{
    [HarmonyPatch(typeof(CommNetManager), nameof(CommNetManager.GetConnectionStatus))]
    internal static class CommNextConnectionStatusPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(IGGuid __0, ref ConnectionNodeStatus __result)
        {
            try
            {
                var manager = CommNetBridge.Manager;
                if (manager == null)
                    return true;

                var source = manager.GetSourceNode();
                if (source != null && source.Owner == __0)
                {
                    __result = ConnectionNodeStatus.Connected;
                    return false;
                }

                var game = GameManager.Instance == null ? null : GameManager.Instance.Game;
                var vessel = game == null ? null : game.UniverseModel.FindVesselComponent(__0);
                if (vessel == null)
                    return true;

                var route = ManagedCommNextGraph.BuildRoute(vessel);
                if (!route.Evaluated)
                    return true;

                __result = route.Connected
                    ? ConnectionNodeStatus.Connected
                    : ConnectionNodeStatus.Disconnected;

                return false;
            }
            catch
            {
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(CommNetManager), nameof(CommNetManager.GetConnectionDistance))]
    internal static class CommNextConnectionDistancePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(IGGuid __0, ref double __result)
        {
            try
            {
                var manager = CommNetBridge.Manager;
                if (manager == null)
                    return true;

                var source = manager.GetSourceNode();
                if (source != null && source.Owner == __0)
                {
                    __result = 0d;
                    return false;
                }

                var game = GameManager.Instance == null ? null : GameManager.Instance.Game;
                var vessel = game == null ? null : game.UniverseModel.FindVesselComponent(__0);
                if (vessel == null)
                    return true;

                var route = ManagedCommNextGraph.BuildRoute(vessel);
                if (!route.Evaluated)
                    return true;

                __result = route.Connected ? route.GraphCost : double.MaxValue;
                return false;
            }
            catch
            {
                return true;
            }
        }
    }
}
