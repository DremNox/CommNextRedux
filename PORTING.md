# Porting status

## v0.0.5

Completed:
- Redux KerbalMod bootstrap.
- Active vessel detection.
- Harmony bridge for KSP.Game.CommNetManager.
- CommNetManager lifecycle capture.
- Vessel CommNet status, antenna range and network distance.
- KSC source position correction and default 2 Gm range.
- Network node registry synchronized with vanilla CommNet.
- Original X / S / K / Ka / V band catalog.
- Vanilla nodes mapped to X band with their current range.
- UI hidden outside active flight.

Next:
1. Port relay and modulator data/modules.
2. Feed per-band ranges into registered nodes.
3. Restore occlusion and path logic.
4. Restore EC consumption.
5. Restore map connection rendering.
6. Port configuration, save data and original UI/assets.

Upstream: https://github.com/Kerbalight/CommNext
