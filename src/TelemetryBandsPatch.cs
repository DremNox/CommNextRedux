using System;
using System.Collections.Generic;
using HarmonyLib;
using CommNextRedux.Modules;
using CommNextRedux.Network;
using KSP.Modules;
using KSP.Sim.impl;

namespace CommNextRedux
{
    [HarmonyPatch(typeof(TelemetryComponent), "RefreshCommNetNode")]
    internal static class TelemetryBandsPatch
    {
        [HarmonyPostfix]
        private static void Postfix(TelemetryComponent __instance)
        {
            try
            {
                if (__instance == null || __instance.SimulationObject == null)
                    return;

                var graphNode = __instance.CommNetNode;
                if (graphNode == null)
                    return;

                CommNetBridge.RegisterOrUpdateNode(graphNode);

                NetworkNode networkNode;
                if (!CommNetBridge.Nodes.TryGetValue(__instance.GlobalId, out networkNode))
                    return;

                var partOwner = __instance.SimulationObject.PartOwner;
                if (partOwner == null)
                    return;

                var isRelay = false;
                var hasEnoughResources = true;
                var bandRanges = new Dictionary<int, double>();
                var transmitterStates = new List<string>();

                foreach (var part in partOwner.Parts)
                {
                    if (IsStockRelayPart(part.PartName))
                        isRelay = true;

                    Data_NextRelay relayData;
                    if (part.TryGetModuleData<PartComponentModule_NextRelay, Data_NextRelay>(out relayData))
                    {
                        var enabled = relayData.EnableRelay.GetValue();
                        isRelay |= enabled;
                        if (enabled)
                            hasEnoughResources &= relayData.HasResourcesToOperate;
                    }

                    PartComponentModule_DataTransmitter transmitter;
                    if (!part.TryGetModule<PartComponentModule_DataTransmitter>(out transmitter))
                        continue;

                    var effectiveRange = GetCommNextRange(part.PartName, transmitter.CommunicationRangeMeters);
                    var transmitterActive = transmitter.IsTransmitterActive();
                    transmitterStates.Add(
                        part.PartName + "=" + (transmitterActive ? "ON" : "OFF") +
                        "@" + FormatRange(effectiveRange));

                    PartComponentModule_NextModulator modulator;
                    if (!part.TryGetModule<PartComponentModule_NextModulator>(out modulator))
                    {
                        // Stock/fallback behavior: active stock transmitters use X band.
                        if (!transmitterActive)
                            continue;

                        var xIndex = NetworkBands.Instance.GetBandIndex(NetworkBands.DefaultBand);
                        if (xIndex >= 0)
                            SetMax(bandRanges, xIndex, effectiveRange);
                        continue;
                    }

                    if (!transmitterActive)
                        continue;

                    var data = modulator.DataModulator;
                    if (data == null)
                        continue;

                    if (data.OmniBand.GetValue())
                    {
                        for (var i = 0; i < NetworkBands.Instance.AllBands.Count; i++)
                            SetMax(bandRanges, i, effectiveRange);
                        continue;
                    }

                    var primary = NetworkBands.Instance.GetBandIndex(data.Band.GetValue());
                    if (primary >= 0)
                        SetMax(bandRanges, primary, effectiveRange);

                    var secondary = NetworkBands.Instance.GetBandIndex(data.SecondaryBand.GetValue());
                    if (secondary >= 0)
                        SetMax(bandRanges, secondary, effectiveRange);
                }

                networkNode.IsRelay = isRelay;
                networkNode.HasEnoughResources = hasEnoughResources;
                networkNode.TransmitterSummary = transmitterStates.Count == 0
                    ? "sin transmisores"
                    : string.Join(" | ", transmitterStates.ToArray());
                networkNode.SetBandRanges(bandRanges);

                var effectiveNodeRange = 0d;
                foreach (var range in bandRanges.Values)
                    if (range > effectiveNodeRange)
                        effectiveNodeRange = range;

                if (effectiveNodeRange > 0d)
                    graphNode.MaxRange = effectiveNodeRange;

                ManagedCommNextGraph.Invalidate();
            }
            catch (Exception ex)
            {
                CommNetBridge.Log?.LogError("[CommNextRedux] Telemetry band refresh: " + ex);
            }
        }

        private static string FormatRange(double meters)
        {
            if (meters >= 1_000_000_000d) return (meters / 1_000_000_000d).ToString("F2") + "Gm";
            if (meters >= 1_000_000d) return (meters / 1_000_000d).ToString("F2") + "Mm";
            if (meters >= 1_000d) return (meters / 1_000d).ToString("F1") + "km";
            return meters.ToString("F0") + "m";
        }

        private static double GetCommNextRange(string partName, double stockRange)
        {
            if (string.IsNullOrEmpty(partName))
                return stockRange;

            if (partName.Equals("antenna_0v_16", StringComparison.OrdinalIgnoreCase) ||
                partName.Equals("antenna_0v_16s", StringComparison.OrdinalIgnoreCase))
                return 500_000d;

            if (partName.Equals("antenna_1v_dish_hg5", StringComparison.OrdinalIgnoreCase))
                return 5_000_000d;

            if (partName.Equals("antenna_0v_dish_ra-2", StringComparison.OrdinalIgnoreCase) ||
                partName.Equals("antenna_1v_parabolic_dts-m1", StringComparison.OrdinalIgnoreCase))
                return 2_000_000_000d;

            if (partName.Equals("antenna_0v_dish_ra-15", StringComparison.OrdinalIgnoreCase) ||
                partName.Equals("antenna_1v_dish_hg55", StringComparison.OrdinalIgnoreCase) ||
                partName.Equals("antenna_1v_dish_hg55s", StringComparison.OrdinalIgnoreCase))
                return 15_000_000_000d;

            if (partName.Equals("antenna_1v_dish_ra-100", StringComparison.OrdinalIgnoreCase) ||
                partName.Equals("antenna_1v_dish_88-88", StringComparison.OrdinalIgnoreCase))
                return 100_000_000_000d;

            return stockRange;
        }

        private static bool IsStockRelayPart(string partName)
        {
            if (string.IsNullOrEmpty(partName))
                return false;

            return partName.Equals("antenna_1v_dish_hg5", StringComparison.OrdinalIgnoreCase) ||
                   partName.Equals("antenna_0v_dish_ra-2", StringComparison.OrdinalIgnoreCase) ||
                   partName.Equals("antenna_0v_dish_ra-15", StringComparison.OrdinalIgnoreCase) ||
                   partName.Equals("antenna_1v_dish_ra-100", StringComparison.OrdinalIgnoreCase);
        }

        private static void SetMax(Dictionary<int, double> values, int index, double range)
        {
            double current;
            if (!values.TryGetValue(index, out current) || range > current)
                values[index] = range;
        }
    }
}
