using System;
using System.Collections.Generic;
using System.Linq;
using CommNext.Network.Compute;
using KSP.Game;
using KSP.Sim;
using KSP.Sim.impl;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace CommNext.Network;

/// <summary>
/// Redux compatibility facade for the original CommNext NetworkManager API.
/// The original UI/rendering code talks to this class, while the actual
/// authoritative network is CommNextRedux.CommNetBridge / ManagedCommNextGraph.
/// </summary>
public class NetworkManager
{
    public static NetworkManager Instance { get; } = new NetworkManager();

    private CommNetManager? _commNetManager;
    private readonly List<ConnectionGraphNode> _graphNodes = new List<ConnectionGraphNode>();
    private int[] _previousIndices = Array.Empty<int>();
    private NativeArray<NetworkJobConnection> _connections;
    private float _nextSync;

    public Dictionary<IGGuid, NetworkNode> Nodes { get; } =
        new Dictionary<IGGuid, NetworkNode>();

    public CommNetManager CommNetManager =>
        _commNetManager ?? CommNextRedux.CommNetBridge.Manager;

    internal NativeArray<NetworkJobConnection> ConnectionArray => _connections;

    private NetworkManager() { }

    public void SetupListeners()
    {
        // Redux already refreshes the CommNet graph on dock/undock/decouple.
        // The authoritative bridge listens to Telemetry/CommNet lifecycle.
    }

    public void Initialize(CommNetManager commNetManager)
    {
        _commNetManager = commNetManager;
        Sync(true);
    }

    public void Shutdown()
    {
        _commNetManager = null;
        Nodes.Clear();
        _graphNodes.Clear();
        _previousIndices = Array.Empty<int>();
        if (_connections.IsCreated)
            _connections.Dispose();
    }

    public void RegisterNode(NetworkNode node)
    {
        Nodes[node.Owner] = node;
    }

    public void UnregisterNode(IGGuid owner)
    {
        Nodes.Remove(owner);
    }

    private void Sync(bool force = false)
    {
        var manager = CommNextRedux.CommNetBridge.Manager;
        if (manager == null)
            return;

        _commNetManager = manager;

        if (!force && Time.unscaledTime < _nextSync)
            return;

        _nextSync = Time.unscaledTime + 0.2f;

        var graphNodes = CommNextRedux.CommNetBridge.GetGraphNodes();
        _graphNodes.Clear();
        _graphNodes.AddRange(graphNodes);

        SyncNodes();

        var count = _graphNodes.Count;
        _previousIndices = Enumerable.Repeat(-1, count).ToArray();

        var requiredConnections = count * count;
        if (!_connections.IsCreated || _connections.Length != requiredConnections)
        {
            if (_connections.IsCreated)
                _connections.Dispose();

            _connections = requiredConnections == 0
                ? default
                : new NativeArray<NetworkJobConnection>(
                    requiredConnections,
                    Allocator.Persistent,
                    NativeArrayOptions.ClearMemory);
        }

        if (count == 0)
            return;

        var source = manager.GetSourceNode();
        var sourceIndex = source == null ? -1 : FindNodeIndex(source.Owner);

        var bodies = BuildBodies(source);

        for (var sourceNodeIndex = 0; sourceNodeIndex < count; sourceNodeIndex++)
        {
            for (var targetNodeIndex = 0; targetNodeIndex < count; targetNodeIndex++)
            {
                var flatIndex = sourceNodeIndex * count + targetNodeIndex;

                if (sourceNodeIndex == targetNodeIndex)
                {
                    _connections[flatIndex] = default;
                    continue;
                }

                _connections[flatIndex] = EvaluateConnection(
                    _graphNodes[sourceNodeIndex],
                    _graphNodes[targetNodeIndex],
                    bodies);
            }
        }

        // Rebuild the "previous" indices from the authoritative CommNext route.
        if (sourceIndex >= 0)
        {
            foreach (var graphNode in _graphNodes)
            {
                if (graphNode.Owner == source.Owner)
                    continue;

                var vessel = GameManager.Instance.Game.UniverseModel.FindVesselComponent(graphNode.Owner);
                if (vessel == null)
                    continue;

                var route = CommNextRedux.ManagedCommNextGraph.BuildRoute(vessel);
                if (!route.Evaluated || !route.Connected || route.RouteOwners.Count < 2)
                    continue;

                for (var i = 1; i < route.RouteOwners.Count; i++)
                {
                    var prev = FindNodeIndex(route.RouteOwners[i - 1]);
                    var current = FindNodeIndex(route.RouteOwners[i]);
                    if (prev >= 0 && current >= 0)
                        _previousIndices[current] = prev;
                }
            }
        }
    }

