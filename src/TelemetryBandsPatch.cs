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

                foreach (var part in partOwner.Parts)
                {
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

                    PartComponentModule_NextModulator modulator;
                    if (!part.TryGetModule<PartComponentModule_NextModulator>(out modulator))
                    {
                        // Stock/fallback behavior: use X band.
                        var xIndex = NetworkBands.Instance.GetBandIndex(NetworkBands.DefaultBand);
                        if (xIndex >= 0)
                            SetMax(bandRanges, xIndex, transmitter.CommunicationRangeMeters);
                        continue;
                    }

                    if (!transmitter.IsTransmitterActive())
                        continue;

                    var data = modulator.DataModulator;
                    if (data == null)
                        continue;

                    if (data.OmniBand.GetValue())
                    {
                        for (var i = 0; i < NetworkBands.Instance.AllBands.Count; i++)
                            SetMax(bandRanges, i, transmitter.CommunicationRangeMeters);
                        continue;
                    }

                    var primary = NetworkBands.Instance.GetBandIndex(data.Band.GetValue());
                    if (primary >= 0)
                        SetMax(bandRanges, primary, transmitter.CommunicationRangeMeters);

                    var secondary = NetworkBands.Instance.GetBandIndex(data.SecondaryBand.GetValue());
                    if (secondary >= 0)
                        SetMax(bandRanges, secondary, transmitter.CommunicationRangeMeters);
                }

                networkNode.IsRelay = isRelay;
                networkNode.HasEnoughResources = hasEnoughResources;
                networkNode.SetBandRanges(bandRanges);
                ManagedCommNextGraph.Invalidate();
            }
            catch (Exception ex)
            {
                CommNetBridge.Log?.LogError("[CommNextRedux] Telemetry band refresh: " + ex);
            }
        }

        private static void SetMax(Dictionary<int, double> values, int index, double range)
        {
            double current;
            if (!values.TryGetValue(index, out current) || range > current)
                values[index] = range;
        }
    }
}
