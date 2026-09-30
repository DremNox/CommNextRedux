using System.Collections.Generic;
using UnityEngine;

namespace CommNextRedux.Network
{
    internal sealed class NetworkBands
    {
        internal static readonly NetworkBands Instance = new NetworkBands();
        internal const string DefaultBand = "X";

        internal readonly List<NetworkBand> AllBands;
        internal readonly Dictionary<string, int> BandIndexByCode;

        private NetworkBands()
        {
            AllBands = new List<NetworkBand>
            {
                new NetworkBand("X", "X Band", new Color(0.174f, 0.783f, 0.777f, 1f)),
                new NetworkBand("S", "S Band", new Color(0.09f, 0.57f, 0.97f, 1f)),
                new NetworkBand("K", "K Band", new Color(0.44f, 0.39f, 1f, 1f)),
                new NetworkBand("Ka", "Ka Band", new Color(0.84f, 0.15f, 0.92f, 1f)),
                new NetworkBand("V", "V Band", new Color(0.17f, 0.85f, 0.58f, 1f))
            };

            BandIndexByCode = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < AllBands.Count; i++)
                BandIndexByCode[AllBands[i].Code] = i;
        }

        internal int GetBandIndex(string bandCode)
        {
            int index;
            return BandIndexByCode.TryGetValue(bandCode, out index) ? index : -1;
        }
    }
}
