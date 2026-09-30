using CommNext.Network;
using CommNext.Network.Compute;
using Unity.Collections;

namespace CommNext.Patches;

/// <summary>
/// Redux compatibility surface retained for the original renderer/UI.
/// The authoritative ConnectionGraph patching is implemented by CommNextRedux.
/// </summary>
public static class ConnectionGraphPatches
{
    public static NativeArray<NetworkJobConnection> Connections =>
        NetworkManager.Instance.ConnectionArray;
}
