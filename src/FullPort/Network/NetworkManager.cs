using BepInEx.Logging;
using CommNextRedux.Managers;
using CommNextRedux.Network.Compute;
using KSP.Game;
using KSP.Messages;
using KSP.Sim;
using KSP.Sim.Definitions;
using KSP.Sim.impl;
using Unity.Collections;

namespace CommNextRedux.Network;

/// <summary>
/// Redux-compatible facade that preserves the public surface of the original
/// CommNext NetworkManager while delegating lifecycle and routing to the
/// Redux-native CommNetBridge/ManagedCommNextGraph.
/// </summary>
public class NetworkManager : ILateUpdate
{
    private static readonly ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource("CommNextRedux.NetworkManager");

    public static NetworkManager Instance { get; private set; } = new();

    private bool _isInitialized;
    private CommNetManager? _commNetManager;
    private NativeArray<NetworkJobConnection> _cachedConnections;

    public CommNetManager CommNetManager => _commNetManager ?? CommNetBridge.Manager;

    /// <summary>
    /// Expose the Redux node registry through the same API expected by the
    /// original UI and renderer.
    /// </summary>
    public Dictionary<IGGuid, NetworkNode> Nodes => CommNetBridge.Nodes;

    public void SetupListeners()
    {
        MessageListener.Messages.PersistentSubscribe<VesselUndockedMessage>(OnVesselUndocked);
        MessageListener.Messages.PersistentSubscribe<VesselDockedMessage>(OnVesselDocked);
        MessageListener.Messages.PersistentSubscribe<DecoupleMessage>(OnDecouple);
    }

    public void Initialize(CommNetManager commNetManager)
    {
        _commNetManager = commNetManager;
        _isInitialized = true;
        ManagedCommNextGraph.Invalidate();
        Logger.LogInfo("Initialized against Redux CommNetManager");
    }

    public void Shutdown()
    {
        _isInitialized = false;
        _commNetManager = null;
        ManagedCommNextGraph.Invalidate();
        if (_cachedConnections.IsCreated)
            _cachedConnections.Dispose();
    }

    public void RegisterNode(NetworkNode node)
    {
        if (node == null) return;
        CommNetBridge.Nodes[node.Owner] = node;
        ManagedCommNextGraph.Invalidate();
    }

    public void UnregisterNode(IGGuid owner)
    {
        CommNetBridge.Nodes.Remove(owner);
        ManagedCommNextGraph.Invalidate();
    }

    private static void OnVesselUndocked(MessageCenterMessage message)
    {
        var m = (VesselUndockedMessage)message;
        if (m.VesselOne?.SimObjectComponent?.SimulationObject?.Telemetry != null)
            CommNetBridge.RefreshTelemetry(m.VesselOne.SimObjectComponent.SimulationObject.Telemetry);
        if (m.VesselTwo?.SimObjectComponent?.SimulationObject?.Telemetry != null)
            CommNetBridge.RefreshTelemetry(m.VesselTwo.SimObjectComponent.SimulationObject.Telemetry);
    }

    private static void OnVesselDocked(MessageCenterMessage message)
    {
        var m = (VesselDockedMessage)message;
        if (m.VesselOne?.SimObjectComponent?.SimulationObject?.Telemetry != null)
            CommNetBridge.RefreshTelemetry(m.VesselOne.SimObjectComponent.SimulationObject.Telemetry);
        if (m.VesselTwo?.SimObjectComponent?.SimulationObject?.Telemetry != null)
            CommNetBridge.RefreshTelemetry(m.VesselTwo.SimObjectComponent.SimulationObject.Telemetry);
    }

    private static void OnDecouple(MessageCenterMessage message)
    {
        var m = (DecoupleMessage)message;
        var vessel = GameManager.Instance?.Game?.UniverseModel?.FindVesselComponent(m.VesselGuid);
        if (vessel?.SimulationObject?.Telemetry != null)
            CommNetBridge.RefreshTelemetry(vessel.SimulationObject.Telemetry);
    }

