using System;
using System.Collections.Generic;
using CommNextRedux.Network;
using KSP.Game;
using KSP.Sim;
using KSP.Sim.impl;
using Unity.Mathematics;
using UnityEngine;

namespace CommNextRedux
{
    internal sealed class CommNextRouteResult
    {
        internal bool Evaluated;
        internal bool Connected;
        internal int Hops;
        internal double TotalDistanceMeters;
        internal double GraphCost;
        internal List<IGGuid> RouteOwners = new List<IGGuid>();
        internal List<int> RouteBands = new List<int>();
        internal string Path = "sin ruta";
        internal string Reason = "";
    }

    internal static class ManagedCommNextGraph
    {
        private const double OcclusionRadiusFactor = 0.98;
        private const double SeaLevelTerrainTolerance = 1000.0;
        private const float CacheSeconds = 0.25f;

        private sealed class CacheEntry
        {
            internal float ExpiresAt;
            internal int NodeCount;
            internal CommNextRouteResult Result;
        }

        private static readonly Dictionary<IGGuid, CacheEntry> Cache =
            new Dictionary<IGGuid, CacheEntry>();

        internal static void Invalidate()
        {
            Cache.Clear();
        }

        internal static CommNextRouteResult BuildRoute(VesselComponent vessel)
        {
            if (vessel == null)
                return new CommNextRouteResult { Reason = "Sin nave" };

            CacheEntry entry;
            var nodeCount = CommNetBridge.NodeCount;
            if (Cache.TryGetValue(vessel.GlobalId, out entry) &&
                entry != null &&
                entry.ExpiresAt >= Time.unscaledTime &&
                entry.NodeCount == nodeCount)
                return entry.Result;

            var result = ComputeRoute(vessel);
            Cache[vessel.GlobalId] = new CacheEntry
            {
                ExpiresAt = Time.unscaledTime + CacheSeconds,
                NodeCount = nodeCount,
                Result = result
            };
            return result;
        }

