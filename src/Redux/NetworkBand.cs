using System;
using UnityEngine;

namespace CommNextRedux.Network.Bands
{
    [Serializable]
    public sealed class NetworkBand
    {
        internal string Code { get; }
        internal string DisplayName { get; }
        internal Color Color { get; }

        internal NetworkBand(string code, string displayName, Color color)
        {
            Code = code;
            DisplayName = displayName;
            Color = color;
        }
    }
}