    /// <summary>
    /// Redux owns its own graph update loop. This method intentionally does not
    /// rebuild private game fields; it only keeps the compatibility interface.
    /// </summary>
    public void OnLateUpdate()
    {
        // No-op by design. CommNetBridge hooks Initialize/Shutdown/RegisterNode
        // and ManagedCommNextGraph computes the CommNext-aware paths.
    }

    /// <summary>
    /// Provides a snapshot shaped like the original ConnectionGraph output so
    /// the original CommNext renderer/UI can keep operating.
    /// </summary>
    public bool TryGetConnectionGraphNodesAndIndexes(
        out List<ConnectionGraphNode>? nodes,
        out int[] prevIndexes,
        out NativeArray<NetworkJobConnection>? connections)
    {
        var graphNodes = CommNetBridge.GetGraphNodes();
        if (CommNetManager == null || graphNodes.Count == 0)
        {
            nodes = null;
            prevIndexes = Array.Empty<int>();
            connections = null;
            return false;
        }

        nodes = graphNodes;
        var count = graphNodes.Count;
        prevIndexes = new int[count];
        for (var i = 0; i < prevIndexes.Length; i++)
            prevIndexes[i] = -1;

        if (_cachedConnections.IsCreated)
            _cachedConnections.Dispose();
        _cachedConnections = new NativeArray<NetworkJobConnection>(
            count * count,
            Allocator.Persistent,
            NativeArrayOptions.ClearMemory);

        var indexByOwner = new Dictionary<IGGuid, int>();
        for (var i = 0; i < count; i++)
        {
            indexByOwner[graphNodes[i].Owner] = i;
            CommNetBridge.RegisterOrUpdateNode(graphNodes[i]);
        }

        var source = CommNetManager.GetSourceNode();
        if (source != null && indexByOwner.TryGetValue(source.Owner, out var sourceIndex))
            prevIndexes[sourceIndex] = sourceIndex;

        // Build the active predecessor tree from the same route calculator
        // that drives the authoritative connection status.
        for (var targetIndex = 0; targetIndex < count; targetIndex++)
        {
            var owner = graphNodes[targetIndex].Owner;
            if (source != null && owner == source.Owner)
                continue;

            var vessel = GameManager.Instance?.Game?.UniverseModel?.FindVesselComponent(owner);
            if (vessel == null)
                continue;

            var route = ManagedCommNextGraph.BuildRoute(vessel);
            if (!route.Evaluated || route.RouteOwners.Count < 2)
                continue;

            for (var edge = 0; edge < route.RouteOwners.Count - 1; edge++)
            {
                if (!indexByOwner.TryGetValue(route.RouteOwners[edge], out var a) ||
                    !indexByOwner.TryGetValue(route.RouteOwners[edge + 1], out var b))
                    continue;

                prevIndexes[b] = a;

                var job = BuildJobConnection(
                    graphNodes[a],
                    graphNodes[b],
                    edge < route.RouteBands.Count ? route.RouteBands[edge] : -1,
                    route.Connected);

                _cachedConnections[b * count + a] = job;
                _cachedConnections[a * count + b] = job;
            }
        }

        connections = _cachedConnections;
        return true;
    }

