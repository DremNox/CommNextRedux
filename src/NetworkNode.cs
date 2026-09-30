using System.Collections.Generic;
using KSP.Sim.impl;

namespace CommNextRedux.Network
{
    internal sealed class NetworkNode
    {
        internal IGGuid Owner { get; }
        internal bool IsRelay { get; set; }
        internal bool HasEnoughResources { get; set; }
        internal double[] BandRanges { get; private set; }
        internal string VesselName { get; set; }

        internal NetworkNode(IGGuid owner)
        {
            Owner = owner;
            HasEnoughResources = true;
            VesselName = "N/A";
            BandRanges = new double[NetworkBands.Instance.AllBands.Count];
        }

        internal void SetVanillaRange(double rangeMeters)
        {
            var index = NetworkBands.Instance.GetBandIndex(NetworkBands.DefaultBand);
            if (index >= 0)
                BandRanges[index] = rangeMeters;
        }

        internal void SetBandRanges(Dictionary<int, double> bandRanges)
        {
            BandRanges = new double[NetworkBands.Instance.AllBands.Count];
            for (var i = 0; i < BandRanges.Length; i++)
            {
                double value;
                BandRanges[i] = bandRanges.TryGetValue(i, out value) ? value : 0d;
            }
        }

        internal double GetBandRange(string bandCode)
        {
            var index = NetworkBands.Instance.GetBandIndex(bandCode);
            return index < 0 || index >= BandRanges.Length ? 0d : BandRanges[index];
        }
    }
}