        private static CommNextRouteResult ComputeRoute(VesselComponent vessel)
        {
            var result = new CommNextRouteResult();

            try
            {
                if (vessel == null || CommNetBridge.Manager == null)
                {
                    result.Reason = "CommNet no disponible";
                    return result;
                }

                var graphNodes = CommNetBridge.GetGraphNodes();
                if (graphNodes.Count == 0)
                {
                    result.Reason = "Sin nodos";
                    return result;
                }

                var sourceNode = CommNetBridge.Manager.GetSourceNode();
                if (sourceNode == null)
                {
                    result.Reason = "Sin nodo origen";
                    return result;
                }

                var sourceIndex = FindNode(graphNodes, sourceNode.Owner);
                var targetIndex = FindNode(graphNodes, vessel.GlobalId);
                if (sourceIndex < 0 || targetIndex < 0)
                {
                    result.Reason = "Origen o nave no registrados";
                    return result;
                }

                result.Evaluated = true;

                if (targetIndex == sourceIndex)
                {
                    result.Connected = true;
                    result.Path = "KSC";
                    result.Reason = "Nodo origen";
                    result.GraphCost = 0d;
                    result.RouteOwners.Add(sourceNode.Owner);
                    return result;
                }

                var bodies = BuildBodies(sourceNode);
                var count = graphNodes.Count;
                var sourceCosts = new double[count];
                var optimums = new double[count];
                var previous = new int[count];
                var previousBand = new int[count];
                var previousEdgeDistance = new double[count];
                var processed = new bool[count];
                var queue = new List<int>(count);
                var remainingRelays = 0;

                for (var i = 0; i < count; i++)
                {
                    sourceCosts[i] = double.MaxValue;
                    optimums[i] = double.MaxValue;
                    previous[i] = -1;
                    previousBand[i] = -1;
                    previousEdgeDistance[i] = 0d;
                    CommNetBridge.RegisterOrUpdateNode(graphNodes[i]);
                    queue.Add(i);

                    NetworkNode registered;
                    if (CommNetBridge.Nodes.TryGetValue(graphNodes[i].Owner, out registered) &&
                        registered.IsRelay)
                        remainingRelays++;
                }

                sourceCosts[sourceIndex] = 0d;
                optimums[sourceIndex] = 0d;

                while (queue.Count > 0)
                {
                    var queueIndex = -1;
                    var current = -1;
                    var lower = double.MaxValue;

                    for (var qi = 0; qi < queue.Count; qi++)
                    {
                        var candidateIndex = queue[qi];

                        NetworkNode candidateNode;
                        var candidateIsRelay =
                            CommNetBridge.Nodes.TryGetValue(graphNodes[candidateIndex].Owner, out candidateNode) &&
                            candidateNode.IsRelay;

                        if (remainingRelays > 0 && !candidateIsRelay)
                            continue;

                        if (queueIndex < 0 || sourceCosts[candidateIndex] < lower)
                        {
                            queueIndex = qi;
                            current = candidateIndex;
                            lower = sourceCosts[candidateIndex];
                        }
                    }

                    if (current < 0)
                    {
                        // Defensive fallback if relay metadata changes while evaluating.
                        queueIndex = 0;
                        current = queue[0];
                    }

                    queue.RemoveAt(queueIndex);

                    var currentGraphNode = graphNodes[current];
                    NetworkNode currentNode;
                    if (!CommNetBridge.Nodes.TryGetValue(currentGraphNode.Owner, out currentNode))
                        continue;

                    if (currentNode.IsRelay && remainingRelays > 0)
                        remainingRelays--;

                    processed[current] = true;

                    if (!currentGraphNode.IsActive)
                        continue;

                    if (!currentNode.HasEnoughResources)
                        continue;

                    if (!currentGraphNode.IsControlSource && !currentNode.IsRelay)
                        continue;

                    // The original CommNext processes disconnected relays first, but does
                    // not use them to establish a route until they are reachable.
                    if (sourceCosts[current] == double.MaxValue)
                        continue;

                    for (var target = 0; target < count; target++)
                    {
                        if (target == current || processed[target])
                            continue;

                        var targetGraphNode = graphNodes[target];
                        if (!targetGraphNode.IsActive)
                            continue;

                        NetworkNode targetNode;
                        if (!CommNetBridge.Nodes.TryGetValue(targetGraphNode.Owner, out targetNode))
                            continue;

                        if (!targetNode.HasEnoughResources)
                            continue;

                        var distanceSq = DistanceSquared(
                            currentGraphNode.Position,
                            targetGraphNode.Position);

                        if (distanceSq <= 0d)
                            continue;

                        if (distanceSq >= currentGraphNode.MaxRange * currentGraphNode.MaxRange ||
                            distanceSq >= targetGraphNode.MaxRange * targetGraphNode.MaxRange)
                            continue;

                        var band = FindMatchingBandSquared(currentNode, targetNode, distanceSq);
                        if (band < 0)
                            continue;

                        if (IsOccluded(currentGraphNode.Position, targetGraphNode.Position, bodies))
                            continue;

                        // Original CommNext default: NearestRelay. It minimizes the
                        // current hop while still tracking accumulated squared cost.
                        var optimum = distanceSq;
                        if (optimum >= optimums[target])
                            continue;

                        optimums[target] = optimum;
                        sourceCosts[target] = sourceCosts[current] + distanceSq;
                        previous[target] = current;
                        previousBand[target] = band;
                        previousEdgeDistance[target] = Math.Sqrt(distanceSq);
                    }
                }

                if (previous[targetIndex] < 0)
                {
                    // Expose the direct KSC -> vessel attempt so the map renderer can
                    // still show why there is no valid route.
                    var sourceGraph = graphNodes[sourceIndex];
                    var targetGraph = graphNodes[targetIndex];
                    var directDistanceSq = DistanceSquared(sourceGraph.Position, targetGraph.Position);
                    var directDistance = directDistanceSq > 0d ? Math.Sqrt(directDistanceSq) : 0d;

                    result.RouteOwners.Add(sourceGraph.Owner);
                    result.RouteOwners.Add(targetGraph.Owner);
                    result.TotalDistanceMeters = directDistance;

                    NetworkNode sourceNetwork;
                    NetworkNode targetNetwork;
                    var hasSource = CommNetBridge.Nodes.TryGetValue(sourceGraph.Owner, out sourceNetwork);
                    var hasTarget = CommNetBridge.Nodes.TryGetValue(targetGraph.Owner, out targetNetwork);

                    var directBand = hasSource && hasTarget
                        ? FindMatchingBandSquared(sourceNetwork, targetNetwork, directDistanceSq)
                        : -1;
                    result.RouteBands.Add(directBand);

                    var inNodeRange = directDistanceSq > 0d &&
                                      directDistanceSq < sourceGraph.MaxRange * sourceGraph.MaxRange &&
                                      directDistanceSq < targetGraph.MaxRange * targetGraph.MaxRange;

                    if (!inNodeRange)
                        result.Reason = "Fuera de rango";
                    else if (directBand < 0)
                        result.Reason = "Sin banda/rango compatible";
                    else if (IsOccluded(sourceGraph.Position, targetGraph.Position, bodies))
                        result.Reason = "Bloqueada por cuerpo celeste";
                    else
                        result.Reason = "Sin ruta compatible";

                    return result;
                }

                var indexes = new List<int>();
                var bands = new List<int>();
                var edgeDistances = new List<double>();
                var cursor = targetIndex;

                while (cursor >= 0 && cursor != sourceIndex)
                {
                    indexes.Add(cursor);
                    bands.Add(previousBand[cursor]);
                    edgeDistances.Add(previousEdgeDistance[cursor]);
                    cursor = previous[cursor];

                    if (indexes.Count > count)
                    {
                        result.Reason = "Ruta ciclica";
                        return result;
                    }
                }

                if (cursor != sourceIndex)
                {
                    result.Reason = "Ruta incompleta";
                    return result;
                }

                indexes.Add(sourceIndex);
                indexes.Reverse();
                bands.Reverse();
                edgeDistances.Reverse();

                var pathParts = new List<string>();
                var physicalDistance = 0d;

                for (var i = 0; i < indexes.Count; i++)
                {
                    var graphNode = graphNodes[indexes[i]];
                    string name;

                    if (i == 0)
                    {
                        name = "KSC";
                    }
                    else
                    {
                        NetworkNode networkNode;
                        name = CommNetBridge.Nodes.TryGetValue(graphNode.Owner, out networkNode)
                            ? networkNode.VesselName
                            : graphNode.Owner.ToString();
                    }

                    pathParts.Add(name);

                    if (i < bands.Count)
                    {
                        var bandIndex = bands[i];
                        var bandName = bandIndex >= 0 && bandIndex < NetworkBands.Instance.AllBands.Count
                            ? NetworkBands.Instance.AllBands[bandIndex].Code
                            : "?";
                        pathParts.Add("--" + bandName + "-->");
                    }

                    if (i < edgeDistances.Count)
                        physicalDistance += edgeDistances[i];
                }

                result.Connected = true;
                result.Hops = indexes.Count - 1;
                result.TotalDistanceMeters = physicalDistance;
                result.GraphCost = sourceCosts[targetIndex];
                foreach (var index in indexes)
                    result.RouteOwners.Add(graphNodes[index].Owner);
                foreach (var band in bands)
                    result.RouteBands.Add(band);
                result.Path = string.Join(" ", pathParts.ToArray());
                result.Reason = "Ruta valida";
                return result;
            }
            catch (Exception ex)
            {
                CommNetBridge.Log?.LogError("[CommNextRedux] Managed route: " + ex);
                result.Reason = "Error calculando ruta";
                result.Evaluated = false;
                return result;
            }
        }

