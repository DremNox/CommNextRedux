using System;
using System.Collections.Generic;
using KSP.Game;
using KSP.Map;
using KSP.Sim.impl;
using CommNextRedux.Network;
using UnityEngine;

namespace CommNextRedux
{
    internal static class ReduxMapRouteRenderer
    {
        private static readonly Dictionary<string, LineRenderer> Lines =
            new Dictionary<string, LineRenderer>();

        private static float _nextStructureRefresh;
        private static Material _lineMaterial;

        internal static void Update(VesselComponent vessel, CommNextRouteResult route)
        {
            try
            {
                var game = GameManager.Instance == null ? null : GameManager.Instance.Game;
                var state = game == null || game.GlobalGameState == null
                    ? null
                    : game.GlobalGameState.GetGameState();

                if (game == null || state == null ||
                    state.GameState != GameState.Map3DView ||
                    vessel == null ||
                    route == null ||
                    route.RouteOwners.Count < 2)
                {
                    Clear();
                    return;
                }

                MapCore mapCore;
                if (!game.Map.TryGetMapCore(out mapCore) || mapCore == null || mapCore.map3D == null)
                {
                    Clear();
                    return;
                }

                if (Time.unscaledTime >= _nextStructureRefresh)
                {
                    _nextStructureRefresh = Time.unscaledTime + 0.25f;
                    SyncLines(mapCore, route);
                }

                UpdatePositions(mapCore, route);
            }
            catch (Exception ex)
            {
                CommNetBridge.Log?.LogError("[CommNextRedux] Map renderer: " + ex);
                Clear();
            }
        }

        private static void SyncLines(MapCore mapCore, CommNextRouteResult route)
        {
            var keep = new HashSet<string>();

            for (var i = 0; i < route.RouteOwners.Count - 1; i++)
            {
                var sourceOwner = route.RouteOwners[i];
                var targetOwner = route.RouteOwners[i + 1];
                var id = sourceOwner + "->" + targetOwner;
                keep.Add(id);

                LineRenderer line;
                if (!Lines.TryGetValue(id, out line) || line == null)
                {
                    var obj = new GameObject("CommNextReduxRoute_" + i);
                    obj.layer = LayerMask.NameToLayer("Map");
                    obj.transform.SetParent(mapCore.map3D.transform, false);

                    line = obj.AddComponent<LineRenderer>();
                    line.useWorldSpace = true;
                    line.positionCount = 2;
                    line.startWidth = 0.07f;
                    line.endWidth = 0.07f;
                    line.numCapVertices = 2;
                    line.material = GetLineMaterial();
                    Lines[id] = line;
                }

                var bandIndex = i < route.RouteBands.Count ? route.RouteBands[i] : -1;
                var color = !route.Connected
                    ? new Color(1f, 0.25f, 0.20f, 0.95f)
                    : bandIndex >= 0 && bandIndex < NetworkBands.Instance.AllBands.Count
                        ? NetworkBands.Instance.AllBands[bandIndex].Color
                        : Color.white;

                line.startColor = color;
                line.endColor = color;
            }

            var remove = new List<string>();
            foreach (var pair in Lines)
                if (!keep.Contains(pair.Key))
                    remove.Add(pair.Key);

            foreach (var id in remove)
            {
                var line = Lines[id];
                if (line != null)
                    UnityEngine.Object.Destroy(line.gameObject);
                Lines.Remove(id);
            }
        }

        private static void UpdatePositions(MapCore mapCore, CommNextRouteResult route)
        {
            for (var i = 0; i < route.RouteOwners.Count - 1; i++)
            {
                Map3DFocusItem sourceItem;
                Map3DFocusItem targetItem;

                if (!TryGetMapItem(mapCore, route.RouteOwners[i], i == 0, out sourceItem) ||
                    !TryGetMapItem(mapCore, route.RouteOwners[i + 1], false, out targetItem))
                    continue;

                var id = route.RouteOwners[i] + "->" + route.RouteOwners[i + 1];

                LineRenderer line;
                if (!Lines.TryGetValue(id, out line) || line == null)
                    continue;

                line.SetPosition(0, sourceItem.transform.position);
                line.SetPosition(1, targetItem.transform.position);
            }
        }

        private static bool TryGetMapItem(
            MapCore mapCore,
            IGGuid owner,
            bool isSource,
            out Map3DFocusItem item)
        {
            item = null;

            var guid = isSource && CommNetBridge.Manager != null &&
                       CommNetBridge.Manager.GetSourceNode() != null &&
                       CommNetBridge.Manager.GetSourceNode().Owner == owner
                ? mapCore.KSCGUID
                : owner;

            if (mapCore.map3D.AllMapSelectableItems == null)
                return false;

            return mapCore.map3D.AllMapSelectableItems.TryGetValue(guid, out item) && item != null;
        }

        private static Material GetLineMaterial()
        {
            if (_lineMaterial != null)
                return _lineMaterial;

            var shader = Shader.Find("Sprites/Default");
            _lineMaterial = shader == null ? null : new Material(shader);
            return _lineMaterial;
        }

        internal static void Clear()
        {
            if (Lines.Count == 0)
                return;

            foreach (var line in Lines.Values)
            {
                if (line != null)
                    UnityEngine.Object.Destroy(line.gameObject);
            }

            Lines.Clear();
        }
    }
}
