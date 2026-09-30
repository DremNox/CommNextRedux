using CommNext.Utils;
using HarmonyLib;
using KSP.Sim;
using KSP.Sim.Definitions;
using KSP.Sim.impl;

namespace CommNextRedux
{
    /// <summary>
    /// Optional strict signal-loss behavior.
    ///
    /// KSP2's stock NoCommNet state deliberately keeps a few commands available
    /// (notably throttle min/max and some autopilot/SAS commands). CommNext strict
    /// mode keeps the vessel selectable but rejects NEW flight commands while the
    /// probe is in NoCommNet.
    ///
    /// Crewed/local-control vessels are not affected because KSP2 keeps them in
    /// FullControl instead of NoCommNet.
    /// </summary>
    internal static class StrictControlGuard
    {
        internal static bool Enabled
        {
            get
            {
                try
                {
                    return PluginSettings.SignalLossControl != null &&
                           PluginSettings.SignalLossControl.Value ==
                           PluginSettings.SignalLossControlMode.Strict;
                }
                catch
                {
                    return false;
                }
            }
        }

        internal static bool ShouldBlock(VesselComponent vessel)
        {
            if (!Enabled || vessel == null)
                return false;

            // Let KSP2 decide whether this is actually a probe/command source that
            // requires CommNet. This also preserves local control for crewed vessels,
            // EVA and any future command module that does not require CommNet.
            return vessel.ControlStatus == VesselControlState.NoCommNet;
        }
    }

    [HarmonyPatch(typeof(VesselComponent), nameof(VesselComponent.HasControlForManualYawPitchRoll))]
    internal static class StrictManualControlPatch
    {
        [HarmonyPostfix]
        private static void Postfix(VesselComponent __instance, ref bool __result)
        {
            if (StrictControlGuard.ShouldBlock(__instance))
                __result = false;
        }
    }

    [HarmonyPatch(typeof(VesselComponent), nameof(VesselComponent.HasControlForThrottleDelta))]
    internal static class StrictThrottleDeltaPatch
    {
        [HarmonyPostfix]
        private static void Postfix(VesselComponent __instance, ref bool __result)
        {
            if (StrictControlGuard.ShouldBlock(__instance))
                __result = false;
        }
    }

    [HarmonyPatch(typeof(VesselComponent), nameof(VesselComponent.HasControlForThrottleMinMax))]
    internal static class StrictThrottleMinMaxPatch
    {
        [HarmonyPostfix]
        private static void Postfix(VesselComponent __instance, ref bool __result)
        {
            if (StrictControlGuard.ShouldBlock(__instance))
                __result = false;
        }
    }

    [HarmonyPatch(typeof(VesselComponent), nameof(VesselComponent.HasControlForEditingManeuvers))]
    internal static class StrictManeuverControlPatch
    {
        [HarmonyPostfix]
        private static void Postfix(VesselComponent __instance, ref bool __result)
        {
            if (StrictControlGuard.ShouldBlock(__instance))
                __result = false;
        }
    }

    [HarmonyPatch(typeof(VesselComponent), nameof(VesselComponent.HasControlForEditingStagingStack))]
    internal static class StrictStagingPermissionPatch
    {
        [HarmonyPostfix]
        private static void Postfix(VesselComponent __instance, ref bool __result)
        {
            if (StrictControlGuard.ShouldBlock(__instance))
                __result = false;
        }
    }

    [HarmonyPatch(typeof(VesselComponent), nameof(VesselComponent.ActivateNextStage))]
    internal static class StrictActivateNextStagePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(VesselComponent __instance)
        {
            return !StrictControlGuard.ShouldBlock(__instance);
        }
    }

    [HarmonyPatch(typeof(VesselComponent), nameof(VesselComponent.SetAutopilotMode))]
    internal static class StrictSetAutopilotModePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(VesselComponent __instance, ref bool __result)
        {
            if (!StrictControlGuard.ShouldBlock(__instance))
                return true;

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(VesselComponent), nameof(VesselComponent.SetAutopilotEnableDisable))]
    internal static class StrictSetAutopilotEnableDisablePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(VesselComponent __instance, ref bool __result)
        {
            if (!StrictControlGuard.ShouldBlock(__instance))
                return true;

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(VesselComponent), nameof(VesselComponent.TriggerActionGroup))]
    internal static class StrictTriggerActionGroupPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(VesselComponent __instance)
        {
            return !StrictControlGuard.ShouldBlock(__instance);
        }
    }

    [HarmonyPatch(typeof(VesselComponent), nameof(VesselComponent.SetActionGroup))]
    internal static class StrictSetActionGroupPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(VesselComponent __instance)
        {
            return !StrictControlGuard.ShouldBlock(__instance);
        }
    }

    [HarmonyPatch(typeof(VesselComponent), "set_IsRCSEnabled")]
    internal static class StrictSetRcsPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(VesselComponent __instance)
        {
            return !StrictControlGuard.ShouldBlock(__instance);
        }
    }
}