        private static int FindNode(List<ConnectionGraphNode> nodes, IGGuid owner)
        {
            for (var i = 0; i < nodes.Count; i++)
                if (nodes[i].Owner == owner)
                    return i;
            return -1;
        }

        private static int FindMatchingBandSquared(NetworkNode a, NetworkNode b, double distanceSq)
        {
            for (var i = 0; i < NetworkBands.Instance.AllBands.Count; i++)
            {
                if (a.BandRanges.Length <= i || b.BandRanges.Length <= i)
                    continue;

                var ar = a.BandRanges[i];
                var br = b.BandRanges[i];

                if (ar > 0d && br > 0d &&
                    distanceSq < ar * ar &&
                    distanceSq < br * br)
                    return i;
            }

            return -1;
        }

        private static double DistanceSquared(double3 a, double3 b)
        {
            var dx = a.x - b.x;
            var dy = a.y - b.y;
            var dz = a.z - b.z;
            return dx * dx + dy * dy + dz * dz;
        }

        private sealed class BodyInfo
        {
            internal double3 Position;
            internal double Radius;
        }

        private static List<BodyInfo> BuildBodies(ConnectionGraphNode sourceNode)
        {
            var result = new List<BodyInfo>();
            var game = GameManager.Instance.Game;
            var sourceObject = game.SpaceSimulation.FindSimObject(sourceNode.Owner);
            var sourceTransform = sourceObject == null ? null : sourceObject.transform as TransformModel;
            if (sourceTransform == null)
                return result;

            var celestialBodies = game.UniverseModel.GetAllCelestialBodies();
            foreach (var body in celestialBodies)
            {
                var local = sourceTransform.celestialFrame.ToLocalPosition(body.transform.Position);
                result.Add(new BodyInfo
                {
                    Position = new double3(local.x, local.y, local.z),
                    Radius = Math.Max(
                        0d,
                        body.radius * OcclusionRadiusFactor - SeaLevelTerrainTolerance)
                });
            }

            return result;
        }

        private static bool IsOccluded(double3 p1, double3 p2, List<BodyInfo> bodies)
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
                    return true;
            }

            return false;
        }
    }
}
