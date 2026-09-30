using System;
using System.Collections.Generic;
using CommNextRedux.Network;
using KSP.Game;
using KSP.Sim;
using KSP.Sim.impl;
using Unity.Mathematics;

namespace CommNextRedux
{
    internal sealed class CommNextRouteResult
    {
        internal bool Connected;
        internal int Hops;
        internal double TotalDistanceMeters;
        internal string Path = "sin ruta";
        internal string Reason = "";
    }

    internal static class ManagedCommNextGraph
    {
        private const double OcclusionRadiusFactor = 0.98;
        private const double SeaLevelTerrainTolerance = 1000.0;

        internal static CommNextRouteResult BuildRoute(VesselComponent vessel)
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

                var bodies = BuildBodies(sourceNode);
                var count = graphNodes.Count;
                var distances = new double[count];
                var previous = new int[count];
                var previousBand = new int[count];
                var visited = new bool[count];

                for (var i = 0; i < count; i++)
                {
                    distances[i] = double.MaxValue;
                    previous[i] = -1;
                    previousBand[i] = -1;
                    CommNetBridge.RegisterOrUpdateNode(graphNodes[i]);
                }

                distances[sourceIndex] = 0d;

                for (var step = 0; step < count; step++)
                {
                    var current = -1;
                    var best = double.MaxValue;

                    for (var i = 0; i < count; i++)
                    {
                        if (!visited[i] && distances[i] < best)
                        {
                            best = distances[i];
                            current = i;
                        }
                    }

                    if (current < 0)
                        break;

                    if (current == targetIndex)
                        break;

                    visited[current] = true;

                    var currentGraphNode = graphNodes[current];
                    NetworkNode currentNode;
                    if (!CommNetBridge.Nodes.TryGetValue(currentGraphNode.Owner, out currentNode))
                        continue;

                    var currentIsSource = currentGraphNode.IsControlSource;

                    if (!currentIsSource && !currentNode.IsRelay)
                        continue;

                    if (!currentNode.HasEnoughResources)
                        continue;

                    for (var target = 0; target < count; target++)
                    {
                        if (target == current || visited[target])
                            continue;

                        var targetGraphNode = graphNodes[target];
                        if (!targetGraphNode.IsActive)
                            continue;

                        NetworkNode targetNode;
                        if (!CommNetBridge.Nodes.TryGetValue(targetGraphNode.Owner, out targetNode))
                            continue;

                        if (!targetNode.HasEnoughResources)
                            continue;

                        var distance = Distance(currentGraphNode.Position, targetGraphNode.Position);
                        if (distance <= 0d)
                            continue;

                        if (distance > currentGraphNode.MaxRange || distance > targetGraphNode.MaxRange)
                            continue;

                        var band = FindMatchingBand(currentNode, targetNode, distance);
                        if (band < 0)
                            continue;

                        if (IsOccluded(currentGraphNode.Position, targetGraphNode.Position, bodies))
                            continue;

                        var candidate = distances[current] + distance;
                        if (candidate >= distances[target])
                            continue;

                        distances[target] = candidate;
                        previous[target] = current;
                        previousBand[target] = band;
                    }
                }

                if (targetIndex == sourceIndex)
                {
                    result.Connected = true;
                    result.Path = "KSC";
                    result.Reason = "Nodo origen";
                    return result;
                }

                if (previous[targetIndex] < 0)
                {
                    result.Reason = "Sin ruta compatible";
                    return result;
                }

                var indexes = new List<int>();
                var bands = new List<int>();
                var cursor = targetIndex;

                while (cursor >= 0 && cursor != sourceIndex)
                {
                    indexes.Add(cursor);
                    bands.Add(previousBand[cursor]);
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

                var pathParts = new List<string>();
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
                }

                result.Connected = true;
                result.Hops = indexes.Count - 1;
                result.TotalDistanceMeters = distances[targetIndex];
                result.Path = string.Join(" ", pathParts.ToArray());
                result.Reason = "Ruta valida";
                return result;
            }
            catch (Exception ex)
            {
                CommNetBridge.Log?.LogError("[CommNextRedux] Managed route: " + ex);
                result.Reason = "Error calculando ruta";
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

        private static int FindMatchingBand(NetworkNode a, NetworkNode b, double distance)
        {
            for (var i = 0; i < NetworkBands.Instance.AllBands.Count; i++)
            {
                if (a.BandRanges.Length <= i || b.BandRanges.Length <= i)
                    continue;

                if (a.BandRanges[i] >= distance && b.BandRanges[i] >= distance)
                    return i;
            }

            return -1;
        }

        private static double Distance(double3 a, double3 b)
        {
            var dx = a.x - b.x;
            var dy = a.y - b.y;
            var dz = a.z - b.z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
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
                    Radius = Math.Max(0d,
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

                if ((t1 >= 0d && t1 <= 1d) || (t2 >= 0d && t2 <= 1d))
                    return true;
            }

            return false;
        }
    }
}
