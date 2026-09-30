# CommNext Redux

Port de [CommNext](https://github.com/Kerbalight/CommNext) a **Kerbal Space Program 2 Redux**.

## Estado actual: v0.1.0-preview.1

Esta preview integra el codigo original de CommNext sobre un nucleo adaptado a Redux.

Incluye:

- Red CommNext autoritativa sobre CommNetManager de Redux.
- Oclusion por cuerpos celestes.
- Rutas KSC -> relay -> nave.
- Bandas X / S / K / Ka / V.
- Rangos originales de CommNext.
- Relay y Modulator en PAM mediante Patch Manager Lua.
- Consumo electrico de relays.
- UI Toolkit original de CommNext.
- Toolbar de mapa.
- Vessel Report.
- Filtros, ordenacion y signal strength.
- Renderer original de conexiones.
- Rulers de alcance.
- Tooltips y controles UI originales.
- Assets y localizaciones originales.
- Persistencia de estado de la UI.
- Fallbacks Redux para APIs privadas/renombradas.

La antigua ventana IMGUI de diagnostico queda oculta por defecto.

## Fuente

El port completo esta en:

- `src-full/Original`: codigo original adaptado.
- `src-full/ReduxCore`: nucleo de compatibilidad Redux.
- `src-full/Compat`: adaptadores para APIs antiguas.
- `src-full/UnityControls`: controles UI originales.
- `patches/commnext_modules.lua`: inyeccion Redux de Relay/Modulator.

El script `build-full.ps1` construye la DLL completa.

## Instalacion

Descarga la ultima release preview y copia `CommNextRedux` dentro de:

`<KSP2>/mods/`

Requiere Redux / SpaceWarp2.

## Upstream y licencia

CommNext original: https://github.com/Kerbalight/CommNext

Se conserva la licencia MIT y la atribucion del proyecto original.