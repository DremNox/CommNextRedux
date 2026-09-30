# CommNext Redux

Port comunitario de [CommNext](https://github.com/Kerbalight/CommNext) para **Kerbal Space Program 2 Redux**.

## Estado actual

**v0.0.4 - nucleo CommNet + origen KSC**

- Carga como `KerbalMod` nativo de Redux.
- Detecta la nave activa.
- Se engancha al `KSP.Game.CommNetManager` real mediante Harmony.
- Lee estado CommNet, rango de antena, distancia de red y numero de nodos.
- Porta el tratamiento original del nodo origen de KSC.
- Corrige la posicion del origen CommNet sobre KSC.
- Aplica el rango base original de KSC: 2 Gm.
- La ventana solo aparece con una nave activa.

## Proyecto original

CommNext fue creado por **Kerbalight / leonardfactory**:
https://github.com/Kerbalight/CommNext

Este port conserva la licencia MIT y la atribucion original.

## Instalacion

Descarga el ZIP de la ultima version en **Releases** y copia `CommNextRedux` dentro de `<KSP2>/mods/`.

Las versiones 0.0.x son builds de desarrollo.