    public List<NetworkConnection> GetNodeConnections(
        NetworkNode networkNode,
        VesselNodesFilter nodesFilter = VesselNodesFilter.InRange)
    {
        var result = new List<NetworkConnection>();
        if (networkNode == null) return result;

        var graphNodes = CommNetBridge.GetGraphNodes();
        var sourceGraphNode = graphNodes.FirstOrDefault(n => n.Owner == networkNode.Owner);
        if (sourceGraphNode == null)
            return result;

        foreach (var targetGraphNode in graphNodes)
        {
            if (targetGraphNode == null || targetGraphNode.Owner == networkNode.Owner)
                continue;

            if (!Nodes.TryGetValue(targetGraphNode.Owner, out var targetNetworkNode))
                continue;

            var distance = Math.Sqrt(global::Unity.Mathematics.math.distancesq(
                sourceGraphNode.Position,
                targetGraphNode.Position));

            var selectedBand = FindMatchingBand(networkNode, targetNetworkNode, distance);
            var inRange = selectedBand >= 0 &&
                          distance <= sourceGraphNode.MaxRange &&
                          distance <= targetGraphNode.MaxRange;

            var targetVessel = GameManager.Instance?.Game?.UniverseModel?.FindVesselComponent(targetGraphNode.Owner);
            var targetRoute = targetVessel == null ? null : ManagedCommNextGraph.BuildRoute(targetVessel);
            var active = targetRoute != null &&
                         targetRoute.RouteOwners.Count > 1 &&
                         ContainsEdge(targetRoute.RouteOwners, networkNode.Owner, targetGraphNode.Owner);

            var connected = inRange &&
                            networkNode.HasEnoughResources &&
                            targetNetworkNode.HasEnoughResources;

            var job = new NetworkJobConnection
            {
                IsInRange = inRange,
                IsConnected = connected,
                IsOccluded = false,
                OccludingBody = -1,
                HasMatchingBand = selectedBand >= 0,
                SelectedBand = (short)selectedBand,
                IsBandMissingRange = selectedBand < 0
            };

            var shouldAdd = nodesFilter switch
            {
                VesselNodesFilter.All => true,
                VesselNodesFilter.Active => active,
                VesselNodesFilter.Connected => connected,
                VesselNodesFilter.InRange => inRange,
                _ => false
            };

            if (!shouldAdd) continue;

            result.Add(new NetworkConnection(
                networkNode,
                targetNetworkNode,
                sourceGraphNode,
                targetGraphNode,
                job,
                active));
        }

        return result;
    }

    public bool TryGetNetworkPath(
        IGGuid targetId,
        List<ConnectionGraphNode> graphNodes,
        int[] prevIndexes,
        out HashSet<(int, int)>? path)
    {
        path = null;
        if (!_isInitialized && CommNetBridge.Manager == null)
            return false;

        var vessel = GameManager.Instance?.Game?.UniverseModel?.FindVesselComponent(targetId);
        if (vessel == null)
            return false;

        var route = ManagedCommNextGraph.BuildRoute(vessel);
        if (!route.Connected || route.RouteOwners.Count < 2)
            return false;

        var indexByOwner = new Dictionary<IGGuid, int>();
        for (var i = 0; i < graphNodes.Count; i++)
            indexByOwner[graphNodes[i].Owner] = i;

        var result = new HashSet<(int, int)>();
        for (var i = 0; i < route.RouteOwners.Count - 1; i++)
        {
            if (!indexByOwner.TryGetValue(route.RouteOwners[i], out var a) ||
                !indexByOwner.TryGetValue(route.RouteOwners[i + 1], out var b))
                continue;
            result.Add((a, b));
        }

        path = result;
        return result.Count > 0;
    }

    private static NetworkJobConnection BuildJobConnection(
        ConnectionGraphNode source,
        ConnectionGraphNode target,
        int band,
        bool connected)
    {
        var distance = Math.Sqrt(global::Unity.Mathematics.math.distancesq(source.Position, target.Position));
        return new NetworkJobConnection
        {
            IsInRange = distance <= source.MaxRange && distance <= target.MaxRange,
            IsConnected = connected,
            IsOccluded = !connected,
            OccludingBody = -1,
            HasMatchingBand = band >= 0,
            SelectedBand = (short)band,
            IsBandMissingRange = band < 0
        };
    }

    private static int FindMatchingBand(NetworkNode a, NetworkNode b, double distance)
    {
        var length = Math.Min(a.BandRanges.Length, b.BandRanges.Length);
        for (var i = 0; i < length; i++)
        {
            if (a.BandRanges[i] >= distance && b.BandRanges[i] >= distance)
                return i;
        }
        return -1;
    }

    private static bool ContainsEdge(List<IGGuid> owners, IGGuid a, IGGuid b)
    {
        for (var i = 0; i < owners.Count - 1; i++)
        {
            if ((owners[i] == a && owners[i + 1] == b) ||
                (owners[i] == b && owners[i + 1] == a))
                return true;
        }
        return false;
    }
}

