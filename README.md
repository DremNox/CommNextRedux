# CommNext Redux

Port comunitario de [CommNext](https://github.com/Kerbalight/CommNext) para **Kerbal Space Program 2 Redux**.

## Estado actual

**v0.0.3 - nucleo CommNet**

- Carga como `KerbalMod` nativo de Redux.
- Detecta la nave activa.
- Se engancha al `KSP.Game.CommNetManager` real mediante Harmony.
- Lee el estado CommNet de la nave, rango de antena y distancia de red.
- Lee el numero actual de nodos del grafo CommNet.
- La ventana solo aparece cuando existe una nave activa; no se muestra en el menu principal.
- Compilado contra KSP2 v0.2.2.0 + Redux / SpaceWarp2.

## Proyecto original

CommNext fue creado por **Kerbalight / leonardfactory**:
https://github.com/Kerbalight/CommNext

Este port conserva la licencia MIT y la atribucion original.

## Instalacion

Descarga el ZIP de la ultima version en **Releases** y copia la carpeta `CommNextRedux` dentro de `<KSP2>/mods/`.

Las versiones 0.0.x son builds de desarrollo.
