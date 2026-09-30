# Porting status

## v0.0.4

Completed:
- Redux KerbalMod bootstrap.
- Active vessel detection.
- Harmony bridge for KSP.Game.CommNetManager.
- CommNetManager lifecycle capture.
- Vessel CommNet status, antenna range and network distance.
- CommNet graph node count.
- Original KSC CommNet source-node position correction.
- Original default KSC source range (2 Gm).
- UI hidden outside active flight.

Next:
1. Port NetworkNode metadata and bands.
2. Port relay and modulator modules.
3. Restore occlusion and path logic.
4. Restore EC consumption.
5. Restore map connection rendering.
6. Port configuration, save data and original UI/assets.

Upstream: https://github.com/Kerbalight/CommNext
