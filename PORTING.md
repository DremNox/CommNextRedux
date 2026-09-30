# Porting status

## v0.0.3

Completed:
- Redux KerbalMod bootstrap.
- Active vessel detection.
- Harmony bridge for KSP.Game.CommNetManager.
- CommNetManager Initialize/Shutdown lifecycle capture.
- Vessel CommNet status and antenna range.
- Network distance from CommNetManager.
- CommNet graph node count.
- Diagnostic UI hidden outside active flight.

Next:
1. Port source-node/KSC handling.
2. Port NetworkNode metadata and bands.
3. Port relay and modulator modules.
4. Restore occlusion and path logic.
5. Restore EC consumption.
6. Restore map connection rendering.
7. Port configuration, save data and original UI/assets.

Upstream: https://github.com/Kerbalight/CommNext