    private void SyncNodes()
    {
        var alive = new HashSet<IGGuid>();

        foreach (var graphNode in _graphNodes)
        {
            alive.Add(graphNode.Owner);

            NetworkNode legacy;
            if (!Nodes.TryGetValue(graphNode.Owner, out legacy))
            {
                legacy = new NetworkNode(graphNode.Owner);
                Nodes[graphNode.Owner] = legacy;
            }

            CommNextRedux.Network.NetworkNode redux;
            if (CommNextRedux.CommNetBridge.Nodes.TryGetValue(graphNode.Owner, out redux))
            {
                legacy.IsRelay = redux.IsRelay;
                legacy.HasEnoughResources = redux.HasEnoughResources;
                legacy.VesselName = IsSourceNode(graphNode.Owner)
                    ? "KSC"
                    : redux.VesselName;

                var ranges = new Dictionary<int, double>();
                for (var i = 0; i < redux.BandRanges.Length; i++)
                    if (redux.BandRanges[i] > 0d)
                        ranges[i] = redux.BandRanges[i];
                legacy.SetBandRanges(ranges);
            }
            else
            {
                var ranges = new Dictionary<int, double> { { 0, graphNode.MaxRange } };
                legacy.SetBandRanges(ranges);

                var vessel = GameManager.Instance.Game.UniverseModel.FindVesselComponent(graphNode.Owner);
                if (vessel != null)
                    legacy.VesselName = vessel.Name;
            }
        }

        var removed = Nodes.Keys.Where(id => !alive.Contains(id)).ToArray();
        foreach (var id in removed)
            Nodes.Remove(id);
    }

    public bool TryGetConnectionGraphNodesAndIndexes(
        out List<ConnectionGraphNode>? nodes,
        out int[] prevIndexes,
        out NativeArray<NetworkJobConnection>? connections)
    {
        Sync();

        if (_graphNodes.Count == 0 || !_connections.IsCreated)
        {
            nodes = null;
            prevIndexes = Array.Empty<int>();
            connections = null;
            return false;
        }

        nodes = _graphNodes;
        prevIndexes = (int[])_previousIndices.Clone();
        connections = _connections;
        return true;
    }

    public List<NetworkConnection> GetNodeConnections(
        NetworkNode networkNode,
        VesselNodesFilter nodesFilter = VesselNodesFilter.InRange)
    {
        Sync();

        var result = new List<NetworkConnection>();
        if (!_connections.IsCreated || _graphNodes.Count == 0)
            return result;

        var count = _graphNodes.Count;
        var nodeIndex = FindNodeIndex(networkNode.Owner);
        if (nodeIndex < 0)
            return result;

        var hasActivePath = _previousIndices[nodeIndex] != -1 ||
                            IsSourceNode(_graphNodes[nodeIndex].Owner);

        for (var i = 0; i < count; i++)
        {
            if (i == nodeIndex)
                continue;

            var outbound = _connections[nodeIndex * count + i];
            var inbound = _connections[i * count + nodeIndex];

            var outboundActive = _previousIndices[i] == nodeIndex;
            var inboundActive = _previousIndices[nodeIndex] == i;

            var shouldAddOutbound = ShouldInclude(outbound, outboundActive, nodesFilter);
            var shouldAddInbound = ShouldInclude(inbound, inboundActive, nodesFilter);

            if (!Nodes.TryGetValue(_graphNodes[i].Owner, out var otherNode))
                continue;

            // Redux' compatibility graph is evaluated in both directions, while the
            // current CommNext link properties are symmetric. Showing both entries
            // produces duplicate rows for the same remote vessel. Keep one row and
            // prefer the direction that belongs to the active route when available.
            if (inboundActive && shouldAddInbound)
            {
                result.Add(new NetworkConnection(
                    otherNode,
                    networkNode,
                    _graphNodes[i],
                    _graphNodes[nodeIndex],
                    inbound,
                    true));
            }
            else if (outboundActive && shouldAddOutbound)
            {
                result.Add(new NetworkConnection(
                    networkNode,
                    otherNode,
                    _graphNodes[nodeIndex],
                    _graphNodes[i],
                    outbound,
                    true));
            }
            else if (shouldAddOutbound)
            {
                result.Add(new NetworkConnection(
                    networkNode,
                    otherNode,
                    _graphNodes[nodeIndex],
                    _graphNodes[i],
                    outbound,
                    false));
            }
            else if (shouldAddInbound &&
                     (!hasActivePath || nodesFilter != VesselNodesFilter.Active))
            {
                result.Add(new NetworkConnection(
                    otherNode,
                    networkNode,
                    _graphNodes[i],
                    _graphNodes[nodeIndex],
                    inbound,
                    false));
            }
        }

        return result;
    }

    private static bool ShouldInclude(
        NetworkJobConnection connection,
        bool isActive,
        VesselNodesFilter filter)
    {
        switch (filter)
        {
            case VesselNodesFilter.All:
                return true;
            case VesselNodesFilter.Active:
                return isActive;
            case VesselNodesFilter.InRange:
                return connection.IsInRange;
            case VesselNodesFilter.Connected:
                return connection.IsConnected;
            default:
                return false;
        }
    }

