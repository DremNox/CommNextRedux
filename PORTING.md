# Porting status

## Completed through v0.0.9
- Redux bootstrap and lifecycle.
- Flight/OAB state detection.
- CommNetManager lifecycle bridge.
- KSC source-node correction.
- Physical KSC distance.
- Node registry.
- X/S/K/Ka/V band model.
- Relay and Modulator part modules.
- Stock antenna/relay range patches.
- Relay EC consumption.
- Parallel managed route calculator.
- Band-aware links.
- Relay-only forwarding.
- Planetary occlusion checks.

## Next
1. Map managed route result to Redux ConnectionEdge / ConnectionGraph.
2. Replace vanilla route selection with CommNext-aware routing.
3. Restore connection rendering in map view.
4. Restore configuration and save data.
5. Restore original UITK windows and controls.
6. Restore localization and final PAM controls.

Upstream: https://github.com/Kerbalight/CommNext