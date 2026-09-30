# Porting status â€” v0.1.0-alpha

## Full source included
The original CommNext C# source tree is now compiled as part of CommNextRedux.

## Redux replacements
- BepInEx logging/config -> compatibility layer.
- SpaceWarp AssetManager -> direct AssetBundle loader.
- SpaceWarp ModSaves -> compatibility registry; save-game persistence still needs final Redux-native wiring.
- UITKForKsp2 windows -> runtime UIDocument factory.
- CommNetManager private-field lifecycle -> CommNetBridge + managed Redux graph.
- Original NetworkManager public API -> facade backed by Redux routing.
- Private map actions -> reflection compatibility helpers.
- Old runtime syntax -> KSP2-compatible equivalents.

## Already functional from previous alpha work
- KSC routing and occlusion.
- Antenna ranges.
- Band-aware links.
- Relay recognition.
- Map link rendering.
- Authoritative Connected/Disconnected state.

## Needs runtime validation
- Original UITK window layout against Redux PanelSettings.
- Original ruler prefabs/shader bundle.
- Vessel report interactions.
- Save persistence per campaign.
- Final relay/modulator PAM injection via Redux-native Lua patches.

Upstream: https://github.com/Kerbalight/CommNext