    public bool TryGetNetworkPath(
        IGGuid targetId,
        List<ConnectionGraphNode> graphNodes,
        int[] prevIndexes,
        out HashSet<(int, int)>? path)
    {
        path = null;
        var targetIndex = -1;
        for (var i = 0; i < graphNodes.Count; i++)
        {
            if (graphNodes[i].Owner == targetId)
            {
                targetIndex = i;
                break;
            }
        }

        var source = CommNetManager == null ? null : CommNetManager.GetSourceNode();
        var sourceIndex = source == null ? -1 : FindNodeIndex(source.Owner);

        if (targetIndex < 0 || sourceIndex < 0)
            return false;

        var result = new HashSet<(int, int)>();
        var current = targetIndex;
        var guard = 0;

        while (current != sourceIndex && guard++ < graphNodes.Count)
        {
            var previous = prevIndexes[current];
            if (previous < 0)
                return false;

            result.Add((previous, current));
            current = previous;
        }

        if (current != sourceIndex)
            return false;

        path = result;
        return true;
    }

    private int FindNodeIndex(IGGuid owner)
    {
        for (var i = 0; i < _graphNodes.Count; i++)
            if (_graphNodes[i].Owner == owner)
                return i;
        return -1;
    }

    private bool IsSourceNode(IGGuid owner)
    {
        var source = CommNetManager == null ? null : CommNetManager.GetSourceNode();
        return source != null && source.Owner == owner;
    }

    private NetworkJobConnection EvaluateConnection(
        ConnectionGraphNode sourceNode,
        ConnectionGraphNode targetNode,
        List<BodyInfo> bodies)
    {
        var result = new NetworkJobConnection
        {
            SelectedBand = -1,
            OccludingBody = -1
        };

        NetworkNode source;
        NetworkNode target;
        if (!Nodes.TryGetValue(sourceNode.Owner, out source) ||
            !Nodes.TryGetValue(targetNode.Owner, out target))
            return result;

        var distanceSq = math.distancesq(sourceNode.Position, targetNode.Position);
        var distance = math.sqrt(distanceSq);

        var maxRangeInRange =
            distance <= sourceNode.MaxRange &&
            distance <= targetNode.MaxRange;

        result.IsInRange = maxRangeInRange;

        var anyCommonBand = false;
        short selectedBand = -1;

        var bandCount = Math.Min(source.BandRanges.Length, target.BandRanges.Length);
        for (short i = 0; i < bandCount; i++)
        {
            var sourceRange = source.BandRanges[i];
            var targetRange = target.BandRanges[i];

            if (sourceRange <= 0d || targetRange <= 0d)
                continue;

            anyCommonBand = true;

            if (distance <= sourceRange && distance <= targetRange)
            {
                selectedBand = i;
                break;
            }
        }

        result.HasMatchingBand = anyCommonBand;
        result.SelectedBand = selectedBand;
        result.IsBandMissingRange = anyCommonBand && selectedBand < 0;

        var occludingBody = FindOccludingBody(
            sourceNode.Position,
            targetNode.Position,
            bodies);

        result.IsOccluded = occludingBody >= 0;
        result.OccludingBody = (short)occludingBody;

        result.IsConnected =
            maxRangeInRange &&
            selectedBand >= 0 &&
            !result.IsOccluded &&
            source.HasEnoughResources &&
            target.HasEnoughResources;

        return result;
    }

    private sealed class BodyInfo
    {
        internal double3 Position;
        internal double Radius;
        internal int Index;
    }

    private static List<BodyInfo> BuildBodies(ConnectionGraphNode? sourceNode)
    {
        var result = new List<BodyInfo>();
        if (sourceNode == null)
            return result;

        try
        {
            var game = GameManager.Instance.Game;
            var sourceObject = game.SpaceSimulation.FindSimObject(sourceNode.Owner);
            var sourceTransform = sourceObject == null
                ? null
                : sourceObject.transform as TransformModel;

            if (sourceTransform == null)
                return result;

            var bodies = game.UniverseModel.GetAllCelestialBodies();
            for (var i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                var local = sourceTransform.celestialFrame.ToLocalPosition(body.transform.Position);
                result.Add(new BodyInfo
                {
                    Position = new double3(local.x, local.y, local.z),
                    Radius = Math.Max(0d, body.radius * 0.98d - 1000d),
                    Index = i
                });
            }
        }
        catch
        {
            // Rendering diagnostics are best-effort. The authoritative route remains
            // the Redux CommNext graph.
        }

        return result;
    }

    private static int FindOccludingBody(
        double3 p1,
        double3 p2,
        List<BodyInfo> bodies)
    {
        foreach (var body in bodies)
        {
            var a = p1 - body.Position;
            var b = p2 - body.Position;
            var d = b - a;

            var qa = math.dot(d, d);
            if (qa <= 0d)
                continue;

            var qb = 2d * math.dot(d, a);
            var qc = math.dot(a, a) - body.Radius * body.Radius;
            var discriminant = qb * qb - 4d * qa * qc;

            if (discriminant < 0d)
                continue;

            var sqrt = Math.Sqrt(discriminant);
            var t1 = (-qb - sqrt) / (2d * qa);
            var t2 = (-qb + sqrt) / (2d * qa);

            if ((t1 >= 0d && t1 <= 1d) ||
                (t2 >= 0d && t2 <= 1d))
                return body.Index;
        }

        return -1;
    }
}
