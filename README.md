# CommNext Redux

Full port of Kerbalight/CommNext to KSP2 Redux / SpaceWarp2.

## v0.1.0-alpha

This is the first full-source alpha. The original CommNext source tree has been brought into the Redux build instead of only porting isolated features.

Included:
- Redux-native CommNet bootstrap and lifecycle.
- Authoritative CommNext routing.
- Planetary occlusion.
- X / S / K / Ka / V band model.
- Relay / modulator modules and relay EC logic.
- Effective original CommNext antenna ranges.
- KSC source handling.
- Original NetworkManager API adapted onto the Redux graph.
- Original ConnectionsRenderer and map ruler code compiled.
- Original CommNext UITK controls and UI controllers compiled.
- Original commnext_ui.bundle is packaged and loaded through a Redux compatibility layer.
- Original localization and native library are packaged.
- Legacy BepInEx / SpaceWarp / UITKForKsp2 calls are bridged through Redux compatibility shims.
- Diagnostic IMGUI remains available with Alt+C as a fallback.

Repository layout:
- src/Redux/ â€” Redux-native bridge, authoritative routing, compatibility shims.
- src/FullPort/ â€” adapted source tree from the original CommNext project.

Upstream / license:
Original project: https://github.com/Kerbalight/CommNext
The original MIT license and attribution are preserved.
